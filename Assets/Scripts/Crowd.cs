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

    // Start is called before the first frame update
    public virtual void Start()
    {
        cm = CrowdManager.Instance;
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
    }

    public virtual NavMeshAgent SetNavMesh() {
        NavMeshAgent n = Utility.GetOrAddComponent<NavMeshAgent>(gameObject);
        n.speed = info.speed;
        n.radius = radius;
        n.height = height;
        return n;
    }
}
