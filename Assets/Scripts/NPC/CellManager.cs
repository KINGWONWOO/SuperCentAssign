using UnityEngine;
using System.Collections.Generic;

public class CellManager : MonoBehaviour
{
    [Tooltip("각 수감자가 배정될 침대 위치 Transform 배열")]
    [SerializeField] private Transform[] cellPositions;

    [Tooltip("수감 시 활성화될 침대 오브젝트 배열 (cellPositions와 순서 일치)")]
    [SerializeField] private GameObject[] bedObjects;

    private List<PrisonerAI> prisoners = new List<PrisonerAI>();
    private int capacity;

    public bool IsFull => prisoners.Count >= capacity;
    public int PrisonerCount => prisoners.Count;

    void Start()
    {
        capacity = GameManager.Instance.Settings.defaultCellCapacity;
        capacity = Mathf.Min(capacity, cellPositions != null ? cellPositions.Length : capacity);
    }

    public bool TryAddPrisoner(PrisonerAI prisoner)
    {
        if (IsFull) return false;
        prisoners.Add(prisoner);
        return true;
    }

    public Transform GetNextCellPosition()
    {
        int index = prisoners.Count - 1;
        if (cellPositions == null || index >= cellPositions.Length) return null;

        // 해당 침대 활성화
        if (bedObjects != null && index < bedObjects.Length && bedObjects[index] != null)
            bedObjects[index].SetActive(true);

        return cellPositions[index];
    }

    public void ExpandCapacity(int additionalSlots)
    {
        capacity += additionalSlots;
    }
}
