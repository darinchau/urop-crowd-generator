using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{

    ScreenCapturer sc;

    // Start is called before the first frame update
    void Start()
    {
        sc = ScreenCapturer.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        bool IsVisible = sc.IsHumanVisible(transform.position, out Vector2Int pos);
        Debug.Log("Is visible? " + IsVisible.ToString() + " Position: " + pos.ToString());
    }
}
