using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");
    private static readonly int IsInVehicleHash = Animator.StringToHash("IsInVehicle");

    void Awake()
    {
        animator = GetComponent<Animator>();
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
