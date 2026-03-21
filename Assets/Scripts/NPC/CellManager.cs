using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CellManager : MonoBehaviour
{
    [Tooltip("각 수감자가 배정될 침대 위치 Transform 배열")]
    [SerializeField] private Transform[] cellPositions;

    [Tooltip("수감 시 활성화될 침대 오브젝트 배열 (cellPositions와 순서 일치)")]
    [SerializeField] private GameObject[] bedObjects;

    [Tooltip("감방 위에 표시될 X/Y 형식 카운터 텍스트 (TextMeshPro 3D)")]
    [SerializeField] private TextMeshPro cellCounterText;

    private List<PrisonerAI> prisoners = new List<PrisonerAI>();
    private int capacity;

    public bool IsFull => prisoners.Count >= capacity;
    public int PrisonerCount => prisoners.Count;

    void Start()
    {
        capacity = GameManager.Instance.Settings.defaultCellCapacity;
        if (cellPositions != null)
            capacity = Mathf.Min(capacity, cellPositions.Length);
        RefreshCounter();
    }

    public bool TryAddPrisoner(PrisonerAI prisoner)
    {
        if (IsFull) return false;
        prisoners.Add(prisoner);
        RefreshCounter();
        return true;
    }

    public Transform GetNextCellPosition()
    {
        int index = prisoners.Count - 1;
        if (cellPositions == null || index >= cellPositions.Length) return null;

        if (bedObjects != null && index < bedObjects.Length && bedObjects[index] != null)
            bedObjects[index].SetActive(true);

        return cellPositions[index];
    }

    public void ExpandCapacity(int additionalSlots)
    {
        capacity += additionalSlots;
        RefreshCounter();
    }

    private void RefreshCounter()
    {
        if (cellCounterText != null)
            cellCounterText.text = $"{prisoners.Count}/{capacity}";
    }
}
