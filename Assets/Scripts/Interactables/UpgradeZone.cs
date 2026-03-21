using UnityEngine;
using TMPro;

public class UpgradeZone : MonoBehaviour
{
    [Tooltip("0 = 곡괭이→드릴, 1 = 드릴→불도저")]
    [SerializeField] private int targetToolLevel;

    [Header("World Space UI")]
    [SerializeField] private TextMeshPro costText;

    [Header("Visual Fill Bar")]
    [Tooltip("채워질 막대 오브젝트 (자식 Cube). pivot이 center이므로 localPosition.y도 함께 조정")]
    [SerializeField] private Transform fillBar;
    [SerializeField] private float maxFillHeight = 4f;

    private int paidAmount = 0;
    private int requiredCost;
    private bool upgradeCompleted = false;

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        if (targetToolLevel < s.toolUpgradeCosts.Length)
            requiredCost = s.toolUpgradeCosts[targetToolLevel];

        if (fillBar != null)
        {
            Vector3 sc = fillBar.localScale;
            fillBar.localScale = new Vector3(sc.x, 0.001f, sc.z);
            fillBar.localPosition = new Vector3(fillBar.localPosition.x, 0f, fillBar.localPosition.z);
        }

        RefreshUI();
    }

    public void TryContribute(PlayerToolManager toolManager, PlayerStackManager stackManager)
    {
        if (upgradeCompleted) return;
        if (toolManager == null || toolManager.CurrentLevel != targetToolLevel) return;
        if (!stackManager.HasItemOfType(ItemType.Cash)) return;

        if (paidAmount >= requiredCost)
        {
            ExecuteUpgrade(toolManager);
            return;
        }

        StackItem cashItem = stackManager.RemoveTopItemOfType(ItemType.Cash);
        if (cashItem == null) return;
        Destroy(cashItem.gameObject);

        int cashValue = GameManager.Instance.Settings.cashPerPrisoner;
        CurrencyManager.Instance.SpendCash(cashValue);

        paidAmount += cashValue;
        RefreshUI();
        RefreshFillBar();

        if (paidAmount >= requiredCost)
            ExecuteUpgrade(toolManager);
    }

    private void ExecuteUpgrade(PlayerToolManager toolManager)
    {
        upgradeCompleted = true;
        toolManager.ForceUpgrade();

        if (costText != null)
            costText.text = "DONE!";

        if (fillBar != null)
        {
            Vector3 sc = fillBar.localScale;
            fillBar.localScale = new Vector3(sc.x, maxFillHeight, sc.z);
            fillBar.localPosition = new Vector3(
                fillBar.localPosition.x, maxFillHeight * 0.5f, fillBar.localPosition.z);
        }
    }

    private void RefreshUI()
    {
        if (costText == null) return;
        int remaining = requiredCost - paidAmount;
        costText.text = remaining > 0 ? $"${remaining}" : "DONE!";
    }

    private void RefreshFillBar()
    {
        if (fillBar == null || requiredCost <= 0) return;

        float ratio = Mathf.Clamp01((float)paidAmount / requiredCost);
        float newHeight = Mathf.Max(ratio * maxFillHeight, 0.001f);

        Vector3 sc = fillBar.localScale;
        fillBar.localScale = new Vector3(sc.x, newHeight, sc.z);
        fillBar.localPosition = new Vector3(
            fillBar.localPosition.x, newHeight * 0.5f, fillBar.localPosition.z);
    }
}
