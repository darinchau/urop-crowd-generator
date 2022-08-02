using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WalkingCrowd : MonoBehaviour {

    // Holds the movement info of the crowd
    [SerializeField] CrowdInfo info;
    CrowdManager cm;

    public void InitializePerson(int pathIdx, int nextWpIndex, bool run, bool back, float speed, string animName, Path path, Vector2 finishPos, Vector3[] specPoints) {
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
        info.animationName = animName;

        // Diverge. Add a bit of randomness to this so not everyone calls the diverge checks simultaneously later on
        info.divergable = UnityEngine.Random.Range(4.6f, 5.4f);
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
        // Move according to the target

        bool diverged = cm.CheckDiverge(transform.position, ref info, false);

        // if (diverged) {
        //     Debug.Log("Diverged in path");
        // }

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
            CycleOfLife();
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
    public void CycleOfLife() {
        // Close enough to final waypoint. Time to kill
        // GG na
        // Debug.Log("Such is the story of sun and moon, me and you, and everything else.");
        bool shouldKill = (info.back && info.path.killAtStart) || (!info.back && info.path.killAtEnd);

        if (!shouldKill) {
            bool diverged = cm.CheckDiverge(transform.position, ref info, true);
            if (diverged) return;
        }

        info.path.Spawn(info.pathIdx, true);
        Destroy(gameObject);
    }
}