using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameSettings", fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Player Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 15f;

    [Header("Mining")]
    [Tooltip("채집 간격(초) - 0=곡괭이(1s), 1=드릴(즉시), 2=드릴 차(즉시)")]
    public float[] miningIntervals = { 1.0f, 0f, 0f };
    [Tooltip("채굴 가로 폭 - 0=1칸, 1=3칸, 2=5칸")]
    public int[] miningWidths = { 1, 3, 5 };
    [Tooltip("최대 광석 스택 - 0=10, 1=20, 2=30")]
    public int[] maxOreStacks = { 10, 20, 30 };
    public float rockRespawnTime = 6f;

    [Header("Mining Grid")]
    public int gridColumns = 7;
    public int gridRows = 20;
    public float gridSpacingX = 1.2f;
    public float gridSpacingZ = 1.2f;

    [Header("Converter")]
    public float processTime = 2f;
    public float oreDropSpeed = 15f;

    [Header("Drop Zone")]
    public float dropInterval = 0.3f;

    [Header("Desk / Arrest")]
    public int handcuffsPerPrisoner = 4;
    public int cashPerHandcuff = 10;
    public float sellInterval = 0.5f;

    [Header("Prisoner")]
    public float prisonerMoveSpeed = 3.5f;
    public int maxPrisonersInSystem = 2;
    public float prisonerSpawnInterval = 3f;

    [Header("Cell")]
    public int defaultCellCapacity = 20;
    public int expandedCellCapacity = 100;

    [Header("Upgrade Costs")]
    public int drillCost = 20;
    public int drillCarCost = 50;
    public int workerHireCost = 40;
    public int autoSellCost = 50;
    public int prisonExpandCost = 50;

    [Header("Bulldozer")]
    [Tooltip("불도저 채굴 시 앞방향 기준 추가로 채굴할 뒤쪽 행 수")]
    public int bulldozerRearDepth = 2;

    [Header("Worker NPC")]
    public float workerMoveSpeed = 3f;
    public float workerMineInterval = 1f;

    [Header("Auto Sell NPC")]
    public float autoSellMoveSpeed = 4f;
    public int autoSellBatchSize = 10;
}
