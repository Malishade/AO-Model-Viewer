using AODB;
using AODB.RDBObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AODB.Common.DbClasses.RDBMesh_t;

[CreateAssetMenu]
public class RDBLoader : ScriptableSingleton<RDBLoader>
{
    public Dictionary<int, string> MeshNames = null;

    private Dictionary<int, Dictionary<int, string>> Names = null;
    public bool IsOpen => _rdbController != null;

    private Settings _settings;
    private RdbController _rdbController;

    protected override void OnInitialize()
    {
        _settings = SettingsManager.Instance.Settings;
    }

    public void OpenDatabase()
    {
        if (_rdbController != null)
            return;

        _rdbController = new RdbController(_settings.AODirectory);

        if (Names == null)
        {
            Names = _rdbController.Get<InfoObject>(1).Types;
            MeshNames = Names[(int)ResourceTypeId.RdbMesh].Where(x => _rdbController.RecordTypeToId[(int)ResourceTypeId.RdbMesh].ContainsKey((int)x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public void CloseDatabase()
    {
        if (_rdbController == null)
            return;

        _rdbController.Dispose();
    }

    public List<GameObject> CreateAbiffMesh(int meshId)
    {
        Debug.Log($"Loading mesh {meshId}");
        List<GameObject> meshes = new List<GameObject>();

        RDBMesh rdbMesh = _rdbController.Get<RDBMesh>(meshId);

        int i = 0;

        foreach (Submesh mesh in rdbMesh.SubMeshes)
        {
            GameObject submeshObj = new GameObject(mesh.Material != null ? mesh.Material.Texture.ToString() : "Unknown");
            MeshFilter meshRenderer = submeshObj.AddComponent<MeshFilter>();

            meshRenderer.mesh = new Mesh()
            {
                vertices = mesh.Vertices.Select(x => x.Position).ToUnityArray(),
                triangles = mesh.Triangles,
                normals = mesh.Vertices.Select(x => x.Normals).ToUnityArray(),
                uv = mesh.Vertices.Select(x => new Vector2(x.UVs.X, -x.UVs.Y)).ToArray(),
            };

            if (mesh.Material != null)
                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterial((int)mesh.Material.Texture);

            submeshObj.transform.position = mesh.BasePos.ToUnity();
            submeshObj.transform.rotation = mesh.BaseRotation.ToUnity();

            meshes.Add(submeshObj);
            i++;
        }

        return meshes;
    }

    private Material LoadMaterial(int texId)
    {
        Debug.Log($"Loading texture {texId}");
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(_rdbController.Get<AOTexture>(texId).JpgData);

        Material mat = new Material(Shader.Find("Diffuse"));
        mat.mainTexture = tex;

        return mat;
    }
}
