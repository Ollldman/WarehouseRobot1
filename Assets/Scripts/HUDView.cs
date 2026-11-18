using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    public Text titleText, progressText, livesText, hintText;
    public Text resultTitle, resultMessage, menuTitle, menuGoal;

    public UnityEngine.UI.Button nextLevelButton;

    public void ShowProgress(string title, string progress, int lives)
    {
        titleText.text = title;
        progressText.text = progress;
        livesText.text = "Жизни: " + lives;
    }

    public void ShowHint(string message, bool warning)
    {
        hintText.text = (warning ? "! " : "✓ ") + message;
        hintText.color = warning ? new Color(1f, 0.5f, 0.4f) : Color.white;
    }

    public void SetGoal(string title, string goal)
    {
        menuTitle.text = title;
        menuGoal.text = goal;
    }

    public void ShowResult(bool won, int level)
    {
        resultTitle.text = won ? "УСПЕХ ✓" : "ПОПРОБУЙТЕ ЕЩЁ !";
        if (won)
        {
            if (level == 3)
                resultMessage.text = "Все уровни пройдены! Рекорд: " + SaveService.Best(level);
            else
                resultMessage.text = "Уровень " + level + " пройден. Рекорд: " + SaveService.Best(level);
        }
        else
        {
            resultMessage.text = "Попробуйте ещё раз.";
        }
        // Показываем кнопку "Следующий уровень" только при победе и если это не 3-й уровень
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(won && level < 3);
        }
    }
}