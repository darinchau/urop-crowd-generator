using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenCapturer : Singleton<ScreenCapturer>
{
    public string savePath = "haha";
    public float captureAfter = 10f;
    public int howManyFrames = 300;
    [Tooltip("Number of raycasts to perform to check obscurity of the human being"), Range(2, 5)] public int rayCasts = 4;

    // Gets the screenPos by reference. It will return something (negative values, etc) even if the object is not on screen
    // World Position ought to be the transform.position of whatever object whose world position we are trying to get.
    public bool IsVisible(Vector3 worldPos, out Vector2Int screenPos) {
        Camera c = Camera.main;
        screenPos = new Vector2Int();
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


    // We use late update instead so that we capture the screen after all the people have moved
    public void LateUpdate()
    {
        
    }
}
