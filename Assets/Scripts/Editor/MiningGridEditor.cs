using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MiningGrid))]
public class MiningGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "씬 뷰에서 Gizmos를 켜면 오렌지 와이어큐브로 돌 스폰 위치가 표시됩니다.\n" +
            "MiningGrid 오브젝트를 이동하면 전체 그리드가 이동합니다.",
            MessageType.Info);
    }
}
