using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuUIController : MonoBehaviour
{
    public Button[] levelButtons;
    public TMP_Text[] levelLabels;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (levelButtons == null) return;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool unlocked = (i + 1) <= SaveService.Unlocked;
            levelButtons[i].interactable = unlocked;
            if (levelLabels != null && i < levelLabels.Length && levelLabels[i] != null)
            {
                int best = SaveService.Best(i + 1);
                levelLabels[i].text = unlocked
                    ? "Уровень " + (i + 1) + (best > 0 ? "  ·  рекорд " + best : "")
                    : "Уровень " + (i + 1) + "  ·  закрыто";
            }
        }
    }
}