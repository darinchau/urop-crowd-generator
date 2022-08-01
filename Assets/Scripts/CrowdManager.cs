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


    // Decide whether the human should diverge
    // t is the human transform, pathidx is the path index of the human, other things are updated by reference
    public Vector3 Diverge(Vector3 currentPos, ref int pathIdx, ref Vector3[] specPoints, ref int currentTargetIdx, ref bool back, ref Path path, ref List<GameObject> rejected, ref float divergable) {
        
        if (divergable <= 0) {
            // Check each waypoint in all the registered waypoints
            for(int i = 0; i < allWaypoints.Count; i++) {
                // If the waypoint is not in the current path, then calculate the distance and see if it is close enough
                GameObject wp = allWaypoints[i];
                
                // Check if the waypoint is already rejected. We do not want to consider them every frame
                bool isRejected = rejected.Contains(wp);

                // Get horizontal distance
                float hDist = HDist(currentPos, wp.transform.position);

                // If far enough away and rejected already, then remove it from the rejected list
                if (hDist > divergeThreshold) {
                    if (isRejected) rejected.Remove(wp); 
                    continue;
                }
                
                // If the waypoint is on the current path or it is already rejected then continue;
                if (isRejected) continue;
                if (path.waypoints.Contains(wp)) continue;

                // Now that it passed all the tests, try to see if the RNG Gods want it to diverge
                if (UnityEngine.Random.Range(0f, 1f) < path.stickyness) {
                    // Whoops RNG gods say no. Reject it
                    rejected.Add(wp);
                    continue;
                }

                // Yay RNG says yes
                path = GetWpPath(wp, ref currentTargetIdx);
                pathIdx = UnityEngine.Random.Range(0, path.pathWidth);
                specPoints = path.GetSpecPoints(pathIdx);

                // Check which side of the path fits the contour more, and decide whther to go backwards or not
                if (currentTargetIdx == 0) {

                }
                if (currentTargetIdx == path.waypoints.Count) {
                    back = true;
                } 
                else if (currentTargetIdx == 1) {
                    back = false;
                }
                else {
                    // Calculate the next waypoint vector and dot it with the direction we are currently going. The smaller angle one will be the new contour
                    Vector3 fw = specPoints[currentTargetIdx + 1] - specPoints[currentTargetIdx];
                    Vector3 bw = specPoints[currentTargetIdx - 1] - specPoints[currentTargetIdx];
                    Vector3 current = specPoints[currentTargetIdx] - currentPos;

                    back = HAngle(fw, current) > HAngle(bw, current);
                }

                // Turn off diverge flag so that the human wont diverge anymore
                divergable = minTimeBetweenDiverge;

                break;
            }
        } 
        else {
            divergable -= Time.deltaTime;
        }

        return specPoints[currentTargetIdx];
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

    // We also put the helper methods here
    public static bool isSameVector(Vector3 a, Vector3 b) {
        return (a-b).magnitude < 1e-5;
    }

    public static float HDist(Vector3 a, Vector3 b) {
        return Vector3.Distance(Vector3.ProjectOnPlane(a, Vector3.up), Vector3.ProjectOnPlane(b, Vector3.up));
    }

    public static float HAngle(Vector3 a, Vector3 b) {
        return Vector3.Angle(Vector3.ProjectOnPlane(a, Vector3.up), Vector3.ProjectOnPlane(b, Vector3.up));
    }
}
