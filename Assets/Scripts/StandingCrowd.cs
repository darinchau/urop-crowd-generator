using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class StandingCrowd : MonoBehaviour
{
    // Holds the movement info of the crowd
    [SerializeField] CrowdInfo info;
    CrowdManager cm;

    public void InitializePerson(int pathIdx, string animName, Path p) {
        info = new CrowdInfo();
        info.pathIdx = pathIdx;
        info.animationName = animName;
        info.path = p;

        // Add the nav mesh agent component
        NavMeshAgent n = Utility.GetOrAddComponent<NavMeshAgent>(gameObject);
        n.speed = 5f;
        n.radius = 0.3f;
        n.height = 1.85f;
    }

    // Start is called once in the beginning. Initialize the animation controller at the start.
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        animator.Play(info.animationName);
        cm = CrowdManager.Instance;
        NavMeshAgent n = GetComponent<NavMeshAgent>();
    }

    
}
