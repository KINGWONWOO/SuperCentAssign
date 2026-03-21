using UnityEngine;
using System.Collections;

// 자동 채굴 NPC. 지정된 행을 왕복하며 앞 한 칸에 돌이 있으면 채굴.
// 채굴된 광석은 플레이어 스택이 아닌 ConverterMachine으로 직접 전달.
public class WorkerNPC : MonoBehaviour
{
    private MiningGrid miningGrid;
    private ConverterMachine converter;
    private int assignedRow;
    private float moveSpeed;
    private float mineInterval;

    private bool movingForward = true;
    private int currentCol = 0;

    public void Initialize(MiningGrid grid, int row)
    {
        miningGrid = grid;
        assignedRow = row;
        converter = FindObjectOfType<ConverterMachine>();

        GameSettings s = GameManager.Instance.Settings;
        moveSpeed = s.workerMoveSpeed;
        mineInterval = s.workerMineInterval;

        transform.position = grid.GetRowStartWorld(row);
        StartCoroutine(WorkRoutine());
    }

    private IEnumerator WorkRoutine()
    {
        while (true)
        {
            int columns = miningGrid.Columns;

            // 현재 칸의 노드 채굴 시도
            RockNode node = miningGrid.GetActiveNodeInRow(assignedRow, currentCol);
            if (node != null && node.IsActive)
            {
                bool mined = node.MineForWorker();
                if (mined && converter != null)
                    converter.ReceiveOre(1);

                yield return new WaitForSeconds(mineInterval);
            }

            // 다음 칸으로 이동
            int nextCol = movingForward ? currentCol + 1 : currentCol - 1;

            if (nextCol >= columns) { movingForward = false; nextCol = currentCol - 1; }
            if (nextCol < 0) { movingForward = true; nextCol = currentCol + 1; }

            Vector3 targetPos = miningGrid.GetRowStartWorld(assignedRow)
                + Vector3.right * nextCol * GameManager.Instance.Settings.gridSpacingX;

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPos, moveSpeed * Time.deltaTime);
                FaceTarget(targetPos);
                yield return null;
            }
            transform.position = targetPos;
            currentCol = nextCol;
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
