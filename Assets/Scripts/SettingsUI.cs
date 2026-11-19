using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    public TMP_Text volumeText;

    private void OnEnable()
    {
        Refresh();
    }

    public void IncreaseVolume()
    {
        SaveService.SetVolume(SaveService.Volume + 0.1f);
        Refresh();
    }

    public void DecreaseVolume()
    {
        SaveService.SetVolume(SaveService.Volume - 0.1f);
        Refresh();
    }

    public void BackToMenu()
    {
        GameManager.Instance.GoToMenu();
    }

    private void Refresh()
    {
        if (volumeText != null)
            volumeText.text = "Громкость: " + Mathf.RoundToInt(SaveService.Volume * 100) + "%";
    }
}