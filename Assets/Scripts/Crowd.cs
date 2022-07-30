using UnityEngine;
using System.Collections;
using System.Collections.Generic;

struct CrowdInfo {
    // Runtime constants (more or less)
    public float speed;
    public bool run;
    public bool loop;
    public bool back;
    public Path path;
    public int pathIdx;
    public float xFinish;
    public float zFinish;
    public string animationName;

    // Total number of waypoints
    public int n;
    public List<GameObject> waypoints;

    // Runtime variables
    public int currentTargetIdx;
}

[System.Serializable]
public class Crowd : MonoBehaviour {
    CrowdInfo info;

    public void InitializePerson(int pathIdx, int nextWpIndex, bool run, bool back, float speed, Path path, Vector2 finishPos) {
        info.speed = speed;
        info.run = run;
        info.loop = path.loopPath;
        info.back = back;
        info.path = path;
        info.pathIdx = pathIdx;
        info.xFinish = finishPos.x;
        info.zFinish = finishPos.y;

        info.n = path.waypoints.Count;
        info.waypoints = path.waypoints;

        info.currentTargetIdx = nextWpIndex;

        info.animationName = run ? "run" : "walk";
    }

    public bool isSameVector(Vector3 a, Vector3 b) {
        return (a-b).magnitude < 1e-5;
    }


    // Start is called once in the beginning. Initialize the animation controller at the start.
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        animator.CrossFade(info.animationName, 0.1f, 0, Random.Range(0.0f, 1.0f));
        animator.speed = info.run ? info.speed / 3f : info.speed * 1.2f;
    }

    // Update is called once every frame. This updates the position of humans
    // TODO add probing. If too close to other humans and/or game objects (like trees or traffic light), then do something about it.
    void Update ()
    {   
        // Set base finish position, handle slopes and calculate target
        // Calculate current distance (horizontal) to target
        // If close enough to the finish and we are really finishing on the path
        //      Destroy oneself and spawn another human
        // If close enough to the finish, but the finish is not the last point
        //      Update the target first
        // Get the target direction vector
        // If there is something directly in front, then probe the environment and attempt to go around whatever that is, and recalculate the target direction vector
        // Move according to the target

        Vector3 baseFinishPos = info.waypoints[info.currentTargetIdx].transform.position;

        // Perform raycast to handle slopes
        RaycastHit hit;

        // Shift character up if raycast hits anything that went into the human feet
        if(Physics.Raycast(transform.position + new Vector3(0, 2, 0), -transform.up, out hit))
        {
            baseFinishPos.y = hit.point.y;
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
        
        // Set the target position as transform position and randomized finish position
        // Chop the y component
        Vector3 finishPos = new Vector3(baseFinishPos.x + info.xFinish, baseFinishPos.y, baseFinishPos.z + info.zFinish);
        Vector3 targetPos = new Vector3(finishPos.x, transform.position.y, finishPos.z);

        // Calculate the horizontal straight line distance between the current position and the finish position
        float hDist = Vector3.Distance(Vector3.ProjectOnPlane(transform.position, Vector3.up), Vector3.ProjectOnPlane(finishPos, Vector3.up));
        
        // If close enough to the next waypoint then we perform some additional updates on waypoints
        bool hasNextWaypoint = info.currentTargetIdx < info.n;
        bool closeEnough = (info.run && hDist < 0.5f) || (!info.run && hDist < 0.2f);

        if (closeEnough && hasNextWaypoint) {
            int nextIdx = info.back ? info.currentTargetIdx + 1 : info.currentTargetIdx - 1;
            targetPos = info.path.points[info.pathIdx, info.currentTargetIdx + 1];
            targetPos.y = transform.position.y;
            info.currentTargetIdx = nextIdx;
        }
        else if (closeEnough && !hasNextWaypoint) {
            // Close enough to final waypoint. Time to kill
            // GG na
            info.path.SpawnPerson(info.pathIdx, true);
            Destroy(gameObject);
        }

        // Move towards the target
        Vector3 targetVector = targetPos - transform.position;

        // Perform probing here

        // This just makes sure there is no divide by zero error in runtime. Practically this always runs.
        // Update the look direction of humans
        if(targetVector != Vector3.zero)
        {
            Vector3 newDir = Vector3.zero;
            newDir = Vector3.RotateTowards(transform.forward, targetVector, 2 * Time.deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        if (Time.deltaTime > 0)
            transform.position = Vector3.MoveTowards(transform.position, finishPos, Time.deltaTime * 1.0f * info.speed);
    }

}