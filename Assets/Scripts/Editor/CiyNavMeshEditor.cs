using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(CityNavMesh))]
public class CityNavMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityNavMesh m = target as CityNavMesh;

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("Update NavMesh"))
        {
            m.UpdateNavMesh();
        }

        EditorGUILayout.Space();
    }
}
