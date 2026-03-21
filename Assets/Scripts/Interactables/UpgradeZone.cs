using UnityEngine;
using TMPro;

public class UpgradeZone : MonoBehaviour
{
    [Header("업그레이드 대상 (업그레이드 발판마다 설정)")]
    [Tooltip("0=곡괭이→드릴, 1=드릴→불도저")]
    [SerializeField] private int targetToolLevel;

    [Header("World Space UI")]
    [SerializeField] private TextMeshPro costText;
    [SerializeField] private GameObject upgradeCompleteSign;

    private int paidAmount = 0;
    private int requiredCost;
    private bool upgradeCompleted = false;

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        if (targetToolLevel < s.toolUpgradeCosts.Length)
            requiredCost = s.toolUpgradeCosts[targetToolLevel];

        RefreshUI();
    }

    public void TryContribute(PlayerToolManager toolManager)
    {
        if (upgradeCompleted) return;
        if (toolManager == null) return;
        if (toolManager.CurrentLevel != targetToolLevel) return;
        if (CurrencyManager.Instance.Cash <= 0) return;

        int remaining = requiredCost - paidAmount;
        if (remaining <= 0)
        {
            ExecuteUpgrade(toolManager);
            return;
        }

        if (CurrencyManager.Instance.SpendCash(1))
        {
            paidAmount++;
            RefreshUI();

            if (paidAmount >= requiredCost)
                ExecuteUpgrade(toolManager);
        }
    }

    private void ExecuteUpgrade(PlayerToolManager toolManager)
    {
        upgradeCompleted = true;

        // CurrencyManager에서 이미 차감됐으므로, TryUpgrade의 내부 SpendCash를 우회
        toolManager.ForceUpgrade();

        if (upgradeCompleteSign != null)
            upgradeCompleteSign.SetActive(true);

        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (costText != null)
        {
            int remaining = requiredCost - paidAmount;
            costText.text = remaining > 0 ? $"${remaining}" : "UPGRADE!";
        }
    }
}
