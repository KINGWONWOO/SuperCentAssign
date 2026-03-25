using UnityEngine;
using System.Collections;

// Player 오브젝트에 부착.
// CharacterController + Rigidbody(isKinematic=true) + SphereCollider(isTrigger=true) 필요.
public class PlayerInteraction : MonoBehaviour
{
    private PlayerStackManager stackManager;
    private PlayerAnimation playerAnimation;
    private PlayerToolManager toolManager;
    private MiningAnimNotifier miningNotifier;

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
        stackManager    = GetComponent<PlayerStackManager>();
        playerAnimation = GetComponent<PlayerAnimation>();
        toolManager     = GetComponent<PlayerToolManager>();

        // MiningAnimNotifier: Animator가 있는 Visual 자식 GO에 부착
        var animGO = GetComponentInChildren<Animator>(true)?.gameObject;
        if (animGO != null)
        {
            miningNotifier = animGO.GetComponent<MiningAnimNotifier>();
            if (miningNotifier == null)
                miningNotifier = animGO.AddComponent<MiningAnimNotifier>();
            miningNotifier.OnMiningImpact += OnPickaxeImpact;
        }
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
        playerAnimation?.SetDrilling(false);
        playerAnimation?.SetBulldozerMode(false);
        UpdateDrillVisual(false);
        UpdatePickaxeVisual(false);
    }

    private IEnumerator MiningRoutine()
    {
        ToolLevel currentLevel = toolManager != null ? toolManager.CurrentLevel : ToolLevel.Pickaxe;

        if (currentLevel == ToolLevel.DrillCar)
        {
            playerAnimation?.SetBulldozerMode(true);
        }
        else if (currentLevel == ToolLevel.Drill)
        {
            playerAnimation?.SetDrilling(true);
            UpdateDrillVisual(true);
        }
        else
        {
            playerAnimation?.SetMining(true);
            UpdatePickaxeVisual(true);
        }

        while (currentMiningGrid != null)
        {
            int width    = toolManager != null ? toolManager.GetMiningWidth() : 1;
            float interval = toolManager != null ? toolManager.GetMiningInterval() : 1f;

            if (currentLevel == ToolLevel.Pickaxe)
            {
                // 곡괭이: MiningAnimNotifier 이벤트로 채굴 (OnPickaxeImpact)
                yield return new WaitForSeconds(interval > 0f ? interval : 0.1f);
            }
            else
            {
                // 드릴/불도저: 타이머 기반 연속 채굴
                float wait = interval <= 0f ? 0.1f : interval;
                yield return new WaitForSeconds(wait);

                if (currentMiningGrid != null)
                {
                    float fwdOffset = toolManager != null ? toolManager.GetMiningForwardOffset() : 0f;
                    float spacing = GameManager.Instance?.Settings.gridSpacingZ ?? 1.2f;
                    Vector3 minePos = transform.position + transform.forward * (spacing + fwdOffset);
                    currentMiningGrid.MineAt(minePos, width, stackManager);
                }
            }
        }

        playerAnimation?.SetMining(false);
        playerAnimation?.SetDrilling(false);
        playerAnimation?.SetBulldozerMode(false);
        UpdateDrillVisual(false);
        UpdatePickaxeVisual(false);
    }

    // 곡괭이 애니메이션 임팩트 타이밍(~69% normalizedTime)에 호출됨
    private void OnPickaxeImpact()
    {
        if (currentMiningGrid == null) return;
        if (toolManager != null && toolManager.CurrentLevel != ToolLevel.Pickaxe) return;
        DoMine();
    }

    private void DoMine()
    {
        if (currentMiningGrid == null) return;
        int width = toolManager != null ? toolManager.GetMiningWidth() : 1;
        float spacing = GameManager.Instance?.Settings.gridSpacingZ ?? 1.2f;
        Vector3 minePos = transform.position + transform.forward * spacing;
        currentMiningGrid.MineAt(minePos, width, stackManager);
    }

    private void UpdateDrillVisual(bool isVisible)
    {
        if (toolManager == null || toolManager.drillVisualObject == null) return;
        if (toolManager.CurrentLevel == ToolLevel.Drill)
            toolManager.drillVisualObject.SetActive(isVisible);
        else
            toolManager.drillVisualObject.SetActive(false);
    }

    private void UpdatePickaxeVisual(bool isVisible)
    {
        if (toolManager == null) return;
        if (toolManager.CurrentLevel == ToolLevel.Pickaxe)
            playerAnimation?.SetPickaxeVisible(isVisible);
        else
            playerAnimation?.SetPickaxeVisible(false);
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
