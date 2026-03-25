using UnityEngine;
using System.Collections.Generic;

// 수요 기반 스폰: 시스템 내 최대 2명 (1명 Desk, 1명 대기).
// 각 마커(SpawnPoint, WaitPosition, DeskPosition)에 MarkerVis 자식이 있으면
// 그 위치를 실제 목적지로 사용 (디자이너가 MarkerVis를 이동하면 경로 자동 반영).
public class PrisonerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform waitPosition;   // WaitPosition 마커
    [SerializeField] private Transform deskPosition;   // DeskPosition 마커
    [SerializeField] private DeskManager deskManager;
    [SerializeField] private CellManager cellManager;

    private List<PrisonerAI> waitQueue = new List<PrisonerAI>();
    private int prisonersInSystem = 0;
    private bool deskOccupied = false;
    private int maxInSystem;
    private float spawnInterval;
    private bool spawnCooldown = false;

    // MarkerVis 자식이 있으면 그 위치 반환, 없으면 부모 위치
    private static Vector3 GetMarkerPos(Transform t)
    {
        if (t == null) return Vector3.zero;
        var mv = t.Find("MarkerVis");
        return mv != null ? mv.position : t.position;
    }

    void Start()
    {
        var s = GameManager.Instance.Settings;
        maxInSystem = s.maxPrisonersInSystem;
        spawnInterval = s.prisonerSpawnInterval;
        // 첫 번째 즉시 스폰, 이후는 spawnInterval 간격으로
        TrySpawn();
        StartCoroutine(SpawnWithDelay());
    }

    // PrisonerAI가 WaitPosition에 도착했을 때 호출
    public void OnPrisonerArrivedAtWait(PrisonerAI ai)
    {
        if (deskOccupied) return;
        if (waitQueue.Count == 0 || waitQueue[0] != ai) return;

        waitQueue.RemoveAt(0);
        deskOccupied = true;
        ai.AdvanceToDesk(GetMarkerPos(deskPosition));
    }

    // 수감자 처리 완료 시 PrisonerAI에서 직접 호출
    public void OnSlotFreed()
    {
        prisonersInSystem--;
        deskOccupied = false;
        AdvanceQueue();
        StartCoroutine(SpawnWithDelay());
    }

    private void AdvanceQueue()
    {
        if (waitQueue.Count == 0) return;

        PrisonerAI front = waitQueue[0];
        if (front == null) { waitQueue.RemoveAt(0); return; }

        if (front.CurrentState == PrisonerState.WaitingBehindDesk)
        {
            waitQueue.RemoveAt(0);
            deskOccupied = true;
            front.AdvanceToDesk(GetMarkerPos(deskPosition));
        }
    }

    private System.Collections.IEnumerator SpawnWithDelay()
    {
        if (spawnCooldown) yield break;
        spawnCooldown = true;
        yield return new UnityEngine.WaitForSeconds(spawnInterval);
        spawnCooldown = false;
        // 빈 슬롯만큼 채워서 재개
        while (prisonersInSystem < maxInSystem && waitQueue.Count < maxInSystem)
            TrySpawn();
    }

    private const int maxOutsideQueue = 4;

    private void TrySpawn()
    {
        if (prisonersInSystem >= maxInSystem) return;
        if (waitQueue.Count >= maxInSystem) return;
        if (cellManager != null && cellManager.OutsideQueueCount >= maxOutsideQueue) return;
        if (prisonerPrefab == null || spawnPoint == null) return;

        Vector3 dest = GetQueuePosition(waitQueue.Count);
        GameObject obj = Instantiate(prisonerPrefab, GetMarkerPos(spawnPoint), spawnPoint.rotation);
        PrisonerAI ai = obj.GetComponent<PrisonerAI>();
        if (ai == null) ai = obj.AddComponent<PrisonerAI>();

        ai.InitializeWithPositions(dest, GetMarkerPos(deskPosition), deskManager, cellManager, this);
        waitQueue.Add(ai);
        prisonersInSystem++;
    }

    // 대기 줄 위치: index=0 → waitPosition(MarkerVis), 이후는 waitPosition.forward 기준 뒤로
    private Vector3 GetQueuePosition(int index)
    {
        Vector3 basePos = GetMarkerPos(waitPosition);
        if (index == 0) return basePos;

        Vector3 dir = waitPosition != null ? -waitPosition.forward : Vector3.back;
        return basePos + dir * (index * 1.5f);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector3 sp = GetMarkerPos(spawnPoint);
        Vector3 wp = GetMarkerPos(waitPosition);
        Vector3 dp = GetMarkerPos(deskPosition);

        if (spawnPoint != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawSphere(sp, 0.35f);
            UnityEditor.Handles.Label(sp + Vector3.up * 0.8f, "Spawn");
        }
        if (waitPosition != null)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawSphere(wp, 0.35f);
            UnityEditor.Handles.Label(wp + Vector3.up * 0.8f, "Wait(MV)");
        }
        if (deskPosition != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(dp, 0.35f);
            UnityEditor.Handles.Label(dp + Vector3.up * 0.8f, "Desk(MV)");
        }
        if (spawnPoint != null && waitPosition != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawLine(sp, wp);
        }
        if (waitPosition != null && deskPosition != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawLine(wp, dp);
        }
    }
#endif
}
