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

    [Tooltip("Make the path closed in the ring")] public bool loopPath;

    [Tooltip("The probability that someone stays within a path when it could divert to a new waypoint")] [Range(0f, 1f)]  public float stickyness = 0.6f;

    [Tooltip("Offset from the line along the axis")] public Vector2 randPos = new Vector2(0.3f, 0.3f);

    [Tooltip("The distances between waypoints when you press the auto fill button")] public float autoFillDistance = 3f;

    [Tooltip("Kill the human when it reaches the start")] public bool killAtStart = true;
    [Tooltip("Kill the human when it reaches the end")] public bool killAtEnd = true;

    
    [HideInInspector] public float spacing = 0.6f;
    [HideInInspector] public List<GameObject> waypoints = new List<GameObject>();
    // Each element in the points list is a path drawn with a green line in the scene view
    [HideInInspector] public List<Vector3[]> points;

    // Abstract methods. Spawn person spawns one person. Populate spawns multiple people, Draw curve gizmos draws the curvy gizmos, and Recalculate point recalculates points, returns the number of waypoints
    public virtual void Spawn(int pathIdx, bool runtime) { }
    public virtual void Populate() { }
    public virtual void DrawCurveGizmos() { }
    public virtual void RecalculatePoint() { }

    // Updates the point set in case something gone wrong
    public void UpdatePointSet()
    {
        // Flush list
        waypoints = new List<GameObject>();

        Transform t = transform.Find("points");

        int count = t.childCount;

        for(int i = 0; i < count; i++) {
            waypoints.Add(t.GetChild(i).gameObject);
        }

        RenameWps();
    }

    // Updates the point set in case something gone wrong
    public void KillAllHumans()
    {
        Transform t = transform.Find("people");
        // int count = t.childCount;

        // You can't remove the looped thing in a foreach loop so we use this implementation insteads
        // We also need to copy the counts over to another integer variable first otherwise checking will gg
        while (t.childCount > 0)
        {
            GameObject child = t.GetChild(0).gameObject;
            DestroyImmediate(child);
        }
    }


    // Automatically fill the waypoints to increase waypoint density
    public void AutoFillWps() {
        Transform t = transform.Find("points");
        List<GameObject> childs = new List<GameObject>();
        for (int i = 0; i < t.childCount; i++) {
            childs.Add(t.GetChild(i).gameObject);
        }

        for(int i = 0; i < childs.Count- 1; i++) {
            GameObject wp1 = childs[i];
            GameObject wp2 = childs[i + 1];
            int actualIdx = wp2.transform.GetSiblingIndex();
            AutoFillWpSingle(wp1, wp2, actualIdx);
        }

        UpdatePointSet();
    }

    // Fills the waypoints between two consecutive waypoints
    private void AutoFillWpSingle(GameObject wp1, GameObject wp2, int idx2) {
        float hDist = Utility.HDist(wp1.transform.position, wp2.transform.position);

        for (int numToFill = (int)(hDist / autoFillDistance); numToFill > 0; numToFill--) {
            Vector3 direction = wp2.transform.position - wp1.transform.position;
            Vector3 newPos = wp1.transform.position + direction.normalized * autoFillDistance * numToFill;
            GameObject newWp = Instantiate(wp1, newPos, Quaternion.identity) as GameObject;
            newWp.name = "p" + idx2.ToString() + " (" + numToFill.ToString() + ")";
            newWp.transform.parent = wp1.transform.parent;
            newWp.transform.SetSiblingIndex(idx2);
        }
    }

    private void RenameWps() {
        Transform t = transform.Find("points");
        for (int i = 0; i < t.childCount; i++) {
            GameObject g = t.GetChild(i).gameObject;
            string name = "p" + i.ToString();
            g.name = name;
        }
    }

    // Start is called once at start. If you want to implement a custom start method please call this base method
    public virtual void Start() {
        CrowdManager.Instance.RegisterPath(this);
        Populate();
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawCurveGizmos();
    }
#endif
}
