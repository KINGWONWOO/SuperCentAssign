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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 감방 슬롯 위치 미리보기 (5열 × 4행)
        if (cellRoot == null) return;

        const int cols = 5, rows = 4;
        const float sX = 1.5f, sZ = 1.5f;

        for (int i = 0; i < cols * rows; i++)
        {
            int col = i % cols;
            int row = i / cols;
            Vector3 offset = new Vector3(col * sX, 0f, -row * sZ);
            Vector3 pos = cellRoot.position + cellRoot.rotation * offset;

            // 슬롯 색상: 앞줄은 밝게, 뒷줄은 어둡게
            float t = (float)row / (rows - 1);
            Gizmos.color = Color.Lerp(new Color(0.3f, 1f, 0.4f, 0.7f),
                                      new Color(0.1f, 0.5f, 0.2f, 0.5f), t);
            Gizmos.DrawWireCube(pos, new Vector3(0.9f, 1.8f, 0.9f));
        }

        // cellRoot 기준점 (초록 구)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(cellRoot.position, 0.3f);
        UnityEditor.Handles.Label(cellRoot.position + Vector3.up * 0.8f,
            $"CellRoot  {cols}×{rows}");

        // 감옥 밖 대기 위치 (보라 구)
        if (outsideWaitSpots != null)
        {
            Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.9f);
            foreach (var spot in outsideWaitSpots)
                if (spot != null)
                {
                    Gizmos.DrawSphere(spot.position, 0.3f);
                    UnityEditor.Handles.Label(spot.position + Vector3.up * 0.7f, "NoCell Wait");
                }
        }
    }
#endif
}
