using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

// Base class for paths that control the behavior of humans
// Its like a path in life :)
[System.Serializable]
public class Path : MonoBehaviour
{
    [Tooltip("People prefabs na")] public GameObject[] people;
    [Tooltip("Width of the path")] public int pathWidth = 8;
    [Tooltip("Density of people")] [Range(0.01f, 1f)] public float density = 0.2f;

    [Tooltip("Distance between people")][Range(0.3f, 1.5f)] public float socialDistance = 0.3f;
    [Tooltip("Make the path closed in the ring")] public bool loopPath;

    [Tooltip("Offset from the line along the X axis")] public Vector2 randPos = new Vector2(0.1f, 0.1f);

    
    [HideInInspector] public float lineSpacing = 0.6f;
    [HideInInspector] public List<GameObject> waypoints = new List<GameObject>();
    [HideInInspector] public Vector3[,] points;

    // Abstract methods. Spawn person spawns one person. Populate spawns multiple people, Draw curve gizmos draws the curvy gizmos, and Recalculate point recalculates points, returns the number of waypoints
    public virtual void SpawnPerson(int pathIdx, bool startAtBeginning) { }
    public virtual void Populate() { }
    public virtual void DrawCurveGizmos() { }
    public virtual int RecalculatePoint() { return 0; }

    // Updates the point set in case something gone wrong
    public void UpdatePointSet()
    {
        // Flush list
        waypoints = new List<GameObject>();

        // Unity magic. Don't ask - actually somehow we can loop transform itself to get all its gameobjects
        foreach (Transform t in transform.GetChild(0)) {
            waypoints.Add(t.gameObject);
        }
    }

    // Updates the point set in case something gone wrong
    public void KillAllHumans()
    {
        Transform t = transform.Find("people");
        int count = t.childCount;

        // You can't remove the looped thing in a foreach loop so we use this implementation insteads
        // We also need to copy the counts over to another integer variable first otherwise checking will gg
        for(int i = 0; i < count; i++)
        {
            GameObject child = t.GetChild(0).gameObject;
            DestroyImmediate(child);
        }
    }


#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawCurveGizmos();
    }
#endif
}
