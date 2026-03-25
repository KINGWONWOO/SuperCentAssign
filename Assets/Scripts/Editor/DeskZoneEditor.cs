using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(DeskZone))]
public class DeskZoneEditor : Editor
{
    private BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    void OnSceneGUI()
    {
        DeskZone zone = (DeskZone)target;
        BoxCollider box = zone.GetComponent<BoxCollider>();
        if (box == null) return;

        boundsHandle.center = zone.transform.TransformPoint(box.center);
        boundsHandle.size   = Vector3.Scale(box.size, zone.transform.lossyScale);

        boundsHandle.handleColor = new Color(0f, 1f, 1f, 1f);
        boundsHandle.wireframeColor = new Color(0f, 1f, 1f, 0.7f);

        EditorGUI.BeginChangeCheck();
        boundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(box, "Resize DeskZone");
            Vector3 lossyScale = zone.transform.lossyScale;
            box.center = zone.transform.InverseTransformPoint(boundsHandle.center);
            box.size   = new Vector3(
                boundsHandle.size.x / lossyScale.x,
                boundsHandle.size.y / lossyScale.y,
                boundsHandle.size.z / lossyScale.z);
        }
    }
}
