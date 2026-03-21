using UnityEngine;
using System.Collections;

// Player 오브젝트에 부착.
// Player에는 CharacterController + Rigidbody(isKinematic=true) + SphereCollider(isTrigger=true)가 함께 있어야
// OnTriggerEnter/Exit 이벤트가 정상 발동된다.
public class PlayerInteraction : MonoBehaviour
{
    private PlayerStackManager stackManager;
    private PlayerAnimation playerAnimation;
    private PlayerToolManager toolManager;

    private MiningNode currentMiningNode;
    private DropZone currentDropZone;
    private UpgradeZone currentUpgradeZone;
    private ArrestZone currentArrestZone;

    private Coroutine miningCoroutine;
    private Coroutine dropCoroutine;
    private Coroutine upgradeCoroutine;
    private Coroutine arrestCoroutine;

    void Awake()
    {
        stackManager = GetComponent<PlayerStackManager>();
        playerAnimation = GetComponent<PlayerAnimation>();
        toolManager = GetComponent<PlayerToolManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryStartMining(other.GetComponent<MiningNode>());
        TryStartDrop(other.GetComponent<DropZone>());
        TryStartUpgrade(other.GetComponent<UpgradeZone>());
        TryStartArrest(other.GetComponent<ArrestZone>());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<MiningNode>() != null) StopMining();
        if (other.GetComponent<DropZone>() != null) StopDrop();
        if (other.GetComponent<UpgradeZone>() != null) StopUpgrade();
        if (other.GetComponent<ArrestZone>() != null) StopArrest();
    }

    // ─── Mining ───────────────────────────────────────────────────

    private void TryStartMining(MiningNode node)
    {
        if (node == null || !node.IsActive) return;
        currentMiningNode = node;
        if (miningCoroutine != null) StopCoroutine(miningCoroutine);
        miningCoroutine = StartCoroutine(MiningRoutine());
    }

    private void StopMining()
    {
        currentMiningNode = null;
        if (miningCoroutine != null) { StopCoroutine(miningCoroutine); miningCoroutine = null; }
        playerAnimation?.SetMining(false);
    }

    private IEnumerator MiningRoutine()
    {
        while (currentMiningNode != null && currentMiningNode.IsActive)
        {
            if (stackManager.IsFull)
            {
                playerAnimation?.SetMining(false);
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            playerAnimation?.SetMining(true);
            currentMiningNode.Mine(stackManager);

            float interval = toolManager != null
                ? toolManager.GetMiningInterval()
                : GameManager.Instance.Settings.miningIntervals[0];

            yield return new WaitForSeconds(interval);
        }
        playerAnimation?.SetMining(false);
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

    // ─── Arrest Zone ──────────────────────────────────────────────

    private void TryStartArrest(ArrestZone az)
    {
        if (az == null) return;
        currentArrestZone = az;
        if (arrestCoroutine != null) StopCoroutine(arrestCoroutine);
        arrestCoroutine = StartCoroutine(ArrestRoutine(az));
    }

    private void StopArrest()
    {
        currentArrestZone = null;
        if (arrestCoroutine != null) { StopCoroutine(arrestCoroutine); arrestCoroutine = null; }
    }

    private IEnumerator ArrestRoutine(ArrestZone az)
    {
        while (currentArrestZone == az)
        {
            az.TryArrest(stackManager);
            yield return new WaitForSeconds(1f);
        }
    }
}
