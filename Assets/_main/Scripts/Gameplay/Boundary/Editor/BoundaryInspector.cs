using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


[CustomEditor(typeof(BoundaryAuthoring))]
public class BoundaryInspector : Editor
{
    bool m_Editing;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_Editing = GUILayout.Toggle(m_Editing, "Edit", "Button", GUILayout.MaxWidth(100));
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        var ba = (BoundaryAuthoring)target;
        if (m_Editing)
        {
            Undo.RecordObject(target, "Boundary min/max adjustment");
            ba.Min = Handles.DoPositionHandle(ba.Min, Quaternion.identity);
            ba.Max = Handles.DoPositionHandle(ba.Max, Quaternion.identity);

            Handles.color = Color.blue;
            Handles.DrawSolidDisc(ba.Min, Vector3.back, 0.1f);
            Handles.DrawSolidDisc(ba.Max, Vector3.back, 0.1f);
        }

        Handles.color = Color.black;
        Handles.DrawDottedLines(new Vector3[] { 
            ba.Min, 
            new Vector3(ba.Min.x, ba.Max.y),
            new Vector3(ba.Min.x, ba.Max.y),
            ba.Max, 
            ba.Max, 
            new Vector3(ba.Max.x, ba.Min.y),
            new Vector3(ba.Max.x, ba.Min.y),
            ba.Min, 
        },1);
    }
}
