using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ScreenCapturer sc = ScreenCapturer.Instance;
        bool IsVisible = sc.IsVisible(transform.position, out Vector2Int pos);
        Debug.Log(IsVisible.ToString() + "   " + pos.ToString());
    }
}
