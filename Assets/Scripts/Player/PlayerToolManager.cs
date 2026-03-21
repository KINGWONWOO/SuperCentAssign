using UnityEngine;

public class PlayerToolManager : MonoBehaviour
{
    [Tooltip("0=곡괭이, 1=드릴, 2=드릴 차 모델 오브젝트")]
    [SerializeField] private GameObject[] toolModels;

    private int currentLevel = 0;

    public ToolLevel CurrentLevel => (ToolLevel)currentLevel;
    public int CurrentLevelInt => currentLevel;
    public bool IsMaxLevel => currentLevel >= 2;

    void Start()
    {
        ApplyToolModel(currentLevel);
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, 2);
        ApplyToolModel(currentLevel);

        // 최대 광석 스택 갱신
        PlayerStackManager psm = GetComponent<PlayerStackManager>();
        if (psm != null)
        {
            int[] maxStacks = GameManager.Instance.Settings.maxOreStacks;
            int idx = Mathf.Clamp(currentLevel, 0, maxStacks.Length - 1);
            psm.SetMaxOreStack(maxStacks[idx]);
        }
    }

    public float GetMiningInterval()
    {
        GameSettings s = GameManager.Instance.Settings;
        int idx = Mathf.Clamp(currentLevel, 0, s.miningIntervals.Length - 1);
        return s.miningIntervals[idx];
    }

    public int GetMiningWidth()
    {
        GameSettings s = GameManager.Instance.Settings;
        int idx = Mathf.Clamp(currentLevel, 0, s.miningWidths.Length - 1);
        return s.miningWidths[idx];
    }

    private void ApplyToolModel(int level)
    {
        for (int i = 0; i < toolModels.Length; i++)
            if (toolModels[i] != null)
                toolModels[i].SetActive(i == level);
    }
}
