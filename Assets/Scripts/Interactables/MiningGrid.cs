using UnityEngine;

// 7열 × 20행 돌 그리드를 관리.
// 부모(MiningGridGroup)의 회전을 반영하여 로컬 좌표계 기반으로 스폰.
[RequireComponent(typeof(BoxCollider))]
public class MiningGrid : MonoBehaviour
{
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject orePrefab;

    [Header("Editor Preview (씬 뷰 미리보기)")]
    [SerializeField] private int previewColumns = 7;
    [SerializeField] private int previewRows    = 20;
    [SerializeField] private float previewSpacingX = 1.2f;
    [SerializeField] private float previewSpacingZ  = 1.2f;

    private int columns;
    private int rows;
    private float spacingX;
    private float spacingZ;

    private RockNode[,] grid;

    // 로컬 공간 원점 (부모 회전 반영)
    private Vector3 localGridOrigin;

    public GameObject OrePrefab => orePrefab;

    void Start()
    {
        GameSettings s = GameManager.Instance.Settings;
        columns  = s.gridColumns;
        rows     = s.gridRows;
        spacingX = s.gridSpacingX;
        spacingZ = s.gridSpacingZ;

        // 로컬 공간 기준 센터링 → transform.TransformPoint로 월드 변환
        localGridOrigin = new Vector3(
            -(columns - 1) * spacingX * 0.5f,
            0f,
            -(rows    - 1) * spacingZ * 0.5f);

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
                Vector3 worldPos = GridToWorld(c, r);
                GameObject obj = Instantiate(rockPrefab, worldPos,
                                             transform.rotation, transform);
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
        float totalZ = (rows    - 1) * spacingZ + spacingZ;
        bc.size   = new Vector3(totalX, 3f, totalZ);
        bc.center = Vector3.zero;
    }

    public void MineAt(Vector3 worldPos, int width, PlayerStackManager stackManager, int rearDepth = 0)
    {
        Vector2Int coord = WorldToGrid(worldPos);
        int half = width / 2;

        // dr=0: 앞 행, dr<0: 뒤쪽 행 (rearDepth만큼 추가)
        for (int dr = -rearDepth; dr <= 0; dr++)
        {
            for (int dc = -half; dc <= half; dc++)
            {
                int col = coord.x + dc;
                int row = coord.y + dr;
                if (col < 0 || col >= columns) continue;
                if (row < 0 || row >= rows) continue;

                RockNode node = grid[col, row];
                if (node != null && node.IsActive)
                    node.Mine(stackManager, orePrefab);
            }
        }
    }

    public RockNode GetActiveNodeInRow(int row, int col)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows) return null;
        return grid[col, row];
    }

    public Vector3 GetRowStartWorld(int row) => GridToWorld(0, row);
    public Vector3 GetRowEndWorld(int row)   => GridToWorld(columns - 1, row);

    // 특정 (col, row) 노드의 월드 좌표
    public Vector3 GetNodeWorldPos(int col, int row) => GridToWorld(col, row);

    public int Rows    => rows;
    public int Columns => columns;

    // 로컬 → 월드 (부모 회전 자동 반영)
    private Vector3 GridToWorld(int col, int row)
    {
        Vector3 local = localGridOrigin + new Vector3(col * spacingX, 0f, row * spacingZ);
        return transform.TransformPoint(local);
    }

    // 월드 → 그리드 좌표 (InverseTransformPoint로 로컬 변환)
    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos) - localGridOrigin;
        int col = Mathf.Clamp(Mathf.RoundToInt(local.x / spacingX), 0, columns - 1);
        int row = Mathf.Clamp(Mathf.RoundToInt(local.z / spacingZ), 0, rows    - 1);
        return new Vector2Int(col, row);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        int pCols = Application.isPlaying ? columns : previewColumns;
        int pRows = Application.isPlaying ? rows    : previewRows;
        float pSX = Application.isPlaying ? spacingX : previewSpacingX;
        float pSZ = Application.isPlaying ? spacingZ  : previewSpacingZ;

        Vector3 localOrigin = new Vector3(
            -(pCols - 1) * pSX * 0.5f, 0f,
            -(pRows - 1) * pSZ * 0.5f);

        // 개별 돌 위치 — 부모 회전 반영
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        for (int r = 0; r < pRows; r++)
            for (int c = 0; c < pCols; c++)
            {
                Vector3 local = localOrigin + new Vector3(c * pSX, 0f, r * pSZ);
                Vector3 world = transform.TransformPoint(local);
                Gizmos.DrawWireCube(world, new Vector3(0.5f, 0.55f, 0.5f));
            }

        // 전체 영역 외곽 박스
        float totalW = (pCols - 1) * pSX + pSX;
        float totalD = (pRows - 1) * pSZ + pSZ;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color  = new Color(1f, 0.3f, 0f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(totalW, 1f, totalD));
        Gizmos.matrix = Matrix4x4.identity;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"MiningGrid {pCols}×{pRows}");
    }
#endif
}
