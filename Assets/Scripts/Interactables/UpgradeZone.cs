using UnityEngine;
using TMPro;

// 5종 업그레이드 발판.
// Drill / DrillCar / WorkerHire / AutoSell / PrisonExpand
public class UpgradeZone : MonoBehaviour
{
    [SerializeField] private UpgradeType upgradeType;

    [Header("World Space UI")]
    [SerializeField] private TextMeshPro costText;

    [Header("Visual Fill Bar")]
    [SerializeField] private Transform fillBar;
    [SerializeField] private float maxFillHeight = 4f;

    [Header("References")]
    [SerializeField] private MiningGrid miningGrid;
    [SerializeField] private CellManager cellManager;

    [Header("Prefabs (WorkerHire / AutoSell)")]
    [SerializeField] private GameObject workerNpcPrefab;
    [SerializeField] private GameObject autoSellNpcPrefab;

    [Header("WorkerHire 스폰 위치")]
    [SerializeField] private Transform[] workerSpawnPoints;

    [Header("AutoSell NPC 스폰 위치")]
    [SerializeField] private Transform autoSellSpawnPoint;

    [Header("업그레이드 완료 시 활성화할 오브젝트들")]
    [SerializeField] private GameObject[] objectsToActivateOnComplete;

    private int paidAmount = 0;
    private int requiredCost;
    private bool upgradeCompleted = false;
    private float lastContributeTime = -999f;
    private const float contributeInterval = 0.5f;

    void Start()
    {
        requiredCost = GetCost();
        InitFillBar();
        RefreshUI();
    }

    private int GetCost()
    {
        GameSettings s = GameManager.Instance.Settings;
        return upgradeType switch
        {
            UpgradeType.Drill => s.drillCost,
            UpgradeType.DrillCar => s.drillCarCost,
            UpgradeType.WorkerHire => s.workerHireCost,
            UpgradeType.AutoSell => s.autoSellCost,
            UpgradeType.PrisonExpand => s.prisonExpandCost,
            _ => 0
        };
    }

    public void TryContribute(PlayerToolManager toolManager, PlayerStackManager stackManager)
    {
        if (upgradeCompleted) return;
        if (!MeetsPrerequisite(toolManager)) return;
        if (!stackManager.HasItemOfType(ItemType.Cash)) return;
        if (Time.time - lastContributeTime < contributeInterval) return;

        if (paidAmount >= requiredCost)
        {
            ExecuteUpgrade(toolManager);
            return;
        }

        StackItem cashItem = stackManager.RemoveTopItemOfType(ItemType.Cash);
        if (cashItem == null) return;

        int cashValue = GameManager.Instance.Settings.cashPerHandcuff; // 10원 단위
        CurrencyManager.Instance.SpendCash(cashValue);
        Destroy(cashItem.gameObject);

        paidAmount += cashValue;
        lastContributeTime = Time.time;
        RefreshUI();
        RefreshFillBar();

        if (paidAmount >= requiredCost)
            ExecuteUpgrade(toolManager);
    }

    private bool MeetsPrerequisite(PlayerToolManager toolManager)
    {
        if (toolManager == null) return false;
        int level = toolManager.CurrentLevelInt;
        return upgradeType switch
        {
            UpgradeType.Drill => level == 0,
            UpgradeType.DrillCar => level >= 1,
            UpgradeType.WorkerHire => level >= 1,
            UpgradeType.AutoSell => level >= 1,
            UpgradeType.PrisonExpand => level >= 2,
            _ => false
        };
    }

    private void ExecuteUpgrade(PlayerToolManager toolManager)
    {
        upgradeCompleted = true;

        switch (upgradeType)
        {
            case UpgradeType.Drill:
                toolManager.SetLevel(1);
                break;
            case UpgradeType.DrillCar:
                toolManager.SetLevel(2);
                break;
            case UpgradeType.WorkerHire:
                SpawnWorkers();
                break;
            case UpgradeType.AutoSell:
                SpawnAutoSeller();
                break;
            case UpgradeType.PrisonExpand:
                GameSettings s = GameManager.Instance.Settings;
                cellManager?.ExpandCapacity(s.expandedCellCapacity - s.defaultCellCapacity);
                break;
        }

        foreach (var obj in objectsToActivateOnComplete)
            if (obj != null) obj.SetActive(true);

        // 업그레이드 완료 → 발판 비활성화 (잠시 "DONE!" 보여준 뒤 사라짐)
        if (costText != null) costText.text = "DONE!";
        SetFillBarFull();
        StartCoroutine(HideAfterDelay(1.2f));
    }

    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private void SpawnWorkers()
    {
        if (workerNpcPrefab == null || miningGrid == null) return;

        // 7열 그리드에서 아래쪽 3열(col 4, 5, 6)을 각 워커가 담당
        int[] cols = { 4, 5, 6 };
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = workerSpawnPoints != null && i < workerSpawnPoints.Length
                ? workerSpawnPoints[i].position
                : miningGrid.GetNodeWorldPos(cols[i], 0);

            GameObject obj = Instantiate(workerNpcPrefab, spawnPos, Quaternion.identity);
            WorkerNPC w = obj.GetComponent<WorkerNPC>();
            if (w == null) w = obj.AddComponent<WorkerNPC>();
            w.Initialize(miningGrid, cols[i]);
        }
    }

    private void SpawnAutoSeller()
    {
        if (autoSellNpcPrefab == null) return;
        Vector3 spawnPos = autoSellSpawnPoint != null
            ? autoSellSpawnPoint.position : transform.position;
        Instantiate(autoSellNpcPrefab, spawnPos, Quaternion.identity);
    }

    private void InitFillBar()
    {
        if (fillBar == null) return;
        Vector3 sc = fillBar.localScale;
        fillBar.localScale = new Vector3(sc.x, 0.001f, sc.z);
        fillBar.localPosition = new Vector3(fillBar.localPosition.x, 0f, fillBar.localPosition.z);
    }

    private void SetFillBarFull()
    {
        if (fillBar == null) return;
        Vector3 sc = fillBar.localScale;
        fillBar.localScale = new Vector3(sc.x, maxFillHeight, sc.z);
        fillBar.localPosition = new Vector3(
            fillBar.localPosition.x, maxFillHeight * 0.5f, fillBar.localPosition.z);
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
