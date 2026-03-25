using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 감방 수용 관리. 기본 20명, 업그레이드 시 100명.
// 초과 시 수감자는 감옥 앞에서 FIFO 줄을 서며 대기.
// stackPoints 배열이 있으면 해당 Transform 위치를 사용; 없으면 격자 계산.
// 시각적 마커를 이동하면 실제 스택 위치도 함께 변경됨.
public class CellManager : MonoBehaviour
{
    [SerializeField] private Transform cellRoot;             // 감방 내부 위치들의 부모
    [SerializeField] private Transform outsideQueueStart;    // 대기줄 맨 앞 위치 (감옥 입구 바로 앞)
    [SerializeField] private float queueSpacing = 1.5f;      // 줄 서는 간격
    [SerializeField] private TextMeshPro cellCounterText;

    [Tooltip("수감자 스택 포인트 마커 (비어 있으면 격자 계산 사용). Inspector에서 이동 시 실제 스택 위치도 변경됨.")]
    [SerializeField] private Transform[] stackPoints;

    // 수감자 pivot이 발 위치(Y=0)이므로 추가 오프셋 불필요
    private const float PRISONER_Y_OFFSET = 0f;

    private int capacity;
    private List<PrisonerAI> prisonersInCell  = new List<PrisonerAI>();
    private List<PrisonerAI> outsideQueue     = new List<PrisonerAI>();
    private int nextCellIndex = 0;

    public bool IsFull => prisonersInCell.Count >= capacity;

    // 감옥 입구 경유 포인트 — outsideQueueStart의 MarkerVis 위치 사용
    public Vector3 PrisonEntrancePos
    {
        get
        {
            if (outsideQueueStart == null) return Vector3.zero;
            var mv = outsideQueueStart.Find("MarkerVis");
            return mv != null ? mv.position : outsideQueueStart.position;
        }
    }

    // cellRoot의 실제 기준 위치 (MarkerVis 반영)
    private Vector3 CellGridOrigin
    {
        get
        {
            if (cellRoot == null) return Vector3.zero;
            var mv = cellRoot.Find("MarkerVis");
            return mv != null ? mv.position : cellRoot.position;
        }
    }

    System.Collections.IEnumerator Start()
    {
        yield return new UnityEngine.WaitUntil(() =>
            GameManager.Instance != null && GameManager.Instance.Settings != null);
        capacity = GameManager.Instance.Settings.defaultCellCapacity;
        RefreshCounter();
    }

    // 감방 슬롯 확보 가능하면 위치 반환, 가득 차면 null
    public Transform TryGetCellPosition(PrisonerAI prisoner)
    {
        if (IsFull || cellRoot == null) return null;

        Vector3 pos;
        Quaternion rot;

        if (stackPoints != null && nextCellIndex < stackPoints.Length && stackPoints[nextCellIndex] != null)
        {
            // 시각적 마커 위치 사용
            pos = stackPoints[nextCellIndex].position;
            rot = stackPoints[nextCellIndex].rotation;
        }
        else
        {
            // 격자 계산 fallback
            int col = nextCellIndex % 5;
            int row = nextCellIndex / 5;
            Vector3 offset = new Vector3(col * 1.5f, 0f, row * 1.5f);
            pos = CellGridOrigin + cellRoot.rotation * offset;
            pos.y = PRISONER_Y_OFFSET;
            rot = cellRoot.rotation;
        }

        GameObject slot = new GameObject($"CellSlot_{nextCellIndex}");
        slot.transform.position = pos;
        slot.transform.rotation = rot;
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
                for (int i = 0; i < outsideQueue.Count; i++)
                    outsideQueue[i].MoveToQueuePosition(BuildQueueSlot(i));
            }
            else
            {
                outsideQueue.Insert(0, front);
                break;
            }
        }
    }

    // 외부 대기줄 i번째 Transform — outsideQueueStart의 MarkerVis 위치 기준
    private Transform BuildQueueSlot(int index)
    {
        Vector3 origin = PrisonEntrancePos;
        if (outsideQueueStart != null)
        {
            // MarkerVis가 없는 경우 outsideQueueStart 자체 위치 fallback
            var mv = outsideQueueStart.Find("MarkerVis");
            if (mv == null) origin = outsideQueueStart.position;
        }

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
        // ── 명시적 스택 포인트 마커 ────────────────────────────────
        if (stackPoints != null && stackPoints.Length > 0)
        {
            for (int i = 0; i < stackPoints.Length; i++)
            {
                if (stackPoints[i] == null) continue;
                float t = (float)i / Mathf.Max(1, stackPoints.Length - 1);
                Gizmos.color = Color.Lerp(new Color(0.2f, 1f, 0.3f, 0.9f),
                                          new Color(0.1f, 0.4f, 0.2f, 0.6f), t);
                Gizmos.DrawWireCube(stackPoints[i].position, new Vector3(0.9f, 1.8f, 0.9f));
                UnityEditor.Handles.Label(stackPoints[i].position + Vector3.up * 1.1f, $"S{i}");
            }
        }
        // ── 격자 계산 미리보기 (stackPoints 미설정 시) ──────────────
        else if (cellRoot != null)
        {
            Vector3 origin = CellGridOrigin;
            for (int i = 0; i < 20; i++)
            {
                int col = i % 5, row = i / 5;
                Vector3 offset = new Vector3(col * 1.5f, 0f, row * 1.5f);
                Vector3 pos    = origin + cellRoot.rotation * offset;
                pos.y          = PRISONER_Y_OFFSET;
                float t = (float)row / 3f;
                Gizmos.color = Color.Lerp(new Color(0.2f, 1f, 0.3f, 0.8f),
                                          new Color(0.1f, 0.4f, 0.2f, 0.5f), t);
                Gizmos.DrawWireCube(pos, new Vector3(0.9f, 1.8f, 0.9f));
            }
            // MarkerVis 위치 표시
            var mv = cellRoot.Find("MarkerVis");
            Gizmos.color = Color.green;
            Vector3 markPos = mv != null ? mv.position : cellRoot.position;
            Gizmos.DrawSphere(markPos, 0.35f);
            UnityEditor.Handles.Label(markPos + Vector3.up, "CellGrid Origin (MV)");
        }

        // ── 외부 대기줄 미리보기 ──────────────────────────────────
        if (outsideQueueStart != null)
        {
            Vector3 origin = PrisonEntrancePos;
            Vector3 dir = outsideQueueStart.forward;
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = origin + dir * (i * queueSpacing);
                pos.y = PRISONER_Y_OFFSET;
                float t = i / 4f;
                Gizmos.color = Color.Lerp(new Color(1f, 0.3f, 1f, 0.9f),
                                          new Color(0.5f, 0.1f, 0.5f, 0.4f), t);
                Gizmos.DrawWireCube(pos, new Vector3(0.7f, 1.8f, 0.5f));
                UnityEditor.Handles.Label(pos + Vector3.up * 1.2f, $"Wait {i + 1}");
            }
            Gizmos.color = new Color(1f, 0f, 1f);
            Gizmos.DrawSphere(origin, 0.35f);
            UnityEditor.Handles.Label(origin + Vector3.up * 0.5f, "PrisonEntrance(MV)");
        }
    }
#endif
}
