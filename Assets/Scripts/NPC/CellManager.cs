using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 감방 수용 관리. 기본 20명, 업그레이드 시 100명.
// 초과 시 수감자는 감옥 앞에서 FIFO 줄을 서며 대기.
public class CellManager : MonoBehaviour
{
    [SerializeField] private Transform cellRoot;             // 감방 내부 위치들의 부모
    [SerializeField] private Transform outsideQueueStart;    // 대기줄 맨 앞 위치 (감옥 입구 바로 앞)
    [SerializeField] private float queueSpacing = 1.5f;      // 줄 서는 간격
    [SerializeField] private TextMeshPro cellCounterText;

    private int capacity;
    private List<PrisonerAI> prisonersInCell  = new List<PrisonerAI>();
    private List<PrisonerAI> outsideQueue     = new List<PrisonerAI>(); // FIFO 대기줄
    private int nextCellIndex = 0;

    public bool IsFull => prisonersInCell.Count >= capacity;

    void Start()
    {
        capacity = GameManager.Instance.Settings.defaultCellCapacity;
        RefreshCounter();
    }

    // 감방 슬롯 확보 가능하면 위치 반환, 가득 차면 null
    public Transform TryGetCellPosition(PrisonerAI prisoner)
    {
        if (IsFull || cellRoot == null) return null;

        int col = nextCellIndex % 5;
        int row = nextCellIndex / 5;
        Vector3 offset = new Vector3(col * 1.5f, 0f, -row * 1.5f);
        Vector3 pos    = cellRoot.position + cellRoot.rotation * offset;

        GameObject slot = new GameObject($"CellSlot_{nextCellIndex}");
        slot.transform.position = pos;
        slot.transform.rotation = cellRoot.rotation;
        slot.transform.SetParent(cellRoot);

        nextCellIndex++;
        return slot.transform;
    }

    // 감옥 밖 FIFO 대기줄에 추가
    public Transform GetOutsideWaitPosition(PrisonerAI prisoner)
    {
        int idx = outsideQueue.Count;
        outsideQueue.Add(prisoner);
        return BuildQueueSlot(idx);
    }

    // 감방에 실제 입소 확정
    public void ConfirmPrisonerInCell(PrisonerAI prisoner)
    {
        prisonersInCell.Add(prisoner);
        RefreshCounter();
    }

    // 업그레이드 시 감옥 확장 → FIFO 순서대로 입소
    public void ExpandCapacity(int additionalSlots)
    {
        capacity += additionalSlots;
        RefreshCounter();
        ProcessOutsideQueue();
    }

    // 대기 중인 수감자들을 FIFO로 처리하고, 남은 인원 위치 재배정
    private void ProcessOutsideQueue()
    {
        while (outsideQueue.Count > 0 && !IsFull)
        {
            PrisonerAI front = outsideQueue[0];
            outsideQueue.RemoveAt(0);

            Transform cellPos = TryGetCellPosition(front);
            if (cellPos != null)
            {
                front.OnCellAvailable(cellPos);

                // 뒤에 남은 수감자들을 한 칸씩 앞으로 이동
                for (int i = 0; i < outsideQueue.Count; i++)
                    outsideQueue[i].MoveToQueuePosition(BuildQueueSlot(i));
            }
            else
            {
                outsideQueue.Insert(0, front); // 슬롯 부족 → 다시 맨 앞으로
                break;
            }
        }
    }

    // 줄의 i번째 위치 Transform 생성
    private Transform BuildQueueSlot(int index)
    {
        Vector3 origin = outsideQueueStart != null
            ? outsideQueueStart.position
            : transform.position + Vector3.back * 3f;
        Vector3 dir = outsideQueueStart != null
            ? outsideQueueStart.forward
            : Vector3.forward;

        GameObject slot = new GameObject($"QueueSlot_{index}");
        slot.transform.position = origin + dir * (index * queueSpacing);
        return slot.transform;
    }

    private void RefreshCounter()
    {
        if (cellCounterText != null)
            cellCounterText.text = $"{prisonersInCell.Count}/{capacity}";
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // ── 감방 슬롯 미리보기 (5×4 초록 와이어박스) ─────────────────
        if (cellRoot != null)
        {
            for (int i = 0; i < 20; i++)
            {
                int col = i % 5, row = i / 5;
                Vector3 offset = new Vector3(col * 1.5f, 0f, -row * 1.5f);
                Vector3 pos    = cellRoot.position + cellRoot.rotation * offset;
                float t = (float)row / 3f;
                Gizmos.color = Color.Lerp(new Color(0.2f, 1f, 0.3f, 0.8f),
                                          new Color(0.1f, 0.4f, 0.2f, 0.5f), t);
                Gizmos.DrawWireCube(pos, new Vector3(0.9f, 1.8f, 0.9f));
            }
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(cellRoot.position, 0.35f);
            UnityEditor.Handles.Label(cellRoot.position + Vector3.up,
                "CellRoot (5×4)");
        }

        // ── 외부 대기줄 미리보기 (보라 와이어박스 × 5칸) ─────────────
        if (outsideQueueStart != null)
        {
            Vector3 dir = outsideQueueStart.forward;
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = outsideQueueStart.position + dir * (i * queueSpacing);
                float t = i / 4f;
                Gizmos.color = Color.Lerp(new Color(1f, 0.3f, 1f, 0.9f),
                                          new Color(0.5f, 0.1f, 0.5f, 0.4f), t);
                Gizmos.DrawWireCube(pos, new Vector3(0.7f, 1.8f, 0.5f));
                UnityEditor.Handles.Label(pos + Vector3.up * 1.2f, $"Wait {i+1}");
            }
            Gizmos.color = new Color(1f, 0f, 1f);
            Gizmos.DrawSphere(outsideQueueStart.position, 0.35f);
            // 방향 화살표
            Gizmos.color = new Color(1f, 0.5f, 1f, 0.6f);
            Gizmos.DrawLine(outsideQueueStart.position,
                            outsideQueueStart.position + dir * (5 * queueSpacing));
        }
    }
#endif
}
