using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CrowdInfo {
    // Runtime constants (more or less)
    public float speed;
    public bool run;
    public float xFinish;
    public float zFinish;
    public string animationName;

    // Total number of waypoints
    public Vector3[] specPoints;

    public Vector3 spawnPos;

    // Runtime variables
    public int currentTargetIdx;

    // Information about the path
    public bool back;
    public Path path;
    public int pathIdx;

    // Check the diverge. Rejected is the previously rejected waypoints so we do not recalculate it on every frame
    // Divergable is like the frame count. If divergable == 0 then we can diverge
    // We need to update the specifiedPoints, currentTargetIdx, back, path shall we diverge
    public float divergable;
    public List<GameObject> rejected;
}

public class CrowdManager : Singleton<CrowdManager>
{
    // The distance one must be close enough in order to diverge to a new path
    public float divergeThreshold = 5f;

    // Rotation speed of humans
    [Range(0.1f, 10f)] public float rotationSpeed = 1.3f;
    [Range(0.1f, 10f)] public float avoidanceRotationSpeed = 2.2f;

    // Minimum time between two diverges
    [Range(1f, 10f)] public float minTimeBetweenDiverge = 15f;

    // A list to hold paths and all waypoints
    public List<Path> paths = new List<Path>();
    public List<GameObject> allWaypoints = new List<GameObject>();

    // List that stores all humans
    public List<Crowd> population = new List<Crowd>();

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

    // Register the person
    public void RegisterPerson(Crowd c) {
        population.Add(c);
    }

    // Deregister the person. This method should be called when the person is despawned. Returns true if the person is successfully removed from the list, false otherwise
    public bool DeregisterPerson(Crowd c) {
        try {
            population.Remove(c);
            return true;
        }
        catch {
            return false;
        }
    }

    // returns true if successfully diverged, false otherwise
    // This is called like 10000 times a second so better to optimze the heck out of it
    public bool CheckDiverge(Vector3 currentPos, ref CrowdInfo info, bool force = false) {
        if (info.divergable <= 0 || force) {
            // First optimization is to not actually do all the list operations and for loop stuff every frame
            // Offset it a bit so not everyone calls this function at the same time
            // Let's not call RNG here either since RNG call can be expensive too
            info.divergable = 0.5f;
            // Check each waypoint in all the registered waypoints
            for(int i = 0; i < allWaypoints.Count; i++) {
                // If the waypoint is not in the current path, then calculate the distance and see if it is close enough
                GameObject wp = allWaypoints[i];

                // Check if the waypoint is already rejected. We do not want to consider them every frame
                bool isRejected = info.rejected.Contains(wp);

                // Get horizontal distance
                float hDist = Utility.HDist(currentPos, wp.transform.position);

                // If far enough away and rejected already, then remove it from the rejected list
                // Too far away anyway - should not diverge there even if force
                if (hDist > divergeThreshold) {
                    if (isRejected) info.rejected.Remove(wp); 
                    continue;
                }

                // If the waypoint is on the current path or it is already rejected then continue;
                if (!force && isRejected) continue;
                if (info.path.waypoints.Contains(wp)) continue;

                // Now that it passed all the tests, try to see if the RNG Gods want it to diverge
                if (!force && UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(info.path.stickyness, 0.25f)) {
                    // Whoops RNG gods say no. Reject it
                    info.rejected.Add(wp);
                    continue;
                }

                // Yay RNG says yes
                info.path = GetWpPath(wp, ref info.currentTargetIdx);
                info.pathIdx = UnityEngine.Random.Range(0, info.path.pathWidth);
                info.specPoints = info.path.points[info.pathIdx];

                // Check which side of the path fits the contour more, and decide whther to go backwards or not
                if (info.currentTargetIdx == info.path.waypoints.Count + 1) {
                    info.back = true;
                } 
                else if (info.currentTargetIdx == 1) {
                    info.back = false;
                }
                else {
                    // Calculate the next waypoint vector and dot it with the direction we are currently going. The smaller angle one will be the new contour
                    Vector3 fw = info.specPoints[info.currentTargetIdx + 1] - info.specPoints[info.currentTargetIdx];
                    Vector3 bw = info.specPoints[info.currentTargetIdx - 1] - info.specPoints[info.currentTargetIdx];
                    Vector3 current = info.specPoints[info.currentTargetIdx] - currentPos;

                    info.back = Utility.HAngle(fw, current) > Utility.HAngle(bw, current);
                }

                // reset diverge countdown
                info.divergable = minTimeBetweenDiverge;
                return true;
            }
        } 
        else {
            info.divergable -= Time.deltaTime;
        }
        return false;
    }
}
