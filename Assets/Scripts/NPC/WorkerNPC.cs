using UnityEngine;
using System.Collections;

// 자동 채굴 NPC.
// 그리드 밖(업그레이드존 근처)에서 스폰 → 그리드 row0부터 끝까지 왕복하며 채굴.
// 각 행에 도달 후 돌이 있으면 채굴, 없으면 다음 행으로 이동.
// 채굴 속도는 플레이어의 2배 (animator.speed = 2f).
public class WorkerNPC : MonoBehaviour
{
    private MiningGrid miningGrid;
    private DropZone oreDropZone;
    private int assignedCol;
    private float moveSpeed;

    private Animator animator;
    private MiningAnimNotifier miningNotifier;
    private Transform rightHandBone;
    private bool movingForward = true;
    private int currentRow = 0;
    private bool impactFired = false;
    private RockNode pendingNode = null;

    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int IsMiningHash   = Animator.StringToHash("IsMining");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");

    [Header("곡괭이 손 부착 오프셋")]
    [SerializeField] private Transform pickaxeModel;
    [SerializeField] private Vector3 pickaxeLocalPos   = new Vector3(0f, 0.1f, 0.05f);
    [SerializeField] private Vector3 pickaxeLocalRot   = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 pickaxeLocalScale = new Vector3(1f, 1f, 1f);

    public void Initialize(MiningGrid grid, int col, Vector3 spawnOutsidePos)
    {
        miningGrid  = grid;
        assignedCol = col;

        // OreDropZone 자동 탐색
        foreach (var dz in FindObjectsOfType<DropZone>())
        {
            if (dz.gameObject.name.ToLower().Contains("ore") ||
                dz.gameObject.name.ToLower().Contains("drop"))
            { oreDropZone = dz; break; }
        }
        if (oreDropZone == null)
        {
            var go = GameObject.Find("OreDropZone");
            if (go != null) oreDropZone = go.GetComponent<DropZone>();
        }

        GameSettings s = GameManager.Instance.Settings;
        moveSpeed = s.workerMoveSpeed;

        // Animator (2배 속도) + MiningAnimNotifier
        animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.speed = 1f;
            miningNotifier = animator.gameObject.GetComponent<MiningAnimNotifier>();
            if (miningNotifier == null)
                miningNotifier = animator.gameObject.AddComponent<MiningAnimNotifier>();
            miningNotifier.OnMiningImpact += OnMiningImpact;
        }

        // 곡괭이 손 부착 (뼈가 없으면 무시)
        rightHandBone = FindBoneRecursive(transform, "mixamorig:RightHand");
        AttachPickaxeToHand();

        // 그리드 밖 위치에서 시작
        transform.position = spawnOutsidePos;
        currentRow = 0;

        StartCoroutine(WorkRoutine());
    }

    // 이전 호환용 (spawnPos 없이 호출 시 row0 앞에서 시작)
    public void Initialize(MiningGrid grid, int col)
    {
        Vector3 row0 = grid.GetNodeWorldPos(col, 0);
        // 그리드 앞 방향의 반대(입구쪽)로 2칸
        Vector3 before = row0 - grid.transform.forward * (GameManager.Instance.Settings.gridSpacingZ * 2f);
        Initialize(grid, col, before);
    }

    private IEnumerator WorkRoutine()
    {
        while (true)
        {
            int totalRows = miningGrid.Rows;
            RockNode node = miningGrid.GetActiveNodeInRow(currentRow, assignedCol);

            if (node != null && node.IsActive)
            {
                // 돌에서 1.8칸 앞에 정지 (대형 캐릭터 스케일 고려)
                Vector3 rockPos = miningGrid.GetNodeWorldPos(assignedCol, currentRow);
                float spacing = GameManager.Instance?.Settings.gridSpacingZ ?? 1.2f;
                Vector3 approachDir = movingForward
                    ? miningGrid.transform.forward
                    : -miningGrid.transform.forward;
                Vector3 stopPos = rockPos - approachDir * (spacing * 1.8f);

                // stopPos로 이동 — 이동 중 Speed=1 유지
                if (Vector3.Distance(transform.position, stopPos) > 0.15f)
                {
                    animator?.SetFloat(SpeedHash, 1f);
                    while (Vector3.Distance(transform.position, stopPos) > 0.15f)
                    {
                        transform.position = Vector3.MoveTowards(
                            transform.position, stopPos, moveSpeed * Time.deltaTime);
                        FaceTarget(stopPos);
                        yield return null;
                    }
                    transform.position = stopPos;
                }
                animator?.SetFloat(SpeedHash, 0f);

                // 돌 방향으로 회전 후 채굴
                FaceTarget(rockPos);

                pendingNode = node;
                animator?.SetBool(IsMiningHash, true);
                SetPickaxeVisible(true);

                // Mining 상태에 실제 진입할 때까지 대기
                impactFired = false;
                float mineTimer = 0f;
                const float mineTimeout = 4f;
                yield return new WaitUntil(() =>
                    animator == null ||
                    (!animator.IsInTransition(0) &&
                     animator.GetCurrentAnimatorStateInfo(0).shortNameHash == MiningStateHash));

                // Mining 상태에서 전체 1회 재생 대기
                while (mineTimer < mineTimeout)
                {
                    mineTimer += Time.deltaTime;
                    if (animator != null && !animator.IsInTransition(0) &&
                        animator.GetCurrentAnimatorStateInfo(0).shortNameHash == MiningStateHash)
                    {
                        float norm = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                        if (!impactFired && norm >= 0.69f)
                            OnMiningImpact();
                        if (norm >= 0.97f) break;
                    }
                    yield return null;
                }
                if (!impactFired) OnMiningImpact();

                animator?.SetBool(IsMiningHash, false);
                SetPickaxeVisible(false);
                pendingNode = null;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // 돌 없음 → 이동 없이 row 카운터만 증가 (이동은 다음 돌의 stopPos로 자연스럽게)
                int nextRow = movingForward ? currentRow + 1 : currentRow - 1;
                if (nextRow >= totalRows)      { movingForward = false; nextRow = currentRow - 1; }
                else if (nextRow < 0)          { movingForward = true;  nextRow = currentRow + 1; }
                currentRow = nextRow;
                yield return null;
            }
        }
    }

    private void OnMiningImpact()
    {
        if (pendingNode == null || !pendingNode.IsActive) return;
        bool mined = pendingNode.MineForWorker();
        if (mined && oreDropZone != null)
            oreDropZone.WorkerDeliverOre(miningGrid.OrePrefab);
        impactFired = true;
    }

    private void AttachPickaxeToHand()
    {
        if (rightHandBone == null || pickaxeModel == null) return;
        pickaxeModel.SetParent(rightHandBone, false);
        pickaxeModel.localPosition    = pickaxeLocalPos;
        pickaxeModel.localEulerAngles = pickaxeLocalRot;
        pickaxeModel.localScale       = pickaxeLocalScale;
        pickaxeModel.gameObject.SetActive(false);
    }

    private void SetPickaxeVisible(bool visible)
    {
        if (pickaxeModel != null) pickaxeModel.gameObject.SetActive(visible);
    }

    private Transform FindBoneRecursive(Transform root, string boneName)
    {
        if (root.name == boneName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindBoneRecursive(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
