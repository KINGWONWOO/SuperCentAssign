using UnityEngine;
using System.Collections;

// 자동 채굴 NPC. 지정된 열(X)을 고정하고 20개 행(Z)을 왕복하며 채굴.
// 플레이어와 동일한 MiningAnimNotifier 기반 채굴 타이밍 사용.
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

    private static readonly int SpeedHash    = Animator.StringToHash("Speed");
    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");

    [Header("곡괭이 손 부착 오프셋")]
    [SerializeField] private Transform pickaxeModel;
    [SerializeField] private Vector3 pickaxeLocalPos   = new Vector3(0f, 0.1f, 0.05f);
    [SerializeField] private Vector3 pickaxeLocalRot   = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 pickaxeLocalScale = new Vector3(1f, 1f, 1f);

    public void Initialize(MiningGrid grid, int col)
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

        // Animator + MiningAnimNotifier 설정
        animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            miningNotifier = animator.gameObject.GetComponent<MiningAnimNotifier>();
            if (miningNotifier == null)
                miningNotifier = animator.gameObject.AddComponent<MiningAnimNotifier>();
            miningNotifier.OnMiningImpact += OnMiningImpact;
        }

        // 곡괭이 손 부착
        rightHandBone = FindBoneRecursive(transform, "mixamorig:RightHand");
        AttachPickaxeToHand();

        transform.position = miningGrid.GetNodeWorldPos(assignedCol, 0);
        StartCoroutine(WorkRoutine());
    }

    private IEnumerator WorkRoutine()
    {
        while (true)
        {
            int totalRows = miningGrid.Rows;
            RockNode node = miningGrid.GetActiveNodeInRow(currentRow, assignedCol);

            if (node != null && node.IsActive)
            {
                // 돌 앞에 도달 → 채굴 애니메이션 시작
                pendingNode = node;
                animator?.SetFloat(SpeedHash, 0f);
                animator?.SetBool(IsMiningHash, true);
                SetPickaxeVisible(true);

                // MiningAnimNotifier 이벤트로 채굴 (impactFired 플래그로 한 번만)
                impactFired = false;
                yield return new WaitUntil(() => impactFired);
                animator?.SetBool(IsMiningHash, false);
                SetPickaxeVisible(false);
                pendingNode = null;

                // 채굴 후 잠깐 대기 (애니메이션 완료)
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                // 돌 없음 → 이동 (다음 행)
                int nextRow = movingForward ? currentRow + 1 : currentRow - 1;
                if (nextRow >= totalRows) { movingForward = false; nextRow = currentRow - 1; }
                if (nextRow < 0)         { movingForward = true;  nextRow = currentRow + 1; }

                Vector3 targetPos = miningGrid.GetNodeWorldPos(assignedCol, nextRow);
                animator?.SetFloat(SpeedHash, 1f);

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, targetPos, moveSpeed * Time.deltaTime);
                    FaceTarget(targetPos);
                    yield return null;
                }
                transform.position = targetPos;
                animator?.SetFloat(SpeedHash, 0f);
                currentRow = nextRow;
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
