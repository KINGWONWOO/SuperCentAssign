using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private VirtualJoystick joystick;

    private CharacterController cc;
    private PlayerAnimation playerAnimation;
    private Camera mainCamera;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerAnimation = GetComponent<PlayerAnimation>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        GameSettings settings = GameManager.Instance.Settings;
        Vector2 input = joystick != null ? joystick.InputDirection : Vector2.zero;

#if UNITY_EDITOR
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        input = Vector2.ClampMagnitude(input + new Vector2(h, v), 1f);
#endif

        if (input.magnitude < 0.1f)
        {
            playerAnimation?.SetSpeed(0f);
            return;
        }

        // 카메라 방향 기준 이동 (쿼터뷰 대응)
        Vector3 camForward = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized
            : Vector3.forward;
        Vector3 camRight = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized
            : Vector3.right;

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        cc.SimpleMove(moveDir * settings.moveSpeed);

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, settings.rotationSpeed * Time.deltaTime);
        }

        playerAnimation?.SetSpeed(input.magnitude);
    }
}
