using AODB;
using AODB.RDBObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AODB.DbClasses.RDBMesh_t;

public class RDBLoader : MonoBehaviour
{
    private string _aopath = @"D:\Anarchy Online";
    private RdbController _rdbController;

    public List<RdbData> LoadNames()
    {
        _rdbController = new RdbController(_aopath);
        var RDBNames = _rdbController.Get<InfoObject>(1).Types;
        return RDBNames[1010001].Select(x => new RdbData { MeshId = (uint)x.Key, MeshName = x.Value }).ToList();
    }

    public List<GameObject> CreateAbiffMesh(uint meshId)
    {
        List<GameObject> meshes = new List<GameObject>();

        RDBMesh rdbMesh = _rdbController.Get<RDBMesh>(meshId);

        int i = 0;

        foreach (Submesh mesh in rdbMesh.SubMeshes)
        {
            GameObject submeshObj = new GameObject(mesh.Material != null ? mesh.Material.Texture.ToString() : "Unknown");
            MeshFilter meshRenderer = submeshObj.AddComponent<MeshFilter>();

            meshRenderer.mesh = new Mesh()
            {
                vertices = mesh.Vertices.Select(x => x.position).ToUnityArray(),
                triangles = mesh.Triangles,
                normals = mesh.Vertices.Select(x => x.normals).ToUnityArray(),
                uv = mesh.Vertices.Select(x => new Vector2(x.uvs.X, -x.uvs.Y)).ToArray(),
            };

            if (mesh.Material != null)
                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterial(mesh.Material.Texture);

            submeshObj.transform.position = mesh.BasePos.ToUnity();
            submeshObj.transform.rotation = mesh.BaseRotation.ToUnity();

            meshes.Add(submeshObj);
            i++;
        }

        return meshes;
    }

    private Material LoadMaterial(uint texId)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(_rdbController.Get<AOTexture>(texId).JpgData);

        Material mat = new Material(Shader.Find("Diffuse"));
        mat.mainTexture = tex;

        return mat;
    }

    public class RdbData
    {
        public string MeshName;
        public uint MeshId;
    }
}
