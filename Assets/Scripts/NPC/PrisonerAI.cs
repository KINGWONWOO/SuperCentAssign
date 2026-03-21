using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PrisonerAI : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTimeAtPoint = 2f;

    private NavMeshAgent agent;
    private Animator animator;

    private PrisonerState currentState = PrisonerState.Roaming;
    public PrisonerState CurrentState => currentState;

    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private Transform cellTarget;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            currentState = PrisonerState.Roaming;
            MoveToNextWaypoint();
        }
        else
        {
            currentState = PrisonerState.Waiting;
        }
    }

    void Update()
    {
        animator?.SetFloat(SpeedHash, agent.velocity.magnitude);

        switch (currentState)
        {
            case PrisonerState.Roaming:
                UpdateRoaming();
                break;

            case PrisonerState.Waiting:
                // 플레이어의 체포를 기다림 (ArrestZone이 담당)
                break;

            case PrisonerState.Arrested:
                UpdateArrested();
                break;
        }
    }

    private void UpdateRoaming()
    {
        if (agent.pathPending) return;
        if (agent.remainingDistance > agent.stoppingDistance) return;

        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTimeAtPoint)
        {
            waitTimer = 0f;
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                // 마지막 웨이포인트 도달 → 체포 대기
                currentState = PrisonerState.Waiting;
                agent.ResetPath();
            }
            else
            {
                MoveToNextWaypoint();
            }
        }
    }

    private void UpdateArrested()
    {
        if (cellTarget == null) return;
        if (agent.pathPending) return;
        if (agent.remainingDistance > agent.stoppingDistance) return;

        // 감방 도착
        currentState = PrisonerState.InCell;
        agent.ResetPath();
    }

    public void Arrest(CellManager cellManager)
    {
        if (!cellManager.TryAddPrisoner(this)) return;

        currentState = PrisonerState.Arrested;
        cellTarget = cellManager.GetNextCellPosition();

        if (cellTarget != null)
            agent.SetDestination(cellTarget.position);
    }

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypoint = 0;
        currentState = PrisonerState.Roaming;
        MoveToNextWaypoint();
    }

    private void MoveToNextWaypoint()
    {
        if (waypoints == null || currentWaypoint >= waypoints.Length) return;
        agent.SetDestination(waypoints[currentWaypoint].position);
    }
}
