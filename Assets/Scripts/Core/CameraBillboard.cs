using UnityEngine;

// 카메라 방향으로 항상 정면을 향하게 하는 빌보드 컴포넌트.
// 3D TextMeshPro 레이블에 부착하여 카메라 시점에서 읽기 쉽게 함.
public class CameraBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}
