using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameSettings", fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Player Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    [Header("Mining")]
    [Tooltip("채집 간격(초) - 인덱스 0=곡괭이, 1=드릴, 2=불도저")]
    public float[] miningIntervals = { 1.0f, 0.5f, 0.2f };
    public int maxStackCount = 15;

    [Header("Mining Node")]
    public int defaultNodeHP = 5;
    public float respawnTime = 8f;

    [Header("Converter Machine")]
    public float processTime = 3f;
    public int orePerHandcuff = 3;
    public int maxConverterOutput = 10;

    [Header("Drop Zone")]
    [Tooltip("아이템 하나씩 이동하는 간격(초)")]
    public float dropInterval = 0.25f;

    [Header("Upgrades")]
    [Tooltip("곡괭이→드릴, 드릴→불도저 업그레이드 비용")]
    public int[] toolUpgradeCosts = { 50, 150 };

    [Header("Prisoner")]
    public float prisonerSpawnInterval = 8f;
    public int maxPrisonersAtOnce = 5;
    public int cashPerPrisoner = 10;

    [Header("Cell")]
    public int defaultCellCapacity = 3;
}
