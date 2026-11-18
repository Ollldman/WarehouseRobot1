using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelData
    {
        public string title;
        public string goal;
        public string progressLabel;
        public int mode;
        public int target;
        public int capacity;
        public bool resource;
        public int hazards;
    }

    public LevelData[] levels = new LevelData[3];
    public int Current { get; private set; }
    public LevelData Data => levels[Current - 1];

    private void Awake()
    {
        LoadLevelsFromJson();
    }

    private void LoadLevelsFromJson()
    {
        TextAsset json = Resources.Load<TextAsset>("profiles");
        if (json == null) return;

        ProfileList list = JsonUtility.FromJson<ProfileList>(json.text);
        for (int i = 0; i < Mathf.Min(3, list.variants.Length); i++)
        {
            levels[i] = new LevelData
            {
                title = list.variants[i].title,
                goal = list.variants[i].goal,
                progressLabel = list.variants[i].progressLabel,
                mode = list.variants[i].mode,
                target = list.variants[i].target,
                capacity = list.variants[i].capacity,
                resource = list.variants[i].resource,
                hazards = 2 + i * 2 // 2, 4, 6 опасностей
            };
        }
    }

    public bool Select(int number)
    {
        if (number < 1 || number > levels.Length || number > SaveService.Unlocked)
            return false;
        Current = number;
        return true;
    }

    [System.Serializable]
    private class Profile
    {
        public string title, goal, progressLabel;
        public int mode, target, capacity;
        public bool resource;
    }

    [System.Serializable]
    private class ProfileList
    {
        public Profile[] variants;
    }
}