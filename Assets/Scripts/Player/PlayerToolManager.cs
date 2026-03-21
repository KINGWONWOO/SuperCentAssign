using UnityEngine;

public class PlayerToolManager : MonoBehaviour
{
    [Tooltip("0=곡괭이, 1=전동드릴, 2=불도저 모델 오브젝트")]
    [SerializeField] private GameObject[] toolModels;

    private int currentLevel = 0;

    public int CurrentLevel => currentLevel;
    public bool IsMaxLevel => currentLevel >= toolModels.Length - 1;

    void Start()
    {
        ApplyToolModel(currentLevel);
    }

    public float GetMiningInterval()
    {
        GameSettings s = GameManager.Instance.Settings;
        int idx = Mathf.Clamp(currentLevel, 0, s.miningIntervals.Length - 1);
        return s.miningIntervals[idx];
    }

    public int GetUpgradeCost()
    {
        if (IsMaxLevel) return -1;
        GameSettings s = GameManager.Instance.Settings;
        if (currentLevel < s.toolUpgradeCosts.Length)
            return s.toolUpgradeCosts[currentLevel];
        return -1;
    }

    public bool TryUpgrade()
    {
        if (IsMaxLevel) return false;

        int cost = GetUpgradeCost();
        if (!CurrencyManager.Instance.SpendCash(cost)) return false;

        currentLevel++;
        ApplyToolModel(currentLevel);
        return true;
    }

    // UpgradeZone이 비용을 직접 처리할 때 사용 (비용 재차감 없이 레벨만 올림)
    public void ForceUpgrade()
    {
        if (IsMaxLevel) return;
        currentLevel++;
        ApplyToolModel(currentLevel);
    }

    private void ApplyToolModel(int level)
    {
        for (int i = 0; i < toolModels.Length; i++)
        {
            if (toolModels[i] != null)
                toolModels[i].SetActive(i == level);
        }
    }
}
