using System;
using UnityEngine;
using System.Collections.Generic;

// A people spawner whose purpose in life is to let us set a bunch of parameters and spawn a bunch of people
public class CrowdPath : Path
{   
    [Tooltip("Proportion of people running"), Range(0,1)] public float runningProportion = 0.01f;
    [Tooltip("Proportion of people walking/running backwards"), Range(0,1)] public float backProportion = 0.08f;

    [Tooltip("Speed of walk will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 walkSpeed = new Vector2(1f, 0.2f);
    [Tooltip("Speed of runing will be generated UnityEngine.Randomly with normal distribution - X: mean, Y: variance")] [SerializeField] private Vector2 runSpeed = new Vector2(4f, 0.6f);
    [Tooltip("Maximum deviation from normal speed")] [SerializeField] private float maxSigma = 3f;

    [Tooltip("Add a bit of UnityEngine.Randomness to the finishing position")] [SerializeField] private Vector2 randFinish = new Vector2(0.1f, 0.1f);

    // Draw the curve gizmos
    public override void DrawCurveGizmos()
    {
        if (numGizmosLines < 1) numGizmosLines = 1;
        if (lineSpacing < 0.6f) lineSpacing = 0.6f;

        // Number of waypoints. First add the first waypoint to th list if we should loop the path
        if (loopPath) waypoints.Add(waypoints[0]);
        int n = waypoints.Count;

        if (n < 2) return;

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


        // Now we actually draw the Gizmos
        Gizmos.color = Color.green;
        for (int w = 0; w<numGizmosLines; w++)
        {
            for (int i = 1; i < n; i++) Gizmos.DrawLine(points[w, i + 1], points[w, i]);
        }
    }
    
    // Spawn one person on the specified path. 
    public override void SpawnPerson(int pathIdx, bool startAtBeginning)
    {
        int n = waypoints.Count;

        // Randomly generate the profile of the human
        bool run = UnityEngine.Random.value <= runningProportion;
        bool back = UnityEngine.Random.value <= backProportion;

        float speed = run ? GenerateNormal(runSpeed.x, runSpeed.y, maxSigma) : GenerateNormal(walkSpeed.x, walkSpeed.y, maxSigma);
        
        int appearanceIdx = UnityEngine.Random.Range(0, people.Length);

        Vector2 randFinishPos = new Vector2(UnityEngine.Random.Range(-randFinish.x, randFinish.x), UnityEngine.Random.Range(-randFinish.y, randFinish.y));
        
        int nextWpIndex = new int();

        // Create the person somewhere between the given wpindex and the previous one. If the given is 1 or given is n then since 0-1, and n-n+1 are duplicates anyway so there is nothing in between
        if (startAtBeginning) {
            nextWpIndex = back ? 1 : n;
        } else {
            nextWpIndex = UnityEngine.Random.Range(1, n);
        }
        int prevWpIndex = back ? nextWpIndex + 1 : nextWpIndex - 1;
        float randAt = UnityEngine.Random.Range(0, 1);
        Vector3 spawnPos = waypoints[prevWpIndex].transform.position * randAt + waypoints[nextWpIndex].transform.position * (1 - randAt);

        // Now create the person

        GameObject person = Instantiate(people[appearanceIdx], spawnPos, Quaternion.identity) as GameObject;

        Crowd crowd = person.AddComponent<Crowd>();

        crowd.InitializePerson(pathIdx, nextWpIndex, run, back, speed, this, randFinishPos);
    }

    public override void Populate()
    {
        Debug.Log("haha");
    }



    private float GenerateNormal(float mean = 0, float variance = 1, float maxSigma = 3) {
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
}