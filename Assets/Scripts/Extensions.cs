using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static Vector2[] ToUnity2DArray(this IEnumerable<Assimp.Vector3D> vector3) => vector3.Select(x => new Vector2(x.X, x.Y)).ToArray();
    public static Vector3[] ToUnityArray(this IEnumerable<Assimp.Vector3D> vector3) => vector3.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
    public static Vector3[] ToUnityArray(this IEnumerable<AODB.Common.Structs.Vector3> vector3) => vector3.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
    public static Vector3 ToUnity(this AODB.Common.Structs.Vector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);
    public static Quaternion ToUnity(this AODB.Common.Structs.Quaternion quaternion) => new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}