using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdManager : Singleton<CrowdManager>
{
    // The distance and angle in which we perform probing for other humans
    public static float socialDistance = 0.3f;

    // The distance and angle in which we perform probing for objects in the scene
    public static float objectDistance = 1.2f;

    // The distance one must be close enough in order to diverge to a new path
    public float divergeThreshold = 5f;

    // Rotation speed of humans
    [Range(0.1f, 10f)] public float rotationSpeed = 1.3f;
    [Range(0.1f, 10f)] public float avoidanceRotationSpeed = 2.2f;

    // Minimum time between two diverges
    [Range(1f, 10f)] public float minTimeBetweenDiverge = 15f;


    // Whether the human is close enough to the cp
    [Range(0.1f, 10f)] public float closeEnoughDistance = 1f;

    [Range(1f, 10f)] public float closeEnoughFinalDistance = 0.2f;

    // A list to hold paths and all waypoints
    public List<Path> paths = new List<Path>();
    public List<GameObject> allWaypoints = new List<GameObject>();

    public void RegisterPath(Path p) {
        paths.Add(p);
        for (int i = 0; i < p.waypoints.Count; i++) {
            allWaypoints.Add(p.waypoints[i]);
        }
    }

    public Path GetWpPath(GameObject waypoint, ref int wpIdx) {
        for (int i = 0; i < paths.Count; i++) {
            Path p = paths[i];
            for(int j = 0; j < p.waypoints.Count; j++) {
                if (p.waypoints[j] == waypoint) {
                    wpIdx = j + 1;
                    return p;
                }
            }
        }
        Debug.LogWarning("No path with specified waypoint found!");
        return paths[0];
    }
}
