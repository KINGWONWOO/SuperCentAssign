using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CellManager))]
public class CellManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("스택 포인트 마커 생성 (20개)", GUILayout.Height(32)))
            GenerateStackPoints((CellManager)target);
    }

    private void GenerateStackPoints(CellManager cm)
    {
        SerializedObject so = new SerializedObject(cm);
        SerializedProperty cellRootProp = so.FindProperty("cellRoot");
        SerializedProperty stackPointsProp = so.FindProperty("stackPoints");

        Transform cellRoot = cellRootProp.objectReferenceValue as Transform;
        if (cellRoot == null)
        {
            EditorUtility.DisplayDialog("오류", "cellRoot가 할당되지 않았습니다.", "확인");
            return;
        }

        // Remove existing StackPoint children
        var toDelete = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in cellRoot)
            if (t.name.StartsWith("StackPoint_")) toDelete.Add(t);
        foreach (var t in toDelete)
            DestroyImmediate(t.gameObject);

        int count = 20;
        stackPointsProp.arraySize = count;

        Quaternion rot = cellRoot.rotation;
        Vector3 origin = cellRoot.position;

        // Use MarkerVis if present
        var mv = cellRoot.Find("MarkerVis");
        if (mv != null) origin = mv.position;

        for (int i = 0; i < count; i++)
        {
            int col = i % 5, row = i / 5;
            Vector3 offset = rot * new Vector3(col * 1.5f, 0f, row * 1.5f);
            Vector3 pos = origin + offset;

            GameObject marker = new GameObject($"StackPoint_{i:D2}");
            marker.transform.SetParent(cellRoot, false);
            marker.transform.position = pos;
            marker.transform.rotation = rot;

            // Visual cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(marker.transform, false);
            cube.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            cube.transform.localScale = new Vector3(0.3f, 1.8f, 0.3f);
            // Green semi-transparent material
            var mr = cube.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;
            // Remove collider
            DestroyImmediate(cube.GetComponent<BoxCollider>());

            // Number label via TextMesh
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(marker.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text = i.ToString();
            tm.fontSize = 24;
            tm.color = Color.white;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            stackPointsProp.objectReferenceValue = null; // will set below
            stackPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = marker.transform;

            Undo.RegisterCreatedObjectUndo(marker, "Create StackPoint");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cm.gameObject.scene);
        Debug.Log($"[CellManagerEditor] {count}개 스택 포인트 마커 생성 완료.");
    }
}
