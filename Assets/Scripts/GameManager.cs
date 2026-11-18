using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsPlaying { get; private set; }
    public Vector2 MobileDirection { get; private set; }
    public PlayerController player;
    public HUDView hud;
    public ScreenMockupController screens;
    public VisualTheme theme;
    public AudioFeedback audioFeedback;
    public Collectible[] items;
    public GameObject hazardPrefab;
    public Transform exitTransform;
    private List<GameObject> spawnedHazards = new List<GameObject>();
    public TextAsset profiles;
    [Range(0, 30)] public int variantNumber;
    public SpriteRenderer[] gateIndicators;

    public int Progress => progress;
    public int Cargo => cargo;
    public int Lives => lives;
    public bool Won { get; private set; }
    public Profile Current => profile;
    public int CurrentLevel { get; private set; }

    [Serializable] public class Profile
    {
        public string title, goal, progressLabel;
        public int mode, target, capacity;
        public bool resource;
    }
    [Serializable] private class ProfileList { public Profile[] variants; }

    private Profile profile;
    private int progress, cargo, lives;
    private float immunity, remaining;
    private readonly bool[] flags = new bool[6];

    // Позиции коробок для каждого уровня (target = 3, 5, 6)
    private static readonly Vector2[][] BoxPositions = new Vector2[][]
    {
        // Уровень 1: 3 коробки — компактно слева
        new Vector2[]
        {
            new Vector2(-5.5f, 1f),
            new Vector2(-3.4f, 1f),
            new Vector2(-1.3f, 1f),
        },
        // Уровень 2: 5 коробок — по всему полю
        new Vector2[]
        {
            new Vector2(-5.5f, 2f),
            new Vector2(-2.5f, -1f),
            new Vector2(0.5f, 2f),
            new Vector2(3.0f, -1f),
            new Vector2(5.5f, 1f),
        },
        // Уровень 3: 6 коробок — максимум, с опасными зонами рядом
        new Vector2[]
        {
            new Vector2(-5.5f, 2f),
            new Vector2(-3.0f, -1.5f),
            new Vector2(-0.5f, 2f),
            new Vector2(2.0f, -1.5f),
            new Vector2(4.5f, 2f),
            new Vector2(5.5f, -1.5f),
        },
    };
    // Позиции опасностей для каждого уровня (hazards = 2, 4, 6)
    private static readonly Vector2[][] HazardPositions = new Vector2[][]
    {
        // Уровень 1: 2 опасности — по центру поля
        new Vector2[]
        {
            new Vector2(-1.0f, -1.5f),
            new Vector2(2.5f, -1.5f),
        },
        // Уровень 2: 4 опасности — блокируют пути
        new Vector2[]
        {
            new Vector2(-4.0f, -1.0f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(2.5f, -2.0f),
            new Vector2(4.5f, 0.5f),
        },
        // Уровень 3: 6 опасностей — плотная расстановка
        new Vector2[]
        {
            new Vector2(-4.5f, 0.0f),
            new Vector2(-2.0f, -2.0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(2.5f, -1.0f),
            new Vector2(4.0f, 1.5f),
            new Vector2(5.0f, -2.0f),
        },
    };

// Позиции двери выхода для каждого уровня
private static readonly Vector2[] ExitPositions = new Vector2[]
{
    new Vector2(6.7f, 2.3f),   // Уровень 1: правый верхний угол
    new Vector2(-6.7f, 2.3f),  // Уровень 2: левый верхний угол
    new Vector2(0.0f, -3.5f),  // Уровень 3: центр снизу
};

    // Индексы трёх уровней "Складской робот" в profiles.json (0-based)
    private static readonly int[] LevelVariantIndices = { 10, 11, 12 };

    private void Awake() { Instance = this; }

    private void Start()
    {
        CurrentLevel = SaveService.Unlocked;
        SelectVariant(LevelVariantIndices[CurrentLevel - 1]);
        screens.ShowMainMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPlaying) PauseGame();
            else if (screens.pausePanel.activeSelf) ResumeGame();
        }
        if (IsPlaying && profile.resource)
        {
            remaining -= Time.deltaTime;
            Refresh();
            if (remaining <= 0f) Finish(false);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsPlaying) PauseGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsPlaying) PauseGame();
    }

    // Запуск уровня 1, 2 или 3
    public void StartLevel(int number)
    {
        if (number < 1 || number > 3 || number > SaveService.Unlocked) return;
        CurrentLevel = number;
        SelectVariant(LevelVariantIndices[number - 1]);
        ResetState();
        ResumeGame();
    }

    public void SelectVariant(int number)
    {
        variantNumber = number;
        if (profiles != null && profiles.text.Length > 0)
        {
            try
            {
                profile = JsonUtility.FromJson<ProfileList>(profiles.text).variants[number];
            }
            catch
            {
                profile = CreateFallbackProfile();
            }
        }
        else
        {
            profile = CreateFallbackProfile();
        }

        for (int i = 0; i < items.Length; i++)
        {
            items[i].gameObject.SetActive(i < profile.target);
            items[i].transform.Find("Visual").GetComponent<ThemeRole>().role = ThemeRole.Role.Target;
            items[i].transform.Find("Number").GetComponent<TextMesh>().text = (i + 1).ToString();
        }
        theme.Apply();
        hud.SetGoal(profile.title, profile.goal);
    }

    private Profile CreateFallbackProfile()
    {
        return new Profile
        {
            title = "Складской робот",
            goal = "Перенесите объекты в зону выхода по одному.",
            progressLabel = "статус груза и число доставок",
            mode = 2,
            target = 3,
            capacity = 1,
            resource = false
        };
    }

    private void ResetState()
    {
        progress = cargo = 0;
        lives = 3;
        remaining = 90f;
        immunity = 0f;
        Won = false;
        IsPlaying = false;
        MobileDirection = Vector2.zero;
        Array.Clear(flags, 0, flags.Length);

        int levelIndex = Mathf.Clamp(CurrentLevel - 1, 0, BoxPositions.Length - 1);

        // Коробки: количество и позиции
        Vector2[] boxPos = BoxPositions[levelIndex];
        for (int i = 0; i < items.Length; i++)
        {
            bool active = i < profile.target;
            items[i].gameObject.SetActive(active);
            if (active && i < boxPos.Length)
                items[i].transform.position = boxPos[i];
        }

        // Опасности: очищаем старые, создаём новые по массиву
        foreach (GameObject h in spawnedHazards)
            if (h != null) Destroy(h);
        spawnedHazards.Clear();

        if (hazardPrefab != null)
        {
            Vector2[] hazardPos = HazardPositions[levelIndex];
            int hazardCount = Mathf.Min(hazardPos.Length, profile.mode == 0 ? 1 : Mathf.Max(2, levelIndex * 2 + 2));
            // Для варианта 11 (mode=2) используем фиксированное число опасностей по уровню:
            // Уровень 1 = 2, Уровень 2 = 4, Уровень 3 = 6
            int[] hazardsPerLevel = { 2, 4, 6 };
            hazardCount = hazardsPerLevel[levelIndex];

            for (int i = 0; i < hazardCount && i < hazardPos.Length; i++)
            {
                GameObject h = Instantiate(hazardPrefab, hazardPos[i], Quaternion.identity);
                spawnedHazards.Add(h);
            }
        }

        // Выход: меняем позицию
        if (exitTransform != null && levelIndex < ExitPositions.Length)
            exitTransform.position = ExitPositions[levelIndex];

        player.ReturnToStart();
        hud.ShowHint(profile.goal, false);
        Refresh();
    }

    public void StartGame() { StartLevel(1); }
    public void RestartGame() { StartLevel(CurrentLevel); }

    public void NextLevel()
    {
        if (CurrentLevel < 3)
        {
            StartLevel(CurrentLevel + 1);
        }
        else
        {
            GoToMenu();
        }
    }
    public void ResumeGame() { IsPlaying = true; screens.ShowGame(); }
    public void PauseGame() { IsPlaying = false; MobileDirection = Vector2.zero; screens.ShowPause(); }
    public void GoToMenu()
    {
        CurrentLevel = SaveService.Unlocked;
        SelectVariant(LevelVariantIndices[CurrentLevel - 1]);
        ResetState();
        screens.ShowMainMenu();
    }

    public void SetDirection(Vector2 direction)
    {
        MobileDirection = IsPlaying ? direction : Vector2.zero;
    }

    public void Collect(Collectible item)
    {
        if (!IsPlaying || !item.gameObject.activeSelf) return;
        int type = item.index % profile.target;
        bool wrong = profile.mode == 0 && item.index >= profile.target
            || profile.mode == 1 && item.index != progress
            || (profile.mode == 2 || profile.mode == 3) && cargo >= profile.capacity
            || (profile.mode == 4 || profile.mode == 5) && flags[type];
        if (wrong) { Warn("Действие не принято: проверьте порядок, тип или вместимость."); return; }
        if (profile.mode == 2 || profile.mode == 3) cargo++;
        else { flags[type] = true; progress++; }
        item.gameObject.SetActive(false);
        audioFeedback.PlayCollect();
        hud.ShowHint("Принято. " + Status(), false);
        Refresh();
    }

    public void TryFinishLevel()
    {
        if (!IsPlaying) return;
        if (profile.mode == 2 || profile.mode == 3)
        {
            if (cargo == 0) { Warn("Сначала возьмите груз."); return; }
            progress += cargo;
            cargo = 0;
            hud.ShowHint("Груз доставлен. " + Status(), false);
            Refresh();
            if (progress < profile.target) { audioFeedback.PlayCollect(); return; }
        }
        if (progress >= profile.target) Finish(true);
        else Warn("Цель ещё не достигнута. " + Status());
    }

    public void HitHazard()
    {
        if (!IsPlaying || Time.time < immunity) return;
        lives--;
        immunity = Time.time + 1f;
        player.ReturnToStart();
        if (lives == 0) Finish(false);
        else { Warn("Повреждение: потеряна жизнь."); Refresh(); }
    }

    private void Warn(string message)
    {
        audioFeedback.PlayHazard();
        hud.ShowHint(message, true);
    }

    private string Status()
    {
        string text = profile.progressLabel + ": " + progress + "/" + profile.target;
        if (profile.mode == 1) text += " · следующий №" + Mathf.Min(progress + 1, profile.target);
        if (profile.mode == 2 || profile.mode == 3) text += " · груз " + cargo + "/" + profile.capacity;
        if (profile.mode == 4 || profile.mode == 5)
        {
            text += " · ";
            for (int i = 0; i < profile.target; i++)
                text += (flags[i] ? "[✓" : "[ ") + (i + 1) + "] ";
        }
        if (profile.resource) text += " · ресурс " + Mathf.CeilToInt(remaining);
        return text;
    }

    private void Refresh()
    {
        hud.ShowProgress(profile.title, Status(), lives);
        for (int i = 0; i < gateIndicators.Length; i++)
        {
            gateIndicators[i].gameObject.SetActive(profile.mode == 5 && i < profile.target);
            gateIndicators[i].color = flags[i] ? theme.success : theme.danger;
        }
    }

    private void Finish(bool won)
    {
        Won = won;
        IsPlaying = false;
        MobileDirection = Vector2.zero;
        if (won)
        {
            int score = Mathf.Max(1, 1000 + lives * 100);
            SaveService.Complete(CurrentLevel, score);
            audioFeedback.PlaySuccess();
        }
        else
        {
            audioFeedback.PlayFailure();
        }
        hud.ShowResult(won, CurrentLevel);
        screens.ShowResult();
    }
}