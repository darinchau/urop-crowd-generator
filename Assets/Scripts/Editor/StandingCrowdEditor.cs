using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(StandingCrowdPath))]
public class StandingCrowdEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StandingCrowdPath path = target as StandingCrowdPath;

        path.density = EditorGUILayout.Slider("Density", path.density, 0f, 1f);
        path.randPos = EditorGUILayout.Vector2Field("Random Position", path.randPos);
        path.l = EditorGUILayout.IntSlider("Lambda value", path.l, 1, 6);

        EditorGUILayout.Space();

        // GUI.backgroundColor = Color.green;

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

        // EditorGUILayout.Space();

        // GUI.backgroundColor = Color.red;

        // if (GUILayout.Button("Destroy Humans"))
        // {
        //     path.KillAllHumans();
        // }
    }
}