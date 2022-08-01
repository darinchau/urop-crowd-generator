using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityNavMesh : MonoBehaviour
{
    public void UpdateNavMesh() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        for(int i = 0; i < meshes.Length; i++) {
            // Set its navigation static flag
            GameObject g = meshes[i].gameObject;
            g.isStatic = true;
        }
    }
}
