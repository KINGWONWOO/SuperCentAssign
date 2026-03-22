using UnityEngine;
using UnityEditor;

/// <summary>
/// OreDropZone / HandcuffPickupZone의 기존 시각 오브젝트를 제거하고
/// 바닥 점선 사각형으로 교체.
/// 메뉴: Game → Rebuild Dashed Zones
/// </summary>
public static class DashedZoneBuilder
{
    // 점선 파라미터
    private const float DASH_LEN   = 0.30f;  // 선 길이
    private const float GAP_LEN    = 0.18f;  // 빈 간격
    private const float DASH_W     = 0.06f;  // 선 두께 (단변)
    private const float DASH_H     = 0.018f; // 선 높이 (바닥 위 돌출)
    private const float Y_POS      = 0.005f; // 바닥면 위 y 오프셋

    [MenuItem("Game/Rebuild Dashed Zones")]
    public static void RebuildDashedZones()
    {
        ProcessZone("OreDropZone",        new Color(1.00f, 0.80f, 0.10f), 3.50f, 3.00f);
        ProcessZone("HandcuffPickupZone", new Color(0.20f, 0.75f, 1.00f), 4.00f, 2.50f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[DashedZoneBuilder] Done.");
    }

    private static void ProcessZone(string zoneName, Color color, float width, float depth)
    {
        var go = GameObject.Find(zoneName);
        if (go == null) { Debug.LogWarning($"[DashedZoneBuilder] {zoneName} not found."); return; }

        // ── 기존 시각 오브젝트 제거 ────────────────────────────────
        var toDelete = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i);
            string n = child.name;
            // Tile, Corner, DashedBorder 제거. StackRoot, Label, MarkerVis는 보존
            if (n == "Tile" || n.StartsWith("Corner") || n == "DashedBorder")
                toDelete.Add(child.gameObject);
        }
        foreach (var d in toDelete)
            Undo.DestroyObjectImmediate(d);

        // ── 점선 테두리 생성 ──────────────────────────────────────
        var border = new GameObject("DashedBorder");
        Undo.RegisterCreatedObjectUndo(border, "Create DashedBorder");
        border.transform.SetParent(go.transform, false);
        border.transform.localPosition = new Vector3(0f, Y_POS, 0f);

        // URP Unlit 머티리얼
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;
        // 살짝 emissive 효과 (HDR 값)
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.6f);
        }

        float hx = width  * 0.5f;
        float hz = depth  * 0.5f;

        // 4개 엣지 각각 점선 생성
        BuildEdge(border.transform, mat, new Vector3(-hx, 0, -hz), new Vector3( hx, 0, -hz), true);  // 앞(−Z)
        BuildEdge(border.transform, mat, new Vector3(-hx, 0,  hz), new Vector3( hx, 0,  hz), true);  // 뒤(+Z)
        BuildEdge(border.transform, mat, new Vector3(-hx, 0, -hz), new Vector3(-hx, 0,  hz), false); // 왼(−X)
        BuildEdge(border.transform, mat, new Vector3( hx, 0, -hz), new Vector3( hx, 0,  hz), false); // 오른(+X)

        Debug.Log($"[DashedZoneBuilder] {zoneName} rebuilt.");
    }

    // start~end 사이에 점선 큐브 배치
    // isXAxis=true  → 큐브가 X 방향으로 늘어남 (scale.x=dashLen, scale.z=dashW)
    // isXAxis=false → 큐브가 Z 방향으로 늘어남 (scale.x=dashW, scale.z=dashLen)
    private static void BuildEdge(Transform parent, Material mat,
                                   Vector3 start, Vector3 end, bool isXAxis)
    {
        float totalLen = Vector3.Distance(start, end);
        float stride   = DASH_LEN + GAP_LEN;
        int count = Mathf.FloorToInt(totalLen / stride);

        // 전체 실제 길이 중앙 정렬
        float usedLen = count * stride - GAP_LEN; // 마지막 gap 제거
        Vector3 dir = (end - start).normalized;
        Vector3 origin = (start + end) * 0.5f - dir * usedLen * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 center = origin + dir * (i * stride + DASH_LEN * 0.5f);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Dash");
            cube.name = "Dash";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = center;

            if (isXAxis)
                cube.transform.localScale = new Vector3(DASH_LEN, DASH_H, DASH_W);
            else
                cube.transform.localScale = new Vector3(DASH_W, DASH_H, DASH_LEN);

            // 콜라이더 제거 (시각 전용)
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());

            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // 남은 길이에 짧은 보정 대시 추가 (옵션)
        float remainder = totalLen - usedLen - GAP_LEN;
        if (remainder > 0.08f) // 너무 짧으면 생략
        {
            Vector3 center = origin + dir * (count * stride + remainder * 0.5f);
            float len = Mathf.Min(remainder, DASH_LEN);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Dash");
            cube.name = "Dash";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = center;

            if (isXAxis)
                cube.transform.localScale = new Vector3(len, DASH_H, DASH_W);
            else
                cube.transform.localScale = new Vector3(DASH_W, DASH_H, len);

            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
