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
    [Range(0.1f, 10f)] public float closeEnoughDistance = 1.5f;
    [Range(1f, 10f)] public float closeEnoughFinalDistance = 0.2f;


    // An ID of the person to keep track of its movements
    public int id;

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

    // Register this person with the manager
    public virtual void InitializePerson(CrowdInfo i) {
        info = i;
        SetNavMesh();
        gameObject.layer = 8;
        cm = CrowdManager.Instance;
        cm.RegisterPerson(this);
    }

    // Make the nav mesh agent compoent
    public virtual NavMeshAgent SetNavMesh() {
        NavMeshAgent n = Utility.GetOrAddComponent<NavMeshAgent>(gameObject);
        n.speed = info.speed;
        n.radius = radius;
        n.height = height;
        return n;
    }
    

    // Makes the order to kill itself if it has reached the end of its life
    public virtual void CommitSuicide() {
        cm.DeregisterPerson(this);
        Destroy(gameObject);
    }

    // Get all the vertices in the children of a gameobject
    // The vector 3s are local positions
    public Vector3[] GetVertices() {
        // Get the renderer and make a new mesh
        SkinnedMeshRenderer smr = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();;
        GameObject go = smr.gameObject;
        Mesh mesh = new Mesh();

        // Bake the static mesh so we can get the vertices out of it
        smr.BakeMesh(mesh, true);

        // Make the list. The list is updated by reference
        List<Vector3> lis = new List<Vector3>();
        mesh.GetVertices(lis);

        // The vector 3s are local positions
        // We know the objects are at most two layers deep so we can hard code lmao
        for (int i = 0; i < lis.Count; i++) {
            lis[i] = go.transform.TransformPoint(lis[i]);
        }

        return lis.ToArray();
    }
}
