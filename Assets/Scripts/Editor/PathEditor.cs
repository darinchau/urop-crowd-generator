using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(Path))]
public class PathEditor : Editor
{
    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }
        return assets;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Path path = target as Path;

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("Populate"))
        {
            path.Populate();
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Update Points"))
        {
            path.UpdatePointSet();
        }
    }
}
