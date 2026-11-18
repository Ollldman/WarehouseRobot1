using UnityEngine;

public static class SaveService
{
    private const string Prefix = "WarehouseRobot.v1.";

    public static int Unlocked => Mathf.Clamp(
        PlayerPrefs.GetInt(Prefix + "Unlocked", 1), 1, 3);

    public static int Best(int level) => Mathf.Max(0,
        PlayerPrefs.GetInt(Prefix + "Best" + level, 0));

    public static void Complete(int level, int score)
    {
        PlayerPrefs.SetInt(Prefix + "Unlocked",
            Mathf.Max(Unlocked, Mathf.Min(level + 1, 3)));
        if (score > Best(level))
            PlayerPrefs.SetInt(Prefix + "Best" + level, score);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}