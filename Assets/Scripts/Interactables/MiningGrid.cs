using UnityEngine;

// 7열 × 20행 돌 그리드를 관리.
// 플레이어가 BoxCollider(isTrigger)에 진입하면 PlayerInteraction이 MineAt을 호출.
[RequireComponent(typeof(BoxCollider))]
public class MiningGrid : MonoBehaviour
{
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject orePrefab;

    private int columns;
    private int rows;
    private float spacingX;
    private float spacingZ;

    private RockNode[,] grid;

    // 그리드 하단 좌측 월드 위치 (Build 시 자동 계산)
    private Vector3 gridOrigin;

    public GameObject OrePrefab => orePrefab;

    // Start() 사용: Awake() 시점에는 GameManager 싱글톤이 미초기화일 수 있음
    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        columns = s.gridColumns;
        rows = s.gridRows;
        spacingX = s.gridSpacingX;
        spacingZ = s.gridSpacingZ;

        // 그리드를 transform.position 기준으로 X/Z 양방향 센터링
        gridOrigin = transform.position
            - new Vector3((columns - 1) * spacingX * 0.5f, 0f,
                          (rows    - 1) * spacingZ * 0.5f);

        BuildGrid();
        AdjustCollider();
    }

    private void BuildGrid()
    {
        grid = new RockNode[columns, rows];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 pos = GridToWorld(c, r);
                GameObject obj = Instantiate(rockPrefab, pos, Quaternion.identity, transform);
                RockNode node = obj.GetComponent<RockNode>();
                if (node == null) node = obj.AddComponent<RockNode>();
                node.GridCoord = new Vector2Int(c, r);
                grid[c, r] = node;
            }
        }
    }

    private void AdjustCollider()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
        float totalX = (columns - 1) * spacingX + spacingX;
        float totalZ = (rows - 1) * spacingZ + spacingZ;
        bc.size = new Vector3(totalX, 3f, totalZ);
        bc.center = Vector3.zero; // gridOrigin이 이미 센터링됨
    }

    // 월드 위치 기준으로 가장 가까운 행을 찾아 width 폭만큼 채굴
    public void MineAt(Vector3 worldPos, int width, PlayerStackManager stackManager)
    {
        Vector2Int coord = WorldToGrid(worldPos);
        int half = width / 2;

        for (int dc = -half; dc <= half; dc++)
        {
            int col = coord.x + dc;
            if (col < 0 || col >= columns) continue;
            if (coord.y < 0 || coord.y >= rows) continue;

            RockNode node = grid[col, coord.y];
            if (node != null && node.IsActive)
                node.Mine(stackManager, orePrefab);
        }
    }

    // 워커 NPC용: 특정 행의 모든 열을 순회하며 활성 노드 반환
    public RockNode GetActiveNodeInRow(int row, int col)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows) return null;
        return grid[col, row];
    }

    public Vector3 GetRowStartWorld(int row)
    {
        return GridToWorld(0, row);
    }

    public Vector3 GetRowEndWorld(int row)
    {
        return GridToWorld(columns - 1, row);
    }

    public int Rows => rows;
    public int Columns => columns;

    private Vector3 GridToWorld(int col, int row)
    {
        return gridOrigin + new Vector3(col * spacingX, 0f, row * spacingZ);
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - gridOrigin;
        int col = Mathf.RoundToInt(local.x / spacingX);
        int row = Mathf.RoundToInt(local.z / spacingZ);
        col = Mathf.Clamp(col, 0, columns - 1);
        row = Mathf.Clamp(row, 0, rows - 1);
        return new Vector2Int(col, row);
    }
}
