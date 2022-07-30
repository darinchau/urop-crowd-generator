using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// A people spawner whose purpose in life is to let us set a bunch of parameters and spawn a bunch of people
public class CrowdPath : Path
{   
    [Tooltip("Proportion of people running"), Range(0,1)] public float runningProportion = 0.01f;
    [Tooltip("Proportion of people walking/running backwards"), Range(0,1)] public float backProportion = 0.08f;

    [Tooltip("Speed of walk will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 walkSpeed = new Vector2(1f, 0.2f);
    [Tooltip("Speed of runing will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 runSpeed = new Vector2(4f, 0.6f);
    [Tooltip("Maximum deviation from normal speed")] [SerializeField] private float maxSigma = 3f;

    [Tooltip("Add a bit of UnityEngine.Randomness to the finishing position")] [SerializeField] private Vector2 randFinish = new Vector2(0.1f, 0.1f);

    private int RecalculatePoint() {
        if (numGizmosLines < 1) numGizmosLines = 1;
        if (lineSpacing < 0.6f) lineSpacing = 0.6f;

        // Number of waypoints. First add the first waypoint to th list if we should loop the path
        if (loopPath) waypoints.Add(waypoints[0]);
        int n = waypoints.Count;

        if (n < 2) return -1;

        // Now assign length to the points 2D array
        // First index is vertically, second index is horizontally
        points = new Vector3[numGizmosLines, n + 2];

        // Create a vector from one waypoint to the next to draw the gizmos
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
            points[0, i + 1] = numGizmosLines % 2 == 1 ? waypoints[i].transform.position : waypoints[i].transform.position + shear * lineSpacing / 2;
            if (numGizmosLines > 1) points[1, i + 1] = points[0, i + 1] - shear * lineSpacing;

            for (int w = 1; w < numGizmosLines; w++)
            {
                points[w, i + 1] = points[0, i + 1] + shear * lineSpacing * (float) (Math.Pow(-1, w)) * ((w + 1) / 2);
            }
        }

        // Duplicate the first and last waypoints for every path
        for (int w = 0; w < numGizmosLines; w++)
        {
            points[w, 0] = points[w, 1];
            points[w, n + 1] = points[w, n];
        }

        return n;
    }


    // Draw the curve gizmos
    public override void DrawCurveGizmos()
    {
        int n = RecalculatePoint();
        if (n < 0) return;

        // Now we actually draw the Gizmos
        Gizmos.color = Color.green;
        for (int w = 0; w < numGizmosLines; w++)
        {
            for (int i = 1; i < n; i++) Gizmos.DrawLine(points[w, i + 1], points[w, i]);
        }
    }
    
    // Spawn one person on the specified path. 
    public override void SpawnPerson(int pathIdx, bool startAtBeginning)
    {
        // This recalculates the waypoints
        int n = RecalculatePoint();

        // Randomly generate the profile of the human
        bool run = UnityEngine.Random.value <= runningProportion;
        bool back = UnityEngine.Random.value <= backProportion;

        float speed = run ? GenerateNormal(runSpeed.x, runSpeed.y, maxSigma) : GenerateNormal(walkSpeed.x, walkSpeed.y, maxSigma);
        
        int appearanceIdx = UnityEngine.Random.Range(0, people.Length);

        Vector2 randFinishPos = new Vector2(UnityEngine.Random.Range(-randFinish.x, randFinish.x), UnityEngine.Random.Range(-randFinish.y, randFinish.y));
        
        // Create the person somewhere between the given wpindex and the previous one. If the given is 1 or given is n then since 0-1, and n-n+1 are duplicates anyway so there is nothing in betweens
        int prevWpIndex, nextWpIndex;

        if (back) 
        {
            prevWpIndex = startAtBeginning ? n + 1 : GenerateEvenNextWpIdx(pathIdx);
            nextWpIndex = prevWpIndex - 1;
        }
        else 
        {
            nextWpIndex = startAtBeginning ? 1 : GenerateEvenNextWpIdx(pathIdx);
            prevWpIndex = nextWpIndex - 1;
        }

        float randAt = UnityEngine.Random.Range(0f, 1f);
        Vector3 spawnPos = points[pathIdx, prevWpIndex] * randAt + points[pathIdx, nextWpIndex] * (1 - randAt);

        // Now create the person
        Transform personParent = transform.Find("people");

        GameObject person = Instantiate(people[appearanceIdx], spawnPos, Quaternion.identity) as GameObject;
        person.transform.parent = personParent;
        Crowd crowd = person.AddComponent<Crowd>();

        crowd.InitializePerson(pathIdx, nextWpIndex, run, back, speed, this, randFinishPos);
    }

    // TODO use inverse transform sampling to make people more evenly distributed
    public override void Populate()
    {
        int numPerson = (int)(density * waypoints.Count * numGizmosLines);

        for (int i = 0; i < numPerson; i++) {
            int pathIdx = UnityEngine.Random.Range(0, numGizmosLines);
            SpawnPerson(pathIdx, false);
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

    public static int GenerateEvenNextWpIdx(int pathIdx) {
        float[] dists = new float[waypoints.Count - 1];
        float totalDist = 0;

        for(int i = 1; i < waypoints.Count; i++) {
            float hDist =  HDist(points[pathIdx, i], points[pathIdx, i-1]);
            dists[i - 1] = hDist;
            totalDist += hDist;
        }

        float r = UnityEngine.Random.Range(0f, 1f) * totalDist;

        float cumDist = 0;
        for(int i = 1; i < waypoints.Count; i++) {
            cumDist += dists[i-1];
            if (r <= cumDist) {return i; }
        }
        
        return 1;
    }

}