using UnityEngine;

// Animator와 같은 GameObject에 부착 (PlayerCharacter_Visual).
// IsMining 파라미터가 true일 때 애니메이션 85% 시점에 OnMiningImpact 이벤트 발생.
public class MiningAnimNotifier : MonoBehaviour
{
    public System.Action OnMiningImpact;

    private Animator animator;
    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");
    private bool fired = false;
    // 1.267s 클립 기준: 0.85 = 1.077s, 0.2초 앞당김 → 0.877s / 1.267s ≈ 0.69
    private const float Threshold = 0.69f;

    void Awake() => animator = GetComponent<Animator>();

    void Update()
    {
        if (animator == null || !animator.GetBool(IsMiningHash))
        {
            fired = false;
            return;
        }
        if (animator.IsInTransition(0)) return;

        float norm = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;

        if (!fired && norm >= Threshold)
        {
            fired = true;
            OnMiningImpact?.Invoke();
        }
        else if (norm < Threshold * 0.4f)
        {
            fired = false;
        }
    }
}
