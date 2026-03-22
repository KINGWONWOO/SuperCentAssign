using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 존 바닥에 점선 테두리를 그리고 플레이어가 진입하면 흰색 → 형광 녹색으로 전환.
/// BoxCollider 크기를 자동으로 읽어 테두리를 구성.
/// DropZone / DeskZone과 같은 오브젝트에 추가.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneDashedOutline : MonoBehaviour
{
    [Header("색상")]
    [SerializeField] private Color idleColor   = Color.white;
    [SerializeField] private Color activeColor = new Color(0.0f, 1.0f, 0.35f); // 형광 녹색

    [Header("점선 스타일")]
    [SerializeField] private float dashLen = 0.30f;
    [SerializeField] private float gapLen  = 0.18f;
    [SerializeField] private float dashW   = 0.06f;
    [SerializeField] private float dashH   = 0.018f;

    private Material dashMat;
    private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
    private GameObject borderRoot;

    void Start()
    {
        // 기존 정적 DashedBorder 제거 (빌더 도구로 생성된 것)
        var old = transform.Find("DashedBorder");
        if (old != null) Destroy(old.gameObject);

        // BoxCollider에서 크기 읽기
        var bc = GetComponent<BoxCollider>();
        float w = bc != null ? bc.size.x : 2f;
        float d = bc != null ? bc.size.z : 2f;

        BuildBorder(w, d);
        ApplyColor(idleColor);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerInteraction>() != null)
            ApplyColor(activeColor);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInteraction>() != null)
            ApplyColor(idleColor);
    }

    // ── 점선 생성 ──────────────────────────────────────────────────

    private void BuildBorder(float width, float depth)
    {
        borderRoot = new GameObject("DashedBorder");
        borderRoot.transform.SetParent(transform, false);
        borderRoot.transform.localPosition = new Vector3(0f, 0.005f, 0f);

        dashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        dashMat.color = idleColor;

        float hx = width  * 0.5f;
        float hz = depth  * 0.5f;

        // 4변 점선
        BuildEdge(new Vector3(-hx, 0, -hz), new Vector3( hx, 0, -hz), true);  // 앞(−Z)
        BuildEdge(new Vector3(-hx, 0,  hz), new Vector3( hx, 0,  hz), true);  // 뒤(+Z)
        BuildEdge(new Vector3(-hx, 0, -hz), new Vector3(-hx, 0,  hz), false); // 왼(−X)
        BuildEdge(new Vector3( hx, 0, -hz), new Vector3( hx, 0,  hz), false); // 오른(+X)
    }

    private void BuildEdge(Vector3 start, Vector3 end, bool xAxis)
    {
        float total  = Vector3.Distance(start, end);
        float stride = dashLen + gapLen;
        int   count  = Mathf.FloorToInt((total + gapLen) / stride);

        // 테두리를 정중앙에 맞추기
        float usedLen = count * dashLen + Mathf.Max(0, count - 1) * gapLen;
        Vector3 dir    = (end - start).normalized;
        Vector3 origin = (start + end) * 0.5f - dir * usedLen * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float centerOffset = i * stride + dashLen * 0.5f;
            Vector3 center = origin + dir * centerOffset;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Dash";
            cube.transform.SetParent(borderRoot.transform, false);
            cube.transform.localPosition = center;
            cube.transform.localScale = xAxis
                ? new Vector3(dashLen, dashH, dashW)
                : new Vector3(dashW,   dashH, dashLen);

            Destroy(cube.GetComponent<BoxCollider>());

            var mr = cube.GetComponent<MeshRenderer>();
            mr.sharedMaterial = dashMat;
            renderers.Add(mr);
        }
    }

    // ── 색상 전환 ──────────────────────────────────────────────────

    private void ApplyColor(Color c)
    {
        if (dashMat == null) return;
        dashMat.color = c;

        // URP Unlit Emission 시뮬레이션: activeColor일 때 밝게
        bool glow = (c == activeColor);
        if (dashMat.HasProperty("_EmissionColor"))
        {
            if (glow) dashMat.EnableKeyword("_EMISSION");
            else      dashMat.DisableKeyword("_EMISSION");
            dashMat.SetColor("_EmissionColor", glow ? c * 1.8f : Color.black);
        }
    }
}
