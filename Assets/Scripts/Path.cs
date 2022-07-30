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
    [Tooltip("Density of people")] [Range(0.01f, 0.50f)] public float Density = 0.2f;

    [Tooltip("Distance between people")][Range(1f, 10f)] public float socialDistance = 0.1f;
    [Tooltip("Make the path closed in the ring")] public bool loopPath;

    [Tooltip("Offset from the line along the X axis")] public Vector2 randPos = new Vector2(0.1f, 0.1f);

    protected float[] _distances;

    [HideInInspector] public int numGizmosLines = 8;
    [HideInInspector] public float lineSpacing = 0.6f;
    [HideInInspector] public List<GameObject> waypoints = new List<GameObject>();
    [HideInInspector] public Vector3[,] points;

    // Abstract methods
    public virtual void SpawnPerson(int pathIdx, bool startAtBeginning) { }
    public virtual void Populate() { }
    public virtual void DrawCurveGizmos() { }

    // Updates the point set in case something gone wrong
    public void UpdatePointSet()
    {
        // Flush list
        waypoints = new List<GameObject>();

        // Unity magic. Don't ask - actually somehow we can loop transform itself to get all its gameobjects
        foreach (Transform t in transform) {
            waypoints.Add(t.gameObject);
        }
    }


#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawCurveGizmos();
    }
#endif
}
