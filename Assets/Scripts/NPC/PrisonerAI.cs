using UnityEngine;
using System.Collections;

// NavMesh 없이 Vector3.MoveTowards로 이동하는 수감자 AI.
// 경로: SpawnPoint → WaitPosition → DeskPosition → PrisonEntrance(MarkerVis) → Cell (또는 외부 대기)
// 애니메이션: 이동 중 pb_walk, Desk 대기 중 pb_idle (Speed 파라미터로 제어)
public class PrisonerAI : MonoBehaviour
{
    [SerializeField] private SpeechBubble speechBubble;
    [SerializeField] private GameObject prisonerAfterPrefab;

    private PrisonerState state = PrisonerState.WalkingToWaitPos;
    public PrisonerState CurrentState => state;

    private DeskManager deskManager;
    private CellManager cellManager;
    private PrisonerSpawner prisonerSpawner;

    private Vector3 deskPos;   // 실제 목적지 좌표 (MarkerVis 반영)

    private int handcuffsReceived = 0;
    private int handcuffsNeeded = 4;
    private float moveSpeed = 3.5f;

    private Coroutine activeMovement;
    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameSettings s = GameManager.Instance.Settings;
            handcuffsNeeded = s.handcuffsPerPrisoner;
            moveSpeed = s.prisonerMoveSpeed;
        }
        animator = GetComponentInChildren<Animator>(true);
    }

    // PrisonerSpawner가 호출 — 모든 목적지를 Vector3으로 직접 전달
    public void InitializeWithPositions(Vector3 waitPos, Vector3 deskWorldPos,
        DeskManager desk, CellManager cell, PrisonerSpawner spawner)
    {
        deskManager     = desk;
        cellManager     = cell;
        prisonerSpawner = spawner;
        deskPos         = deskWorldPos;

        state = PrisonerState.WalkingToWaitPos;
        UpdateBubble();
        SetMovement(WalkTo(waitPos, () =>
        {
            state = PrisonerState.WaitingBehindDesk;
            prisonerSpawner?.OnPrisonerArrivedAtWait(this);
        }));
    }

    // PrisonerSpawner가 슬롯 열리면 호출 — 미리 받아둔 deskPos로 이동
    public void AdvanceToDesk(Vector3 targetDeskPos)
    {
        deskPos = targetDeskPos;
        state = PrisonerState.WalkingToWaitPos;
        SetMovement(WalkTo(deskPos, () =>
        {
            state = PrisonerState.AtDesk;
            deskManager?.RegisterPrisoner(this);
        }));
    }

    // DeskManager가 수갑 전달 시 호출
    public void ReceiveHandcuff()
    {
        handcuffsReceived++;
        int remaining = handcuffsNeeded - handcuffsReceived;
        if (remaining > 0)
        {
            UpdateBubble();
        }
        else
        {
            speechBubble?.Hide();
            state = PrisonerState.FullyProcessed;
            SwapToPrisonerAfter();
            deskManager?.OnPrisonerLeft();
            prisonerSpawner?.OnSlotFreed();
            TryGoToCell();
        }
    }

    public void OnCellAvailable(Transform cellPos)
    {
        if (state != PrisonerState.WaitingOutsideCell) return;
        speechBubble?.Hide();
        WalkToCell(cellPos.position);
    }

    // 대기줄 내 순번 변경 시 새 위치로 이동
    public void MoveToQueuePosition(Transform newPos)
    {
        if (state != PrisonerState.WaitingOutsideCell) return;
        SetMovement(WalkTo(newPos.position, () =>
        {
            state = PrisonerState.WaitingOutsideCell;
            speechBubble?.Show("No Cell!");
        }));
    }

    // desk 처리 후 감옥 입구(PrisonEntrance MarkerVis) → cell 또는 대기
    private void TryGoToCell()
    {
        Vector3 entrancePos = cellManager != null ? cellManager.PrisonEntrancePos : Vector3.zero;

        if (cellManager == null || entrancePos == Vector3.zero)
        {
            AssignCellOrQueue();
            return;
        }

        state = PrisonerState.WalkingToPrisonWait;
        SetMovement(WalkTo(entrancePos, AssignCellOrQueue));
    }

    private void AssignCellOrQueue()
    {
        Transform cellPos = cellManager?.TryGetCellPosition(this);
        if (cellPos != null)
        {
            WalkToCell(cellPos.position);
        }
        else
        {
            Transform outsidePos = cellManager?.GetOutsideWaitPosition(this);
            if (outsidePos != null)
            {
                SetMovement(WalkTo(outsidePos.position, () =>
                {
                    state = PrisonerState.WaitingOutsideCell;
                    speechBubble?.Show("No Cell!");
                }));
            }
        }
    }

    private void WalkToCell(Vector3 cellWorldPos)
    {
        state = PrisonerState.WalkingToCell;
        SetMovement(WalkTo(cellWorldPos, () =>
        {
            state = PrisonerState.InCell;
            cellManager?.ConfirmPrisonerInCell(this);
        }));
    }

    private void SetMovement(IEnumerator routine)
    {
        if (activeMovement != null) StopCoroutine(activeMovement);
        activeMovement = StartCoroutine(routine);
    }

    private IEnumerator WalkTo(Vector3 destination, System.Action onArrived)
    {
        animator?.SetFloat(SpeedHash, 1f);
        while (Vector3.Distance(transform.position, destination) > 0.25f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, moveSpeed * Time.deltaTime);
            FaceTarget(destination);
            yield return null;
        }
        transform.position = destination;
        animator?.SetFloat(SpeedHash, 0f);
        onArrived?.Invoke();
    }

    private void UpdateBubble()
    {
        int remaining = handcuffsNeeded - handcuffsReceived;
        speechBubble?.Show($"x{remaining}");
    }

    private void SwapToPrisonerAfter()
    {
        Transform visual = transform.Find("Visual");
        if (visual != null) Destroy(visual.gameObject);

        if (prisonerAfterPrefab != null)
        {
            GameObject newVisual = Instantiate(prisonerAfterPrefab, transform);
            newVisual.name = "Visual";
            newVisual.transform.localPosition = Vector3.zero;
            newVisual.transform.localEulerAngles = new Vector3(90f, -90f, 0f);
            newVisual.transform.localScale = Vector3.one;
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }
}
