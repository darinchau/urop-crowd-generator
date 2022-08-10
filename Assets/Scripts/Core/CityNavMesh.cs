using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityNavMesh : MonoBehaviour
{   
    // Makes everything navigation static for navmesh bakery
    public void UpdateNavMesh() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        for(int i = 0; i < meshes.Length; i++) {
            // Set its navigation static flag
            GameObject g = meshes[i].gameObject;
            if (!g.name.StartsWith("Double")) {
                g.isStatic = true;
            }
            
        }
    }

    // Makes all colliders mesh colliders for visibility
    public void UpdateCollider() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        for(int i = 0; i < meshes.Length; i++) {
            MeshRenderer m = meshes[i];
            GameObject g = meshes[i].gameObject;
            // Destroy the colliders
            try {
                DestroyImmediate(g.transform.GetComponent<CapsuleCollider>());
            } catch { 
                // Do nothing
            }

            try {
                DestroyImmediate(g.transform.GetComponent<BoxCollider>());
            } catch { 
                // Do nothing
            }

            try {
                DestroyImmediate(g.transform.GetComponent<MeshCollider>());
            } catch { 
                // Do nothing
            }

            // Add a mesh renderer
            g.AddComponent<MeshCollider>();


        }
    }

    float countdown = 5f;
    bool lightsoff = false;
    ScreenCapturer sc;

    void Start() {
        sc = ScreenCapturer.Instance;
    }

    // Lights off
    void LateUpdate() {
        if (countdown < 0 && !lightsoff) {
            Debug.Log("Lights off :)");
            MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
            
            for (int i = 0; i < meshes.Length; i++) {
                MeshRenderer m = meshes[i];
                if (!m.isVisible) {
                    m.enabled = false;
                    Destroy(m);
                }  
            }

            lightsoff = true;
        } else if (countdown > 0) {
            countdown -= Time.deltaTime;
        }
    }
}
