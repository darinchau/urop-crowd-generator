using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    public static T GetOrAddComponent<T>(this GameObject targetGameObject) where T : Component
    {
        T existingComponent = targetGameObject.GetComponent<T>();
        if (existingComponent != null)
        {
            return existingComponent;
        }

        T component = targetGameObject.AddComponent<T>();

        return component;
    }

    public static bool isSameVector(Vector3 a, Vector3 b) {
        return (a-b).magnitude < 1e-5;
    }

    public static float HDist(Vector3 a, Vector3 b) {
        return Vector3.Distance(Vector3.ProjectOnPlane(a, Vector3.up), Vector3.ProjectOnPlane(b, Vector3.up));
    }

    public static float HAngle(Vector3 a, Vector3 b) {
        return Vector3.Angle(Vector3.ProjectOnPlane(a, Vector3.up), Vector3.ProjectOnPlane(b, Vector3.up));
    }
}
