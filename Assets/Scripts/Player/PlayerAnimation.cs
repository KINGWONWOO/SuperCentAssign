using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");
    private static readonly int IsInVehicleHash = Animator.StringToHash("IsInVehicle");

    void Awake()
    {
        // Animator는 Visual 자식(FBX 루트)에 있으므로 하위 탐색
        animator = GetComponentInChildren<Animator>();
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void SetMining(bool isMining)
    {
        animator.SetBool(IsMiningHash, isMining);
    }

    public void SetInVehicle(bool inVehicle)
    {
        animator.SetBool(IsInVehicleHash, inVehicle);
    }
}
