using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Crowd : MonoBehaviour
{
    // Holds the movement info of the crowd
    [SerializeField] protected CrowdInfo info;
    public CrowdManager cm;
    public ScreenCapturer sc;

    // Width and height of human
    public float radius = 0.3f;
    public float height = 1.85f;

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
        // Set crowd info
        info = i;
        //Set height
        height = GetHeight();
        // Set nav mesh
        SetNavMesh();
        // Set raycast layer mask
        gameObject.layer = 8;
        // Initialize yourself with the manager
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
        SkinnedMeshRenderer smr = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        Destroy(smr);
        Destroy(gameObject);
    }

    // Get all the vertices in the children of a gameobject
    // The vector 3s are local positions
    // Actually let's have this list sorted from high to low
    // It will be ever so useful
    public Vector3[] GetVertices() {
        // Get the renderer and make a new mesh
        SkinnedMeshRenderer smr = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        GameObject go = smr.gameObject;
        Mesh mesh = new Mesh();

        // Bake the static mesh so we can get the vertices out of it
        smr.BakeMesh(mesh, true);

        // Make the list. The list is updated by reference
        List<Vector3> lis = new List<Vector3>();
        mesh.GetVertices(lis);

        List<Vector3> SortedList = lis.OrderBy(v => v.y).ToList();

        // The vector 3s are local positions
        // We know the objects are at most two layers deep so we can hard code lmao
        for (int i = 0; i < SortedList.Count; i++) {
            SortedList[i] = go.transform.TransformPoint(SortedList[i]);
        }

        return SortedList.ToArray();
    }

    // Get the height of the person
    public float GetHeight() {
        // Get all the vertices of the mesh
        Vector3 [] vertices = GetVertices();

        // // Keep track of the highest and lowest point of the human
        // Vector3 highest = new Vector3();
        // highest.y = -999;
        // Vector3 lowest = new Vector3();
        // lowest.y = 999;

        // for (int i = 0; i < vertices.Length; i++) {
        //     Vector3 v = vertices[i];
        //     if (v.y > highest.y) {
        //         highest = v;
        //     } 
            
        //     if (v.y < lowest.y) {
        //         lowest = v;
        //     }
        // }

        Vector3 lowest = vertices[0];
        Vector3 highest = vertices[vertices.Length - 1];

        // Now we have the highest and lowest vector. Now calculate its height
        return highest.y - lowest.y;
    }

    // Maybe also get the radius in the future?
}
