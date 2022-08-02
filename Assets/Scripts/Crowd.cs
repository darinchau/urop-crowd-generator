using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Crowd : MonoBehaviour
{
    // Holds the movement info of the crowd
    [SerializeField] protected CrowdInfo info;
    public CrowdManager cm;
    public ScreenCapturer sc;

    // Width and height of human
    public static float radius = 0.3f;
    public static float height = 1.85f;

    // Whether the human is close enough to the cp
    [Range(0.1f, 10f)] public float closeEnoughDistance = 0.7f;
    [Range(1f, 10f)] public float closeEnoughFinalDistance = 0.2f;

    // Start is called before the first frame update
    public virtual void Start()
    {
        sc = ScreenCapturer.Instance;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        // I Don't know
    }

    public virtual void InitializePerson(CrowdInfo i) {
        info = i;
        SetNavMesh();
        gameObject.layer = 8;
        cm = CrowdManager.Instance;
        cm.RegisterPerson(this);
    }

    public virtual NavMeshAgent SetNavMesh() {
        NavMeshAgent n = Utility.GetOrAddComponent<NavMeshAgent>(gameObject);
        n.speed = info.speed;
        n.radius = radius;
        n.height = height;
        return n;
    }

    public virtual void CommitSuicide() {
        cm.DeregisterPerson(this);
        Destroy(gameObject);
    }
}
