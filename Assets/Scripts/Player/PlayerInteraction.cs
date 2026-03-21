using UnityEngine;
using System.Collections;

// Player 오브젝트에 부착.
// CharacterController + Rigidbody(isKinematic=true) + SphereCollider(isTrigger=true) 필요.
public class PlayerInteraction : MonoBehaviour
{
    private PlayerStackManager stackManager;
    private PlayerAnimation playerAnimation;
    private PlayerToolManager toolManager;

    private MiningGrid currentMiningGrid;
    private DropZone currentDropZone;
    private UpgradeZone currentUpgradeZone;
    private DeskZone currentDeskZone;

    private Coroutine miningCoroutine;
    private Coroutine dropCoroutine;
    private Coroutine upgradeCoroutine;
    private Coroutine deskCoroutine;

    void Awake()
    {
        stackManager = GetComponent<PlayerStackManager>();
        playerAnimation = GetComponent<PlayerAnimation>();
        toolManager = GetComponent<PlayerToolManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryStartMining(other.GetComponent<MiningGrid>());
        TryStartDrop(other.GetComponent<DropZone>());
        TryStartUpgrade(other.GetComponent<UpgradeZone>());
        TryStartDesk(other.GetComponent<DeskZone>());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<MiningGrid>() != null) StopMining();
        if (other.GetComponent<DropZone>() != null) StopDrop();
        if (other.GetComponent<UpgradeZone>() != null) StopUpgrade();
        if (other.GetComponent<DeskZone>() != null) StopDesk();
    }

    // ─── Mining Grid ──────────────────────────────────────────────

    private void TryStartMining(MiningGrid grid)
    {
        if (grid == null) return;
        currentMiningGrid = grid;
        if (miningCoroutine != null) StopCoroutine(miningCoroutine);
        miningCoroutine = StartCoroutine(MiningRoutine());
    }

    private void StopMining()
    {
        currentMiningGrid = null;
        if (miningCoroutine != null) { StopCoroutine(miningCoroutine); miningCoroutine = null; }
        playerAnimation?.SetMining(false);
        // DrillCar 탑승 모드 해제
        if (toolManager != null && toolManager.CurrentLevel == ToolLevel.DrillCar)
            playerAnimation?.SetInVehicle(false);
    }

    private IEnumerator MiningRoutine()
    {
        bool isDrillCar = toolManager != null && toolManager.CurrentLevel == ToolLevel.DrillCar;
        if (isDrillCar) playerAnimation?.SetInVehicle(true);
        else playerAnimation?.SetMining(true);

        while (currentMiningGrid != null)
        {
            int width = toolManager != null ? toolManager.GetMiningWidth() : 1;
            float interval = toolManager != null ? toolManager.GetMiningInterval() : 1f;

            // 채굴 실행 (스택 가득 찼어도 돌은 사라짐)
            currentMiningGrid.MineAt(transform.position, width, stackManager);

            if (interval <= 0f)
                yield return new WaitForSeconds(0.1f); // 드릴/드릴 차: 0.1s 폴링
            else
                yield return new WaitForSeconds(interval);
        }

        playerAnimation?.SetMining(false);
        if (isDrillCar || (toolManager != null && toolManager.CurrentLevel == ToolLevel.DrillCar))
            playerAnimation?.SetInVehicle(false);
    }

    // ─── Drop Zone ────────────────────────────────────────────────

    private void TryStartDrop(DropZone dz)
    {
        if (dz == null) return;
        currentDropZone = dz;
        if (dropCoroutine != null) StopCoroutine(dropCoroutine);
        dropCoroutine = StartCoroutine(DropRoutine(dz));
    }

    private void StopDrop()
    {
        currentDropZone = null;
        if (dropCoroutine != null) { StopCoroutine(dropCoroutine); dropCoroutine = null; }
    }

    private IEnumerator DropRoutine(DropZone dz)
    {
        float interval = GameManager.Instance.Settings.dropInterval;
        while (currentDropZone == dz)
        {
            dz.ProcessTransfer(stackManager);
            yield return new WaitForSeconds(interval);
        }
    }

    // ─── Upgrade Zone ─────────────────────────────────────────────

    private void TryStartUpgrade(UpgradeZone uz)
    {
        if (uz == null) return;
        currentUpgradeZone = uz;
        if (upgradeCoroutine != null) StopCoroutine(upgradeCoroutine);
        upgradeCoroutine = StartCoroutine(UpgradeRoutine(uz));
    }

    private void StopUpgrade()
    {
        currentUpgradeZone = null;
        if (upgradeCoroutine != null) { StopCoroutine(upgradeCoroutine); upgradeCoroutine = null; }
    }

    private IEnumerator UpgradeRoutine(UpgradeZone uz)
    {
        while (currentUpgradeZone == uz)
        {
            uz.TryContribute(toolManager, stackManager);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ─── Desk Zone ────────────────────────────────────────────────

    private void TryStartDesk(DeskZone dz)
    {
        if (dz == null) return;
        currentDeskZone = dz;
        if (deskCoroutine != null) StopCoroutine(deskCoroutine);
        deskCoroutine = StartCoroutine(DeskRoutine(dz));
    }

    private void StopDesk()
    {
        currentDeskZone = null;
        if (deskCoroutine != null) { StopCoroutine(deskCoroutine); deskCoroutine = null; }
    }

    private IEnumerator DeskRoutine(DeskZone dz)
    {
        float interval = GameManager.Instance.Settings.dropInterval;
        while (currentDeskZone == dz)
        {
            dz.TransferHandcuff(stackManager);
            yield return new WaitForSeconds(interval);
        }
    }
}
