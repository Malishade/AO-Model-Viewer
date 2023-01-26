using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static Bounds GetMeshBounds(this List<MeshRenderer> meshRenderers)
    {
        Bounds bounds = new Bounds();

        foreach (MeshRenderer child in meshRenderers)
        {
            bounds.Encapsulate(child.bounds);
        }

        return bounds;
    }

    public static List<MeshRenderer> GetMeshRenderers(this List<GameObject> gameObjects)
    {
        List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        foreach (var gameObject in gameObjects)
        {
            foreach (MeshRenderer child in gameObject.transform.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderers.Add(child);
            }
        }

        return meshRenderers;
    }

    public static void SetMeshRendererParameters(this List<MeshRenderer> meshes, Material material, out int vertexCount, out int triCount)
    {
        triCount = 0;
        vertexCount = 0;

        foreach (MeshRenderer mesh in meshes)
        {
            var texture = mesh.material.GetTexture("_MainTex");
            mesh.material = material;
            mesh.material.SetTexture("_MainTex", texture);

            var meshFilter = mesh.gameObject.GetComponent<MeshFilter>();
            vertexCount += meshFilter.sharedMesh.vertexCount;
            triCount += meshFilter.sharedMesh.triangles.Length/3;
        }
    }

    public static Vector3[] ToUnityArray(this IEnumerable<AODB.Structs.Vector3> vector3) => vector3.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
    public static Vector3 ToUnity(this AODB.Structs.Vector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);
    public static Quaternion ToUnity(this AODB.Structs.Quaternion quaternion) => new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}
