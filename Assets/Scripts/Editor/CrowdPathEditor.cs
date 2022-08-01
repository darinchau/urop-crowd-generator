using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(CrowdPath))]
public class CrowdPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CrowdPath path = target as CrowdPath;

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;

        // if (GUILayout.Button("Populate"))
        // {
        //     path.Populate();
        // }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Update Points"))
        {
            path.UpdatePointSet();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto fill Waypoints"))
        {
            path.AutoFillWps();
        }

        EditorGUILayout.Space();

    //     GUI.backgroundColor = Color.red;

    //     if (GUILayout.Button("Destroy Humans"))
    //     {
    //         path.KillAllHumans();
    //     }
    }
}