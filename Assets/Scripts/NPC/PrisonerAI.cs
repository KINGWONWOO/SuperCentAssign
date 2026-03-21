using UnityEngine;
using System.Collections;

// NavMesh 없이 Vector3.MoveTowards로 이동하는 수감자 AI.
// 모든 이동은 코루틴으로 처리. Update는 사용하지 않음.
public class PrisonerAI : MonoBehaviour
{
    [SerializeField] private SpeechBubble speechBubble;

    private PrisonerState state = PrisonerState.WalkingToWaitPos;
    public PrisonerState CurrentState => state;

    private DeskManager deskManager;
    private CellManager cellManager;

    private int handcuffsReceived = 0;
    private int handcuffsNeeded = 4;
    private float moveSpeed = 3.5f;

    private Coroutine activeMovement;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameSettings s = GameManager.Instance.Settings;
            handcuffsNeeded = s.handcuffsPerPrisoner;
            moveSpeed = s.prisonerMoveSpeed;
        }
    }

    public void Initialize(Transform waitPos, DeskManager desk, CellManager cell)
    {
        deskManager = desk;
        cellManager = cell;
        state = PrisonerState.WalkingToWaitPos;
        UpdateBubble();
        SetMovement(WalkTo(waitPos.position, () =>
        {
            state = PrisonerState.WaitingBehindDesk;
        }));
    }

    // PrisonerSpawner가 Desk 슬롯 열리면 호출
    public void AdvanceToDesk(Transform deskPos)
    {
        state = PrisonerState.WalkingToWaitPos;
        SetMovement(WalkTo(deskPos.position, () =>
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
            deskManager?.OnPrisonerLeft();
            TryGoToCell();
        }
    }

    public void OnCellAvailable(Transform cellPos)
    {
        if (state != PrisonerState.WaitingOutsideCell) return;
        speechBubble?.Hide();
        WalkToCell(cellPos);
    }

    // 대기줄 내 순번 변경 시 새 위치로 이동 (FIFO 재배치)
    public void MoveToQueuePosition(Transform newPos)
    {
        if (state != PrisonerState.WaitingOutsideCell) return;
        SetMovement(WalkTo(newPos.position, () =>
        {
            state = PrisonerState.WaitingOutsideCell;
            speechBubble?.Show("No Cell!");
        }));
    }

    private void TryGoToCell()
    {
        Transform cellPos = cellManager?.TryGetCellPosition(this);
        if (cellPos != null)
        {
            WalkToCell(cellPos);
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

    private void WalkToCell(Transform cellPos)
    {
        state = PrisonerState.WalkingToCell;
        SetMovement(WalkTo(cellPos.position, () =>
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
        while (Vector3.Distance(transform.position, destination) > 0.25f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, moveSpeed * Time.deltaTime);
            FaceTarget(destination);
            yield return null;
        }
        transform.position = destination;
        onArrived?.Invoke();
    }

    private void UpdateBubble()
    {
        int remaining = handcuffsNeeded - handcuffsReceived;
        speechBubble?.Show($"x{remaining}");
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }
}
