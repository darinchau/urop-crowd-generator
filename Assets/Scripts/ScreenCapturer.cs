using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;

public class ScreenCapturer : Singleton<ScreenCapturer>
{
    // Root directory of the saved data
    public static string savePathRoot = "Data";
    
    // Countdown before the capturing starts
    public float captureAfter = 10f;

    // Capture every x seconds
    // -1 means capture every frame
    public float captureEvery = -1f;

    // How many frames to capture in total
    public int howManyFrames = 300;

    // Target frame rate
    public int targetFrameRate = 15;

    [Tooltip("Number of raycasts to perform to check obscurity of the human being"), Range(2, 5)] public int rayCasts = 4;

    // Integer to keep track of frame number
    int frameNumber = 0;

    // We will pause to do calculations on the screenshot frame so this is a flip flop bool to achieve that
    bool paused = false;

    // Crowd manager
    CrowdManager cm;

    // Folder path
    string path;

    // Returns true if the world pos is within the bounds of the screens
    public bool IsInScreenArea(Vector3 worldPos, out Vector2Int screenPos) {
        Vector3 pointPos = Camera.main.WorldToScreenPoint(worldPos);
        screenPos =  new Vector2Int((int)pointPos.x, (int)(Screen.height - pointPos.y));
        return (pointPos.x >= 0) && (pointPos.y >= 0) && (pointPos.x < Screen.width) && (pointPos.y < Screen.height);
    }

    // Gets the screenPos by reference. It will return something (negative values, etc) even if the object is not on screen
    // World Position ought to be the transform.position of whatever object whose world position we are trying to get.
    public bool IsVisible(Vector3 worldPos, out Vector2Int screenPos) {
        // Check if it is on screen
        if (!IsInScreenArea(worldPos, out screenPos)) return false;

        // Now perform raycasting
        float height = Crowd.height;

        // Ignore humans only. That's why we use the bitwise not operator on the bit mask
        int mask = ~(LayerMask.GetMask("People") + LayerMask.GetMask("Ignore Raycasts"));
        
        // Perform raycast from human to cam. If the human is obscured then return false
        Vector3 camPos = Camera.main.transform.position;

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

    // State whether is point visible
    public bool IsPointVisible(Vector3 worldPos, out Vector2Int screenPos) {
        // Check if it is on screen
        if (!IsInScreenArea(worldPos, out screenPos)) return false;

        // Ignore humans only. That's why we use the bitwise not operator on the bit mask
        int mask = ~(LayerMask.GetMask("People") + LayerMask.GetMask("Ignore Raycasts"));
        
        // Perform raycast from human to cam. If the human is obscured then return false
        Vector3 camPos = Camera.main.transform.position;

        // I am afraid the raycast will hit the floor or something so let's subtract epsilon from the raycast magnitude
        float mag = (worldPos - camPos).magnitude - Mathf.Pow(10, -5);
        if (Physics.Raycast(worldPos, camPos - worldPos, out RaycastHit hit, mag, mask)) return false;
        return true;
    }

    // Try to get the bounding box of the given crowd people c
    // The head position will serve as the base position to start calculation
    public void GetBoundingBox(Crowd c, Vector2Int headPos, out Vector2Int topleft, out Vector2Int bottomright){
        // First get all the vertices of the person
        Vector3[] vertices = c.GetVertices();

        // We use this kind of shady assignment lol
        topleft = headPos;
        bottomright = headPos;

        // Loop through every position        
        for (int i = 0; i < vertices.Length; i++) {
            Vector3 v = vertices[i];
            if(!IsInScreenArea(v, out Vector2Int pos)) continue;

            // Try to update the top left and bottom right
            if (pos.x < topleft.x) topleft.x = pos.x;
            else if (pos.x > bottomright.x) bottomright.x = pos.x;

            if (pos.y < topleft.y) topleft.y = pos.y;
            else if (pos.y > bottomright.y) bottomright.y = pos.y;
        }
    }

    public void PauseScene(bool pause) {
        Time.timeScale = pause ? 0 : 1;
        paused = pause;
    }

    // Start is called once before the game starts
    private void Start() 
    {
        cm = CrowdManager.Instance;
        Application.targetFrameRate = targetFrameRate;
        Time.timeScale = 1;

        DateTime timenow = System.DateTime.Now;
        string _timenow = timenow.Month.ToString() + timenow.Day.ToString() + timenow.Hour.ToString() + timenow.Minute.ToString() + timenow.Second.ToString() + timenow.Millisecond.ToString();

        path = "./" + savePathRoot + "/" + "Batch " + _timenow.ToString();

        Debug.Log(path);

        // Make folder directory
        System.IO.Directory.CreateDirectory(path);
    }

    // We use late update instead so that we capture the screen after all the people have moved
    private void LateUpdate()
    {
        if (captureAfter > 0) {
            captureAfter -= Time.deltaTime;
            return;
        }

        if (paused) {
            PauseScene(false);
            return;
        }
        
        
        if (frameNumber < howManyFrames) {
            // Pause the game for this frame to perform all the necessary calculations
            PauseScene(true);

            // Increment frame number, make screenshot, get data
            frameNumber++;
            
            MakeScreenshot();
            string textData = GetFrameData();
            string fn = "data" + ".txt";

            // Write to data file
            using(StreamWriter sw = File.AppendText(path + "/" + fn))
            {
                sw.WriteLine(textData);
            }

            // Update the capture cooldown
            captureAfter = captureEvery;
        }
    }

    // Make a screenshot and save it in png formats
    private void MakeScreenshot() 
    {
        string picName = "Frame " + frameNumber.ToString() + ".png";
        ScreenCapture.CaptureScreenshot(path + "/" + picName);
    }

    // Generate the frame dat
    private string GetFrameData() 
    {
        string data = "Frame " + frameNumber.ToString() + ":\n";
        int count = 0;
        // The premise is we check visibility of everyone spawned, and then export the data into a nice text array. I have no idea how to export stuff into fancier formats like numpy array or h5 lmao
        for (int i = 0; i < cm.population.Count; i++) {
            // Get the corresponding person
            Crowd c = cm.population[i];

            // Calculate only if the person, especially its head, is visible
            if (IsVisible(c.transform.position, out Vector2Int pos) && IsInScreenArea(c.transform.position + Vector3.up * 1.6f, out Vector2Int hp)) {
                // Increment count and calculate bounding box
                count += 1;
                GetBoundingBox(c, hp, out Vector2Int topleft, out Vector2Int bottomright);
                
                // Assign all values first for easy modification. top, right etc are the bounds of the bounding box
                int x = hp.x;
                int y = hp.y;
                int id = c.id;
                int top = topleft.y;
                int bottom = bottomright.y;
                int left = topleft.x;
                int right = bottomright.x;

                data += "\tx:" + x.ToString() + "  y:" + y.ToString() + "  id:" + id.ToString() 
                + "  left:" + left.ToString() + "  top:" + top.ToString() + "  right:" + right.ToString() + "  bottom:" + bottom.ToString();

                data += "\n"; 
            }
        }
        data += "\nTotal count: " + count.ToString();
        data += "\nFrame rate: " + (1 / Time.deltaTime).ToString("F2");
        data += "\nframeend\n\n";
        Debug.Log(count);
        return data;
    }
}
