using UnityEngine;

// 타이틀 이미지에 부착. 커졌다 작아졌다 호흡 애니메이션.
public class TitleUI : MonoBehaviour
{
    [SerializeField] private float breatheSpeed  = 1.2f;  // 사이클 빠르기
    [SerializeField] private float breatheAmount = 0.06f; // 최대 스케일 변화량

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * breatheSpeed * Mathf.PI) * breatheAmount;
        transform.localScale = baseScale * s;
    }
}
