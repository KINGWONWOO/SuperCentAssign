using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Transform rightHandBone;

    private static readonly int SpeedHash        = Animator.StringToHash("Speed");
    private static readonly int IsMiningHash     = Animator.StringToHash("IsMining");
    private static readonly int IsInVehicleHash  = Animator.StringToHash("IsInVehicle");
    private static readonly int IsDrillingHash   = Animator.StringToHash("IsDrilling");

    [Header("곡괭이 손 부착 오프셋 (RightHand 기준)")]
    [SerializeField] private Transform pickaxeModel;
    [SerializeField] private Vector3 pickaxeHandLocalPos   = new Vector3(0f, 0.1f, 0.05f);
    [SerializeField] private Vector3 pickaxeHandLocalRot   = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 pickaxeHandLocalScale = new Vector3(1f, 1f, 1f);

    [Header("DrillCar 모드 transform")]
    [SerializeField] private GameObject bulldozerVisualObject;
    [SerializeField] private Vector3 bulldozerOffset      = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 bulldozerEuler       = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 bulldozerScale       = new Vector3(1f, 1f, 1f);

    void Awake()
    {
        // Animator는 Visual 자식(FBX 루트)에 있으므로 하위 탐색
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        rightHandBone = FindBoneRecursive(transform, "mixamorig:RightHand");
        AttachPickaxeToHand();

        // 불도저 비주얼 초기 transform 설정
        if (bulldozerVisualObject != null)
        {
            bulldozerVisualObject.transform.localPosition    = bulldozerOffset;
            bulldozerVisualObject.transform.localEulerAngles = bulldozerEuler;
            bulldozerVisualObject.transform.localScale       = bulldozerScale;
            bulldozerVisualObject.SetActive(false);
        }
    }

    public void SetSpeed(float speed)
    {
        animator?.SetFloat(SpeedHash, speed);
    }

    public void SetMining(bool isMining)
    {
        animator?.SetBool(IsMiningHash, isMining);
    }

    public void SetInVehicle(bool inVehicle)
    {
        animator?.SetBool(IsInVehicleHash, inVehicle);
    }

    public void SetDrilling(bool isDrilling)
    {
        animator?.SetBool(IsDrillingHash, isDrilling);
    }

    public void SetBulldozerMode(bool active)
    {
        animator?.SetBool(IsInVehicleHash, active);
        if (bulldozerVisualObject != null)
            bulldozerVisualObject.SetActive(active);

        // 불도저 모드 시 플레이어 캐릭터 메시 숨기기 (PlayerCharacter_Visual/Mesh)
        var visual = transform.Find("PlayerCharacter_Visual");
        var meshGO = visual != null ? visual.Find("Mesh") : null;
        if (meshGO != null)
            meshGO.gameObject.SetActive(!active);
    }

    public void SetPickaxeVisible(bool visible)
    {
        if (pickaxeModel != null)
            pickaxeModel.gameObject.SetActive(visible);
    }

    private void AttachPickaxeToHand()
    {
        if (rightHandBone == null || pickaxeModel == null) return;
        pickaxeModel.SetParent(rightHandBone, false);
        pickaxeModel.localPosition    = pickaxeHandLocalPos;
        pickaxeModel.localEulerAngles = pickaxeHandLocalRot;
        pickaxeModel.localScale       = pickaxeHandLocalScale;
        pickaxeModel.gameObject.SetActive(false);
    }

    private Transform FindBoneRecursive(Transform root, string boneName)
    {
        if (root.name == boneName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBoneRecursive(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}
