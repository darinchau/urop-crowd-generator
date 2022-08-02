using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

// A people spawner whose purpose in life is to let us set a bunch of parameters and spawn a bunch of people
public class WalkingCrowdPath : Path
{   
    [Tooltip("Proportion of people running"), Range(0,1)] public float runningProportion = 0.01f;
    [Tooltip("Proportion of people walking/running backwards"), Range(0,1)] public float backProportion = 0.08f;

    [Tooltip("Speed of walk will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 walkSpeed = new Vector2(1f, 0.2f);
    [Tooltip("Speed of runing will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 runSpeed = new Vector2(4f, 0.6f);
    [Tooltip("Maximum deviation from normal speed")] [SerializeField] private float maxSigma = 3f;

    public override void RecalculatePoint() {
        if (pathWidth < 1) pathWidth = 1;
        if (spacing < 0.6f) spacing = 0.6f;

        // Number of waypoints. First add the first waypoint to th list if we should loop the path
        if (loopPath) waypoints.Add(waypoints[0]);
        int n = waypoints.Count;

        // If there is not enough waypoints
        if (n < 2) return;

        // Now assign length to the points 2D array
        // First index is vertically, second index is horizontally
        points = new List<Vector3[]>();

        // First fix the list
        for (int i = 0; i < pathWidth; i++) {
            points.Add(new Vector3[n + 2]);
        }

        // Create a vector from one waypoint to the next
        for (int i = 0; i < n; i++)
        {
            Vector3 vectorStart;
            Vector3 vectorEnd;

            // First one
            if (i == 0)
            {
                vectorStart = Vector3.zero;
                vectorEnd = waypoints[0].transform.position - waypoints[1].transform.position;
            }
            // Last one
            else if (i == n - 1)
            {
                vectorStart = waypoints[n - 2].transform.position - waypoints[n - 1].transform.position;
                vectorEnd = Vector3.zero;
            }
            else
            {
                vectorStart = waypoints[i - 1].transform.position - waypoints[i].transform.position;
                vectorEnd = waypoints[i].transform.position - waypoints[i + 1].transform.position;
            }

            // The shift vector from the actual waypoint coordinates
            Vector3 shear = Vector3.Normalize((Quaternion.Euler(0, 90, 0) * (vectorStart + vectorEnd)));

            // Assign the first vertical waypoint, if there is more than one waypoint then assign the rest
            // Use the transform scale x to determine an additional shear factor
            float shearFactor = waypoints[i].transform.localScale.x;

            // Handle differently if there is even number of paths vs if there is odd number paths
            points[0][i + 1] = pathWidth % 2 == 1 ? waypoints[i].transform.position : waypoints[i].transform.position + shear * (spacing * shearFactor / 2);

            // Update the first one too
            if (pathWidth > 1) 
                points[1][i + 1] = points[0][i + 1] - shear * spacing * shearFactor;

            for (int w = 1; w < pathWidth; w++)
            {
                points[w][i + 1] = points[0][i + 1] + shear * spacing * shearFactor * (float) (Math.Pow(-1, w)) * ((w + 1) / 2);
            }
        }

        // Duplicate the first and last waypoints for every path
        for (int w = 0; w < pathWidth; w++)
        {
            points[w][0] = points[w][1];
            points[w][n + 1] = points[w][n];
        }
    }


    // Draw the curve gizmos
    public override void DrawCurveGizmos()
    {
        int n = waypoints.Count;
        if (n < 2) return;

        RecalculatePoint();

        // Now we actually draw the Gizmos
        Gizmos.color = Color.green;
        for (int w = 0; w < pathWidth; w++)
        {
            for (int i = 1; i < n; i++) Gizmos.DrawLine(points[w][i + 1], points[w][i]);
        }
    }
    
    // Spawn one person on the specified path.
    // Path Idx is index of the path the person is on
    // runtime indicates whether th function is called at runtime or in the beginning
    public override void Spawn(int pathIdx, bool runtime)
    {
        // This recalculates the waypoints
        RecalculatePoint();
        int n = waypoints.Count;

        // Randomly generate the profile of the human
        bool run = UnityEngine.Random.value <= runningProportion;
        bool back;

        // If dont kill at end then don't spawn at end either
        if (runtime && !killAtEnd && killAtStart) {
            back = false;
        }
        else if (runtime && !killAtStart && killAtEnd) {
            back = true;
        }
        else if (runtime && !killAtStart && !killAtEnd) { 
            Debug.Log("Making spawn call on a pacifist path!");
            back = UnityEngine.Random.value <= backProportion;
        } 
        else {
            back = UnityEngine.Random.value <= backProportion;
        }

        float speed = run ? GenerateNormal(runSpeed.x, runSpeed.y, maxSigma) : GenerateNormal(walkSpeed.x, walkSpeed.y, maxSigma);
        
        int appearanceIdx = UnityEngine.Random.Range(0, people.Length);

        Vector2 randFinishPos = new Vector2(UnityEngine.Random.Range(-randPos.x, randPos.x), UnityEngine.Random.Range(-randPos.y, randPos.y));

        // Make a vector 3 array to hold the pathIdx specified information of waypoints
        Vector3[] specPoints = points[pathIdx];
        
        // Create the person somewhere between the given wpindex and the previous one. If the given is 1 or given is n then since 0-1, and n-n+1 are duplicates anyway so there is nothing in betweens
        int prevWpIndex, nextWpIndex;
        
        // Now the reason why we double the end points is clear - if we want someone to start at the beginning we spawn it between point 0 and 1, so by squeeze theorem the point is fixed.
        if (back) 
        {
            prevWpIndex = runtime ? n + 1 : GenerateEvenNextWpIdx(specPoints);
            nextWpIndex = prevWpIndex - 1;
        }
        else 
        {
            nextWpIndex = runtime ? 1 : GenerateEvenNextWpIdx(specPoints);
            prevWpIndex = nextWpIndex - 1;
        }

        float randAt = UnityEngine.Random.Range(0f, 1f);
        Vector3 spawnPos = specPoints[prevWpIndex] * randAt + specPoints[nextWpIndex] * (1 - randAt);

        // Now create the person
        Transform personParent = transform.Find("people");

        GameObject person = Instantiate(people[appearanceIdx], spawnPos, Quaternion.identity) as GameObject;
        person.transform.parent = personParent;
        WalkingCrowd crowd = person.AddComponent<WalkingCrowd>();

        string animName = run ? "run" : "walk";

        crowd.InitializePerson(pathIdx, nextWpIndex, run, back, speed, animName, this, randFinishPos, specPoints);
    }

    // TODO use inverse transform sampling to make people more evenly distributed
    public override void Populate()
    {
        RecalculatePoint();
        float totalDist = GetDist(points[0], out float[] dists);

        int numPerson = (int)(density * totalDist * pathWidth / 3f);

        for (int i = 0; i < numPerson; i++) {
            int pathIdx = UnityEngine.Random.Range(0, pathWidth);
            Spawn(pathIdx, false);
        }
    }

    public static float GenerateNormal(float mean = 0, float variance = 1, float maxSigma = 3) {
        // We use a lazy approach based on the central limit theorem
        float sum = 0;
        for(int i = 0; i < 12; i++) {
            sum += UnityEngine.Random.value;
        }

        sum -= 6;

        if (sum <= -maxSigma || sum >= maxSigma) {
            return GenerateNormal(mean, variance, maxSigma);
        }

        return variance * sum + mean;
    }

    public int GenerateEvenNextWpIdx(Vector3[] specPoints) {
        float totalDist = GetDist(specPoints, out float[] dists);
        float r = UnityEngine.Random.Range(0f, 1f) * totalDist;

        float cumDist = 0;
        for(int i = 1; i < specPoints.Length; i++) {
            cumDist += dists[i-1];
            if (r <= cumDist) {return i; }
        }
        
        return 1;
    }

    public float GetDist(Vector3[] specPoints, out float[] dists) {
        dists = new float[specPoints.Length - 1];
        float totalDist = 0;

        for(int i = 1; i < specPoints.Length; i++) {
            float hDist =  Utility.HDist(specPoints[i], specPoints[i-1]);
            dists[i - 1] = hDist;
            totalDist += hDist;
        }

        return totalDist;
    }
}