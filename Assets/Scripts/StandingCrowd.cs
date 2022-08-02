using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class StandingCrowd : Crowd
{
    private float countdown = 5f;

    // Start is called once in the beginning. Initialize the animation controller at the start.
    public override void Start()
    {
        base.Start();
        Animator animator = GetComponent<Animator>();
        animator.Play(info.animationName, -1, UnityEngine.Random.Range(0f, 1f));
    }

    int destroyed = 0;

    public override void Update() {
        if (countdown > 0) {
            NavMeshAgent n = GetComponent<NavMeshAgent>();
            n.SetDestination(info.spawnPos);
            countdown -= Time.deltaTime;
        }
        else if (destroyed == 0) {
            NavMeshAgent n = GetComponent<NavMeshAgent>();
            Destroy(n);
            destroyed = 1;
        }
        else if (destroyed == 1) {
            SetNavMesh();
            destroyed = 2;
        }
    }
}
