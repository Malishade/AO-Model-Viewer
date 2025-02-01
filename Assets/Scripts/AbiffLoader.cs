using AODB.Common.RDBObjects;
using Assimp;
using System.Collections.Generic;
using static AODB.Common.DbClasses.RDBMesh_t.FAFAnim_t;
using UnityEngine;
using AMaterial = Assimp.Material;
using AODB.Export;

internal class AbiffLoader : ModelLoader
{
    public GameObject CreateAbiffMesh(ResourceTypeId type, int meshId)
    {
        Debug.Log($"Loading mesh {meshId} {type}");
        RDBMesh rdbMesh = type == ResourceTypeId.RdbMesh ? RDBLoader.Instance.RdbController.Get<RDBMesh>(meshId) : RDBLoader.Instance.RdbController.Get<RDBMesh2>(meshId);
        var scene = AbiffConvert.ToAssimpScene(rdbMesh.RDBMesh_t, out var uvAnims, out var transKeys, out var rotKeys);
        AbiffLoader abiffToUnity = new AbiffLoader();
        GameObject rootNode = abiffToUnity.CreateAbiffMesh(rdbMesh, scene, $"MeshAnim_{meshId}", transKeys, rotKeys, uvAnims);

        return rootNode;
    }

    public GameObject CreateAbiffMesh(RDBMesh rdbMesh, Scene scene, string animName, Dictionary<string, List<VectorKey>> transKeys, Dictionary<string, List<QuaternionKey>> rotKeys, Dictionary<int, UVKey[]> uvAnims)
    {
        GameObject rootNode = CreateMeshNode(scene, scene.RootNode, uvAnims);

        if (transKeys.Count != 0 || rotKeys.Count != 0)
            AddAnimationData(rootNode, animName, rotKeys, transKeys);

        return rootNode;
    }

    public void ExportMesh(int meshId, string path)
    {
        RDBMesh mesh = RDBLoader.Instance.RdbController.Get<RDBMesh>(meshId);
        Scene scene = AbiffConvert.ToAssimpScene(mesh.RDBMesh_t, out _, out _, out _);

        foreach (AMaterial mat in scene.Materials)
        {
            if (mat.HasNonTextureProperty("DiffuseId"))
            {
                RDBLoader.Instance.ExportMeshTexture(mat.GetNonTextureProperty("DiffuseId").GetIntegerValue(), path, out string diffuseName);

                TextureSlot diffuse = new TextureSlot
                {
                    FilePath = diffuseName,
                    TextureType = TextureType.Diffuse,
                    TextureIndex = 0
                };

                RDBLoader.AddMaterialTexture(mat, ref diffuse, false);
            }

            if (mat.HasNonTextureProperty("EmissionId"))
            {
                RDBLoader.Instance.ExportMeshTexture(mat.GetNonTextureProperty("EmissionId").GetIntegerValue(), path, out string emissionName);

                TextureSlot emission = new TextureSlot
                {
                    FilePath = emissionName,
                    TextureType = TextureType.Emissive,
                    TextureIndex = 0,
                };

                RDBLoader.AddMaterialTexture(mat, ref emission, false);
            }
        }

        //Scale x100 and flip on x axis for correct transforms
        foreach (var s in scene.RootNode.Children)
            s.Transform = new Assimp.Matrix4x4(
            -100f, 0, 0, 0,
            0, 100f, 0, 0,
            0, 0, 100f, 0,
            0, 0, 0, 1);
        new AssimpContext().ExportFile(scene, path, "fbx");
    }
}