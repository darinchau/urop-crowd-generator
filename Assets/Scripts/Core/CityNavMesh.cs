using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityNavMesh : MonoBehaviour
{

    // This makes everything with a mesh renderer child under the object to be navigation static. Useful to put on the root object of the city
    public void UpdateNavMesh() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        for(int i = 0; i < meshes.Length; i++) {
            // Set its navigation static flag
            GameObject g = meshes[i].gameObject;
            g.isStatic = true;
        }
    }

    // This makes
}
