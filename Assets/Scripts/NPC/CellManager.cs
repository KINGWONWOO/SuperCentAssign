using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 감방 수용 관리. 기본 20명, 업그레이드 시 100명.
// 초과 시 수감자는 감옥 앞에서 No Cell! 말풍선을 띄우며 대기.
public class CellManager : MonoBehaviour
{
    [SerializeField] private Transform cellRoot;            // 감방 내부 위치들의 부모
    [SerializeField] private Transform[] outsideWaitSpots;  // 감옥 앞 대기 위치들
    [SerializeField] private TextMeshPro cellCounterText;

    private int capacity;
    private List<PrisonerAI> prisonersInCell = new List<PrisonerAI>();
    private List<PrisonerAI> prisonersOutside = new List<PrisonerAI>();
    private int nextCellIndex = 0;

    public bool IsFull => prisonersInCell.Count >= capacity;

    void Start()
    {
        capacity = GameManager.Instance.Settings.defaultCellCapacity;
        RefreshCounter();
    }

    // 수감자가 감방에 들어갈 수 있으면 위치 반환, 가득 차면 null
    public Transform TryGetCellPosition(PrisonerAI prisoner)
    {
        if (IsFull) return null;

        if (cellRoot == null) return null;

        // 셀 슬롯 생성 (5×4 배열)
        int col = nextCellIndex % 5;
        int row = nextCellIndex / 5;
        float spacingX = 1.5f;
        float spacingZ = 1.5f;
        Vector3 offset = new Vector3(col * spacingX, 0f, -row * spacingZ);
        Vector3 pos = cellRoot.position + cellRoot.rotation * offset;

        GameObject slot = new GameObject($"CellSlot_{nextCellIndex}");
        slot.transform.position = pos;
        slot.transform.rotation = cellRoot.rotation;
        slot.transform.SetParent(cellRoot);

        nextCellIndex++;
        return slot.transform;
    }

    // 수용 초과 시 밖에 대기할 위치 반환
    public Transform GetOutsideWaitPosition(PrisonerAI prisoner)
    {
        prisonersOutside.Add(prisoner);
        int idx = prisonersOutside.Count - 1;

        if (outsideWaitSpots != null && idx < outsideWaitSpots.Length)
            return outsideWaitSpots[idx];

        // 동적으로 위치 생성 (겹치지 않게)
        GameObject marker = new GameObject($"OutsideWait_{idx}");
        float spread = idx * 1.2f;
        marker.transform.position = transform.position + new Vector3(spread % 4f * 1.2f - 2.4f, 0f, -2f - (idx / 4) * 1.2f);
        return marker.transform;
    }

    public void ConfirmPrisonerInCell(PrisonerAI prisoner)
    {
        prisonersInCell.Add(prisoner);
        RefreshCounter();
    }

    // 업그레이드 시 감옥 확장 → 대기 중인 수감자 입소 처리
    public void ExpandCapacity(int additionalSlots)
    {
        capacity += additionalSlots;
        RefreshCounter();

        // 대기 중인 수감자들을 순서대로 입소
        List<PrisonerAI> toProcess = new List<PrisonerAI>(prisonersOutside);
        prisonersOutside.Clear();

        foreach (PrisonerAI p in toProcess)
        {
            Transform cellPos = TryGetCellPosition(p);
            if (cellPos != null)
                p.OnCellAvailable(cellPos);
            else
                prisonersOutside.Add(p); // 아직 부족하면 다시 대기
        }
    }

    private void RefreshCounter()
    {
        if (cellCounterText != null)
            cellCounterText.text = $"{prisonersInCell.Count}/{capacity}";
    }
}
