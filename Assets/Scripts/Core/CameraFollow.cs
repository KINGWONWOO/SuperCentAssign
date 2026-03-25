using UnityEngine;
using System.Collections;

// 플레이어를 부드럽게 따라가는 3인칭 카메라.
// SceneBuilder가 offset/rotation을 세팅 후 player를 할당.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -7f);
    [SerializeField] private float smoothSpeed = 8f;

    private bool isPanning = false;

    void LateUpdate()
    {
        if (isPanning || target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform t) { target = t; }

    // worldPos 위치로 카메라를 이동해 holdDuration초 보여준 뒤 플레이어로 복귀
    public void PanToAndReturn(Vector3 worldPos, float holdDuration)
    {
        StartCoroutine(PanRoutine(worldPos, holdDuration));
    }

    private IEnumerator PanRoutine(Vector3 worldPos, float holdDuration)
    {
        isPanning = true;
        Vector3 dest = worldPos + offset;

        // 이동
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, dest, elapsed / 0.6f);
            yield return null;
        }
        transform.position = dest;

        yield return new WaitForSeconds(holdDuration);

        // 복귀
        isPanning = false;
    }
}
