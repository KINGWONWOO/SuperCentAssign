using UnityEngine;
using System.Collections;

// 자동 채굴 NPC. 지정된 열(X)을 고정하고 20개 행(Z)을 왕복하며 채굴.
// 채굴된 광석은 OreDropZone(돌 투입구)에 시각적으로 쌓이고 변환기로 자동 전달.
public class WorkerNPC : MonoBehaviour
{
    private MiningGrid miningGrid;
    private DropZone oreDropZone;     // 돌 투입구 (OreToConverter 타입)
    private int assignedCol;          // 고정 열(X) 인덱스
    private float moveSpeed;
    private float mineInterval;

    private bool movingForward = true; // true = row 0→max, false = row max→0
    private int currentRow = 0;

    public void Initialize(MiningGrid grid, int col)
    {
        miningGrid   = grid;
        assignedCol  = col;

        // OreDropZone 자동 탐색
        foreach (var dz in FindObjectsOfType<DropZone>())
        {
            // OreToConverter 타입인 DropZone 찾기 (내부 필드 접근 대신 이름 기반)
            if (dz.gameObject.name.ToLower().Contains("ore") ||
                dz.gameObject.name.ToLower().Contains("drop"))
            {
                oreDropZone = dz;
                break;
            }
        }
        // 이름 기반 탐색 실패 시 GameObject.Find 시도
        if (oreDropZone == null)
        {
            var go = GameObject.Find("OreDropZone");
            if (go != null) oreDropZone = go.GetComponent<DropZone>();
        }

        GameSettings s = GameManager.Instance.Settings;
        moveSpeed    = s.workerMoveSpeed;
        mineInterval = s.workerMineInterval;

        transform.position = miningGrid.GetNodeWorldPos(assignedCol, 0);
        StartCoroutine(WorkRoutine());
    }

    private IEnumerator WorkRoutine()
    {
        while (true)
        {
            int totalRows = miningGrid.Rows;

            // 현재 위치의 노드 채굴 시도
            RockNode node = miningGrid.GetActiveNodeInRow(currentRow, assignedCol);
            if (node != null && node.IsActive)
            {
                bool mined = node.MineForWorker();
                if (mined && oreDropZone != null)
                    oreDropZone.WorkerDeliverOre(miningGrid.OrePrefab);

                yield return new WaitForSeconds(mineInterval);
            }

            // 다음 행으로 이동
            int nextRow = movingForward ? currentRow + 1 : currentRow - 1;

            if (nextRow >= totalRows) { movingForward = false; nextRow = currentRow - 1; }
            if (nextRow < 0)         { movingForward = true;  nextRow = currentRow + 1; }

            Vector3 targetPos = miningGrid.GetNodeWorldPos(assignedCol, nextRow);

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPos, moveSpeed * Time.deltaTime);
                FaceTarget(targetPos);
                yield return null;
            }
            transform.position = targetPos;
            currentRow = nextRow;
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
