using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static Bounds GetMeshBounds(this Transform transform)
    {
        Bounds bounds = new Bounds();

        foreach (MeshRenderer child in transform.GetComponentsInChildren<MeshRenderer>())
        {
            bounds.Encapsulate(child.bounds);
        }

        return bounds;
    }

    public static Vector3[] ToUnityArray(this IEnumerable<AODB.Structs.Vector3> vector3) => vector3.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
    public static Vector3 ToUnity(this AODB.Structs.Vector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);
    public static Quaternion ToUnity(this AODB.Structs.Quaternion quaternion) => new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}
