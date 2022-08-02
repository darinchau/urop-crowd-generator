using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// A people spawner whose purpose in life is to let us set a bunch of parameters and spawn a bunch of people
public class StandingCrowdPath : Path
{   
    [Tooltip("The mean and variance of the clump size in the standing crowd. The size will be generated according to Poisson distribution."), Range(1, 6)] public int l = 3;

    public override void RecalculatePoint() {
        int n = waypoints.Count;
        if (n < 3) return;

        List<Vector3> vertices = new List<Vector3>();

        // These points would be triangulations of the path
        for (int i = 0; i < waypoints.Count; i++) {
            vertices.Add(waypoints[i].transform.position);
        }

        points = TriangulateRecursive(vertices);
    }

    // Triangulate a polygon recursively with the induction approach
    // Returns a list of Vector3[], each with length 3 containing a triangle
    private List<Vector3[]> TriangulateRecursive(List<Vector3> vertices) {
        // Debug.Log("Calling function recursive");
        List<Vector3[]> triangulation = new List<Vector3[]>();
        int n = vertices.Count;
        if (n < 3) {
            throw new System.Exception("Too few vertices! There are only " + n.ToString() + " vertices");
        }
        else if (n == 3) {
            Vector3[] t = new Vector3[3];
            t[0] = vertices[0];
            t[1] = vertices[1];
            t[2] = vertices[2];
            triangulation.Add(t);
        }
        else {
            // Find a convex angle x, with neighbouring vertices y, z
            // Draw line segment yz. If yz intersects the polygon 
            //      Then yz is not a diagonal. That means at least one vertices inside triangle xyz
            //      Get the vertex that is farthest from line yz inside xyz, and call it v
            //      Then xv is a diagonal
            // Split the polygon along the diagonal
            // Triangulate these two smaller polygons

            Vector3 x = vertices[0];
            Vector3 y = vertices[1];
            Vector3 z = vertices[2];
            int xIdx = 1; 
            int vIdx = 0;

            // Vector3 wx = vertices[n-1];
            // Vector3 wy = vertices[0];
            // Vector3 wz = vertices[1];

            // float wangle = Vector3.SignedAngle(wy - wx, wz - wx, Vector3.up);

            // // First check if it is in strict clockwise or counterclockwise orientation, if not then return
            // for (int i = 1; i < n - 1; i++) {

            //     x = vertices[i];
            //     y = vertices[i-1]; y.y = x.y;
            //     z = vertices[i+1]; z.y = x.y;

            //     float xangle = Vector3.SignedAngle(y - x, z - x, Vector3.up);

            //     // if (wangle < 0 || xangle < 0) throw new System.Exception("Not in strict clockwise orientation!");
            // }

            // Maximum number of concave vertices is n-3 by some simple calculation. So we can choose to not consider the first and last and still be guaranteed to find a convex vertex.
            for (; xIdx < n - 1; xIdx++) {
                //Clockwise orientation please
                x = vertices[xIdx];
                y = vertices[xIdx-1]; y.y = x.y;
                z = vertices[xIdx+1]; z.y = x.y;

                float angle = Vector3.SignedAngle(y - x, z - x, Vector3.up);

                if (angle < 0) break;
            }
            
            Vector3 furthestPoint = new Vector3();
            float furthestDistance = -1f;

            for (; vIdx < n; vIdx++) {
                Vector3 v = vertices[vIdx];
                if (v == x || v == y || v == z) continue;
                if (PointInTriangle(v, x, y, z)) {
                    float a = Utility.HDist(y, z);
                    float area = Area(v, y, z);
                    float distance = 2 * area / a;
                    if (distance > furthestDistance) {
                        furthestDistance = distance;
                        furthestPoint = v;
                    }
                }
            }

            // Now split the polygon into two smaller polygons and apply recursion
            if (furthestDistance < 0) {
                // This means yz is actually a diagonal
                vIdx = xIdx - 1;
                xIdx += 1;
            }

            // TODO use a better indexing method
            int adds = 0;
            List<Vector3> polygon1 = new List<Vector3>();
            for (int i = xIdx;; i++) {
                adds += 1;
                if (i == n) i = 0;
                polygon1.Add(vertices[i]);

                if (i == vIdx) break;
                if (adds >= n + 2) throw new System.Exception();
            }

            List<Vector3> polygon2 = new List<Vector3>();
            for (int i = vIdx;; i++) {
                adds += 1;
                if (i == n) i = 0;
                polygon2.Add(vertices[i]);

                if (i == xIdx) break;
                if (adds >= n + 2) throw new System.Exception();
            }

            List<Vector3[]> tri1 = TriangulateRecursive(polygon1);
            List<Vector3[]> tri2 = TriangulateRecursive(polygon2);

            for (int i = 0; i < tri1.Count; i++) {
                // Debug.Log("Tri 1 count = " + tri1.Count.ToString());
                triangulation.Add(tri1[i]);
            }

            for (int i = 0; i < tri2.Count; i++) {
                // Debug.Log("Tri 2 count = " + tri2.Count.ToString());
                triangulation.Add(tri2[i]);
            }
        }

        return triangulation;
    }

