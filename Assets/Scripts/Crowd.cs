using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

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

[System.Serializable]
public class Crowd : MonoBehaviour {

    // Holds the movement info of the crowd
    [SerializeField] CrowdInfo info;
    CrowdManager cm;

    public void InitializePerson(int pathIdx, int nextWpIndex, bool run, bool back, float speed, Path path, Vector2 finishPos, Vector3[] specPoints) {
        // Make a new crowd info
        info = new CrowdInfo();
        info.pathIdx = pathIdx;
        info.currentTargetIdx = nextWpIndex;
        info.run = run;
        info.back = back;
        info.speed = speed;

        // The path of the crowd human
        info.path = path;
        info.xFinish = finishPos.x;
        info.zFinish = finishPos.y;

        // The base waypoints of the path
        info.specPoints = specPoints;
        
        // Animator info
        info.animationName = run ? "run" : "walk";

        // Diverge. Wait 5 seconds before first diverge is available
        info.divergable = 5f;
        info.rejected = new List<GameObject>();

        // Add the nav mesh agent component
        NavMeshAgent n = Utility.GetOrAddComponent<NavMeshAgent>(gameObject);
        n.speed = speed;
        n.radius = 0.3f;
        n.height = 1.85f;
    }

    // Start is called once in the beginning. Initialize the animation controller at the start.
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        animator.CrossFade(info.animationName, 0.1f, 0, Random.Range(0.0f, 1.0f));
        animator.speed = info.run ? info.speed / 3f : info.speed * 1.2f;
        cm = CrowdManager.Instance;
    }

    // Update is called once every frame. This updates the position of humans
    // TODO add probing. If too close to other humans and/or game objects (like trees or traffic light), then do something about it.
    void Update ()
    {   
        NavMeshAgent n = GetComponent<NavMeshAgent>();

        // Set base finish position, handle slopes and calculate target
        // Check diverge
        // Calculate current distance (horizontal) to target
        // If close enough to the finish and we are really finishing on the path
        //      Destroy oneself and spawn another human
        // If close enough to the finish, but the finish is not the last point
        //      Update the target first
        // Get the target direction vector
        // If there is something directly in front, then probe the environment and attempt to go around whatever that is, and recalculate the target direction vector
        // Move according to the target

        // Check divergence, and update base finish pos to specifiedPoints[currentTargetIdx]
        Vector3 baseFinishPos = info.specPoints[info.currentTargetIdx];

        // Perform raycast to handle slopes
        RaycastHit hit;

        // Shift character up if raycast hits anything that went into the human feet
        if(Physics.Raycast(transform.position + new Vector3(0, 2, 0), -transform.up, out hit))
        {
            baseFinishPos.y = hit.point.y;
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
        
        Vector3 targetPos = GetTargetPos(baseFinishPos, info.xFinish, info.zFinish);

        // Calculate the horizontal straight line distance between the current position and the finish position
        float hDist = Utility.HDist(transform.position, targetPos);
        
        // If close enough to the next waypoint then we perform some additional updates on waypoints
        bool hasNextWaypoint = (!info.back && info.currentTargetIdx < info.path.waypoints.Count) || (info.back && info.currentTargetIdx > 0);

        if (hDist < info.speed * cm.closeEnoughDistance && hasNextWaypoint) {
            int nextIdx = info.back ? info.currentTargetIdx - 1 : info.currentTargetIdx + 1;
            targetPos = targetPos = GetTargetPos(info.specPoints[nextIdx], info.xFinish, info.zFinish);
            info.currentTargetIdx = nextIdx;
        }
        else if (hDist < info.speed * cm.closeEnoughFinalDistance && !hasNextWaypoint) {
            CycleOfLife(info.pathIdx);
            return;
        }

        n.SetDestination(targetPos);
    }


    // Get the target vector
    public Vector3 GetTargetPos(Vector3 baseFinishPos, float xFinish, float zFinish) {
        // Set the target position as transform position and randomized finish position
        // Chop the y component
        Vector3 finishPos = new Vector3(baseFinishPos.x + info.xFinish, baseFinishPos.y, baseFinishPos.z + info.zFinish);
        Vector3 targetPos = new Vector3(finishPos.x, transform.position.y, finishPos.z);
        return targetPos;
    }

    // Like a lotus, it opens or closes, dies and is born again. Such is the cycle of life :)
    public void CycleOfLife(int pathIdx) {
        // Close enough to final waypoint. Time to kill
        // GG na
        info.path.SpawnPerson(info.pathIdx, true);
        Destroy(gameObject);
    }
}