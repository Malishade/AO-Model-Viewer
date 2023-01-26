using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MainViewUxml;

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

    public static void GetMeshData(this List<MeshRenderer> meshes, out MeshData meshData)
    {


        meshData = new ();
        meshData.MeshRendererData = new();

        foreach (MeshRenderer mesh in meshes)
        {
            var texture = mesh.material.GetTexture("_MainTex");

            if (texture != null)
                meshData.MeshRendererData.Add(mesh, texture);

            var meshFilter = mesh.gameObject.GetComponent<MeshFilter>();
            meshData.VerticesCount += meshFilter.sharedMesh.vertexCount;
            meshData.TrianglesCount += meshFilter.sharedMesh.triangles.Length/3;
        }
    }

    public static Vector3[] ToUnityArray(this IEnumerable<AODB.Structs.Vector3> vector3) => vector3.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
    public static Vector3 ToUnity(this AODB.Structs.Vector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);
    public static Quaternion ToUnity(this AODB.Structs.Quaternion quaternion) => new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
}
