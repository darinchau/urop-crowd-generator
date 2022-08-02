using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class ScreenCapturer : Singleton<ScreenCapturer>
{
    // Data batch number. Make the file before you change this batch idx since I have no idea how to mkdir in C#
    public static int batchIdx = 1;

    // Root directory of the saved data
    public string savePathRoot = "Data";
    
    // Countdown before the capturing starts
    public float captureAfter = 10f;

    // How many frames to capture in total
    public int howManyFrames = 300;

    [Tooltip("Number of raycasts to perform to check obscurity of the human being"), Range(2, 5)] public int rayCasts = 4;

    // Integer to keep track of frame number
    int frameNumber = 0;

    // Crowd manager
    CrowdManager cm;

    public bool IsInScreenArea(Vector3 worldPos) {
        Vector3 pointPos = Camera.main.WorldToScreenPoint(worldPos);
        return (pointPos.x >= 0) && (pointPos.y >= 0) && (pointPos.x < Screen.width) && (pointPos.y < Screen.height);
    }

    // Gets the screenPos by reference. It will return something (negative values, etc) even if the object is not on screen
    // World Position ought to be the transform.position of whatever object whose world position we are trying to get.
    public bool IsVisible(Vector3 worldPos, out Vector2Int screenPos) {
        
        Camera c = Camera.main;
        Vector3 pointPos = c.WorldToScreenPoint(worldPos);

        screenPos = new Vector2Int((int)pointPos.x, (int)pointPos.y);

        // First perform a preliminary check. If the object isnt even on screen then no need to perform raycast
        bool isInScreenArea = (pointPos.x >= 0) && (pointPos.y >= 0) && (pointPos.x < Screen.width) && (pointPos.y < Screen.height);
        if (!isInScreenArea) return false;

        // Now perform raycasting
        float height = Crowd.height;

        // Ignore humans only. That's why we use the bitwise not operator on the bit mask
        int mask = ~(LayerMask.GetMask("People") + LayerMask.GetMask("Ignore Raycasts"));
        
        // Perform raycast from human to cam. If the human is obscured then return false
        Vector3 camPos = c.transform.position;

        // I am afraid the raycast will hit the floor or something so let's subtract epsilon from the raycast magnitude
        float epsilon = Mathf.Pow(10, -5);

        // Perform raycasts
        for (int i = 0; i < rayCasts; i++) {
            // Perform raycasts at various height
            Vector3 basePos = worldPos + Vector3.up * i / (float)(rayCasts - 1);
            float mag = (basePos - camPos).magnitude - epsilon;
            if (Physics.Raycast(basePos, camPos - basePos, out RaycastHit hit, mag, mask)) return false;
        }
        
        return true;
    }

    // Start is called once before the game starts
    private void Start() 
    {
        cm = CrowdManager.Instance;
    }

    // We use late update instead so that we capture the screen after all the people have moved
    private void LateUpdate()
    {
        if (captureAfter > 0) {
            captureAfter -= Time.deltaTime;
        }
        else if (frameNumber < howManyFrames) {
            frameNumber++;
            MakeScreenshot();
            string textData = GetFrameData();
            string fn = "data" + ".txt";
            string path = "./Assets/" + savePathRoot + "/" + "Batch " + batchIdx.ToString();

            using(StreamWriter sw = File.AppendText(path + "/" + fn))
            {
                sw.WriteLine(textData);
            }
        }
    }

    private void MakeScreenshot() 
    {
        string picName = "Frame " + frameNumber.ToString() + ".png";
        string path = "./Assets/" + savePathRoot + "/" + "Batch " + batchIdx.ToString();
        ScreenCapture.CaptureScreenshot(path + "/" + picName);
    }

    private string GetFrameData() 
    {
        string data = "Frame " + frameNumber.ToString() + ":\n";
        int count = 0;
        // The premise is we check visibility of everyone spawned, and then export the data into a nice text array. I have no idea how to export stuff into fancier formats like numpy array or h5 lmao
        for (int i = 0; i < cm.population.Count; i++) {
            Crowd c = cm.population[i];
            if (IsVisible(c.transform.position, out Vector2Int pos)) {
                data += "\tx:" + pos.x.ToString() + "  y:" + pos.y.ToString() + "\n";
                count += 1;
            }
        }
        Debug.Log(count);
        return data;
    }
}
