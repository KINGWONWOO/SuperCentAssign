using UnityEngine;
using System.Collections.Generic;

// 수요 기반 스폰: 시스템 내 최대 2명 (1명 Desk, 1명 대기).
// OnSlotFreed()가 호출될 때마다 새 수감자 생성 또는 대기자 전진.
public class PrisonerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform waitPosition;   // Desk와 Spawner 사이 대기 위치
    [SerializeField] private Transform deskPosition;   // Desk 앞 위치
    [SerializeField] private DeskManager deskManager;
    [SerializeField] private CellManager cellManager;

    // 시스템 내 수감자: waitQueue[0] = Desk 대기, waitQueue[1] = 뒤 대기
    private List<PrisonerAI> waitQueue = new List<PrisonerAI>();
    private int prisonersInSystem = 0;

    private int maxInSystem;

    void Start()
    {
        maxInSystem = GameManager.Instance.Settings.maxPrisonersInSystem;
        // 게임 시작 시 첫 수감자 스폰
        TrySpawn();
    }

    // 수감자가 Desk를 떠나면 DeskManager → OnPrisonerLeft → 여기 호출
    public void OnSlotFreed()
    {
        prisonersInSystem--;
        AdvanceQueue();
        TrySpawn();
    }

    private void AdvanceQueue()
    {
        if (waitQueue.Count == 0) return;

        PrisonerAI front = waitQueue[0];
        waitQueue.RemoveAt(0);

        if (front != null && front.CurrentState == PrisonerState.WaitingBehindDesk)
            front.AdvanceToDesk(deskPosition);
    }

    private void TrySpawn()
    {
        if (prisonersInSystem >= maxInSystem) return;
        if (prisonerPrefab == null || spawnPoint == null) return;

        // 대기 위치 결정: 앞이 비면 waitPosition, 아니면 뒤 줄
        Transform targetWait = (waitQueue.Count == 0) ? waitPosition : null;
        // 최대 2명이므로 대기자가 이미 1명 있으면 스폰 안 함
        if (waitQueue.Count >= maxInSystem) return;

        GameObject obj = Instantiate(prisonerPrefab, spawnPoint.position, spawnPoint.rotation);
        PrisonerAI ai = obj.GetComponent<PrisonerAI>();
        if (ai == null) ai = obj.AddComponent<PrisonerAI>();

        Transform dest = (waitQueue.Count == 0) ? waitPosition : GetQueuePosition(waitQueue.Count);
        ai.Initialize(dest, deskManager, cellManager);

        waitQueue.Add(ai);
        prisonersInSystem++;

        // 대기자가 1명이고 Desk가 비어있으면 바로 전진
        if (waitQueue.Count == 1)
            ai.AdvanceToDesk(deskPosition);
    }

    private Transform GetQueuePosition(int index)
    {
        // 뒤 대기는 waitPosition 바로 뒤 (간격 1.5)
        GameObject marker = new GameObject($"QueuePos_{index}");
        if (waitPosition != null)
        {
            marker.transform.position = waitPosition.position - waitPosition.forward * (index * 1.5f);
        }
        else
        {
            marker.transform.position = spawnPoint.position;
        }
        return marker.transform;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 스폰 포인트 (빨간 구)
        if (spawnPoint != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawSphere(spawnPoint.position, 0.35f);
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 0.8f, "Spawn");
        }
        // 대기 위치 (노란 구)
        if (waitPosition != null)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawSphere(waitPosition.position, 0.35f);
            UnityEditor.Handles.Label(waitPosition.position + Vector3.up * 0.8f, "Wait");
            // 2번째 대기 위치 미리보기
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f);
            Gizmos.DrawSphere(waitPosition.position - waitPosition.forward * 1.5f, 0.25f);
        }
        // 책상 앞 위치 (하늘색 구)
        if (deskPosition != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(deskPosition.position, 0.35f);
            UnityEditor.Handles.Label(deskPosition.position + Vector3.up * 0.8f, "Desk");
        }
        // 이동 경로 선
        if (spawnPoint != null && waitPosition != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawLine(spawnPoint.position, waitPosition.position);
        }
        if (waitPosition != null && deskPosition != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawLine(waitPosition.position, deskPosition.position);
        }
    }
#endif
}
