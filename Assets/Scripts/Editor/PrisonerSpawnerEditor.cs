using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrisonerSpawner))]
public class PrisonerSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Spawn/Wait/Desk 마커 Transform 아래의 MarkerVis 자식을 이동하면\n" +
            "실제 수감자 경로가 자동으로 변경됩니다.\n" +
            "씬 뷰에서 Gizmos를 켜면 위치가 시각적으로 표시됩니다.",
            MessageType.Info);

        GUILayout.Space(4);
        if (GUILayout.Button("MarkerVis 시각 마커 새로고침", GUILayout.Height(28)))
            RefreshMarkers((PrisonerSpawner)target);
    }

    private void RefreshMarkers(PrisonerSpawner spawner)
    {
        var so = new SerializedObject(spawner);
        string[] fields = { "spawnPoint", "waitPosition", "deskPosition" };
        Color[] colors = { new Color(1f,0.2f,0.2f,0.5f), new Color(1f,0.9f,0f,0.5f), new Color(0.2f,0.8f,1f,0.5f) };

        for (int i = 0; i < fields.Length; i++)
        {
            var prop = so.FindProperty(fields[i]);
            var t = prop.objectReferenceValue as Transform;
            if (t == null) continue;

            var mv = t.Find("MarkerVis");
            if (mv == null)
            {
                var mvGO = new GameObject("MarkerVis");
                mvGO.transform.SetParent(t, false);
                mv = mvGO.transform;
                Undo.RegisterCreatedObjectUndo(mvGO, "Create MarkerVis");
            }

            var sphere = mv.Find("Sphere");
            if (sphere == null)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Sphere";
                s.transform.SetParent(mv, false);
                s.transform.localScale = Vector3.one * 0.5f;

                var mr = s.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = colors[i];
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                mr.sharedMaterial = mat;
                DestroyImmediate(s.GetComponent<SphereCollider>());
                Undo.RegisterCreatedObjectUndo(s, "Create MarkerVis Sphere");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log("[PrisonerSpawnerEditor] MarkerVis markers refreshed.");
    }
}