    float sign (Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }

    bool PointInTriangle (Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }


    // Draw the curve gizmos
    public override void DrawCurveGizmos()
    {
        int n = waypoints.Count;
        if (n < 3) return;

        RecalculatePoint();

        // Now we actually draw the Gizmos
        Gizmos.color = Color.blue;
        // Debug.Log("There should be number of triangles = " + points.Count.ToString());
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawLine(points[i][0], points[i][1]);
            Gizmos.DrawLine(points[i][1], points[i][2]);
            Gizmos.DrawLine(points[i][2], points[i][0]);
        }
    }
    
    // This one actually spawns one clump of people
    public override void Spawn(int pathIdx, bool runtime)
    {
        if (runtime) return;

        int n = waypoints.Count;

        RecalculatePoint();

        // Calculate the area of each triangle, then pick a random one according to its area
        float[] _areas = new float[n - 2];
        float totalArea = 0f;

        for (int i = 0; i < n - 2; i++) {
            _areas[i] = Area(points[i][0], points[i][1], points[i][2]);
            totalArea += _areas[i];
        }

        // Generate the clump size according to Poisson distribution
        int clump = 0; 
        int factorial = 1;
        float r0 = UnityEngine.Random.Range(0f, 1f);
        float sum = Mathf.Exp(-l);
        while (r0 > sum) {
            clump += 1;
            factorial *= clump;
            sum += Mathf.Exp(-l) * Mathf.Pow(l, clump) / factorial;
        }

        // Generate stuff until we get a hit on the navmesh
        int trials = 15;
        Vector3[] spawnPoints = new Vector3[clump];
        Vector3 basePoint = new Vector3();
        while (trials >= 0){
            trials--;

            // Get one random triangle
            float r = UnityEngine.Random.Range(0f, totalArea);
            int i0 = -1;
            while (r >= 0) {
                i0 += 1;
                r -= _areas[i0];
            }
            
            Vector3 A = points[i0][0];
            Vector3 B = points[i0][1];
            Vector3 C = points[i0][2];

            float r1 = UnityEngine.Random.Range(0f, 1f);
            float r2 = UnityEngine.Random.Range(0f, 1f);


            // Generate the base spawn points
            basePoint = (1 - Mathf.Sqrt(r1)) * A + (Mathf.Sqrt(r1) * (1 - r2)) * B + (Mathf.Sqrt(r1) * r2) * C;
            basePoint.y = 0.5f;

            for (int i = 0; i < clump; i++) {
                Vector3 rand = new Vector3(UnityEngine.Random.Range(-randPos.x, randPos.x), 0, UnityEngine.Random.Range(-randPos.y, randPos.y));
                spawnPoints[i] = basePoint + rand;
                if (!NavMesh.Raycast(spawnPoints[i], spawnPoints[i] - Vector3.up, out NavMeshHit hit, 0)) {
                    continue;
                }
            }

            break;
        }

        if (trials == 0) {
            throw new System.Exception("Cannot generate a set of valid spawn points! Perhaps the Nav mesh is too narrow?");
        }

        string[] animations = {"talk1", "talk2", "listen", "idle1", "idle2"};

        // We got the spawn positions that we need. Proceed to spawning
        Transform personParent = transform.Find("people");

        for (int i = 0; i < clump; i++) {
            // Generate random appearance
            int appearanceIdx = UnityEngine.Random.Range(0, people.Length);

            // Now create the person
            GameObject person = Instantiate(people[appearanceIdx], spawnPoints[i], Quaternion.LookRotation(basePoint - spawnPoints[i])) as GameObject;
            person.transform.parent = personParent;
            StandingCrowd crowd = person.AddComponent<StandingCrowd>();

            string animName = animations[UnityEngine.Random.Range(0,animations.Length)];

            // Make a new crowdinfo
            CrowdInfo info = new CrowdInfo();
            info.pathIdx = pathIdx;
            info.animationName = animName;
            info.path = this;
            info.speed = 5f;
            info.spawnPos = spawnPoints[i];

            crowd.InitializePerson(info);
        }
    }

    public override void Populate()
    {
        // This time density means each 10 unit area has such number of clumps
        int n = waypoints.Count;
        RecalculatePoint();

        float[] _areas = new float[n - 2];
        float totalArea = 0f;

        for (int i = 0; i < n - 2; i++) {
            _areas[i] = Area(points[i][0], points[i][1], points[i][2]);
            totalArea += _areas[i];
        }

        int clump = (int)(density * totalArea / 5f);

        for (int i = 0; i < clump; i++) {
            Spawn(i, false);
        }
    }

    // Start is called once at start. If you want to implement a custom start method please call this base method
    // public override void Start() {
    //     base.Start();
    // }

    private float Area(Vector3 x, Vector3 y, Vector3 z) {
        float a = Utility.HDist(y, z);
        float b = Utility.HDist(y, x);
        float c = Utility.HDist(z, x);
        float s = (a + b + c)/2;
        float distance = Mathf.Sqrt(s*(s-a)*(s-b)*(s-c));
        return distance;
    }
}
