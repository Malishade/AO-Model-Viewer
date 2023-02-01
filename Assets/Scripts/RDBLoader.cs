using AODB;
using AODB.Common;
using AODB.RDBObjects;
using Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AODB.Common.DbClasses.RDBMesh_t;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using AMesh = Assimp.Mesh;
using AMaterial = Assimp.Material;
using AMaterialProperty = Assimp.MaterialProperty;
using AQuaternion = Assimp.Quaternion;
using Assimp.Unmanaged;
using static AODB.Common.DbClasses.RDBMesh_t.FAFAnim_t;

[CreateAssetMenu]
public class RDBLoader : ScriptableSingleton<RDBLoader>
{
    public Dictionary<int, Dictionary<int, string>> Names = null;

    public bool IsOpen => _rdbController != null;

    private Settings _settings;
    private RdbController _rdbController;

    protected override void OnInitialize()
    {
#if UNITY_EDITOR
        AssimpLibrary.Instance.LoadLibrary($"{Application.dataPath}\\Plugins\\assimp");
#endif
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
            Names[(int)ResourceTypeId.RdbMesh] = Names[(int)ResourceTypeId.RdbMesh].Where(x => _rdbController.RecordTypeToId[(int)ResourceTypeId.RdbMesh].ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            Names[(int)ResourceTypeId.Texture] = Names[(int)ResourceTypeId.Texture].Where(x => _rdbController.RecordTypeToId[(int)ResourceTypeId.Texture].ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public void CloseDatabase()
    {
        if (_rdbController == null)
            return;

        _rdbController.Dispose();
    }

    public void ExportMesh(int meshId, string path)
    {
        RDBMesh mesh = _rdbController.Get<RDBMesh>(meshId);
        Scene scene = AbiffConverter.ToAssimpScene(mesh.RDBMesh_t, out _);

        foreach (AMaterial mat in scene.Materials)
        {
            if (mat.HasNonTextureProperty("DiffuseId"))
            {
                ExportTexture(path, mat.GetNonTextureProperty("DiffuseId").GetIntegerValue(), out string diffuseName);

                TextureSlot diffuse = new TextureSlot
                {
                    FilePath = diffuseName,
                    TextureType = TextureType.Diffuse,
                    TextureIndex = 0
                };

                AddMaterialTexture(mat, ref diffuse, false);
            }

            if (mat.HasNonTextureProperty("EmissionId"))
            {
                ExportTexture(path, mat.GetNonTextureProperty("EmissionId").GetIntegerValue(), out string emissionName);

                TextureSlot emission = new TextureSlot
                {
                    FilePath = emissionName,
                    TextureType = TextureType.Emissive,
                    TextureIndex = 0,
                };

                AddMaterialTexture(mat, ref emission, false);
            }
        }

        new AssimpContext().ExportFile(scene, path, "fbx");
    }

    public void ExportTexture(string exportPath, int texId, out string texName)
    {
        texName = Names[(int)ResourceTypeId.Texture].TryGetValue(texId, out string rdbName) ? rdbName.Trim('\0') : $"UnnamedTex_{texId}";
        File.WriteAllBytes($"{Path.GetDirectoryName(exportPath)}\\{texName}", _rdbController.Get<AOTexture>(texId).JpgData);
    }

    public List<GameObject> CreateAbiffMeshOld(int meshId)
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
                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterialOld((int)mesh.Material.Texture);

            submeshObj.transform.position = mesh.BasePos.ToUnity();
            submeshObj.transform.rotation = mesh.BaseRotation.ToUnity();

            meshes.Add(submeshObj);
            i++;
        }

        return meshes;
    }

    public GameObject CreateAbiffMesh(int meshId)
    {
        Debug.Log($"Loading mesh {meshId}");

        RDBMesh rdbMesh = _rdbController.Get<RDBMesh>(meshId);
        Scene scene = AbiffConverter.ToAssimpScene(rdbMesh.RDBMesh_t, out var uvAnims);

        return CreateNode(scene, scene.RootNode, uvAnims);
    }

    private GameObject CreateNode(Scene scene, Node node, Dictionary<int, UVKey[]> uvAnims)
    {
        GameObject nodeObj = new GameObject(node.Name);

        node.Transform.Decompose(out Vector3D scale, out AQuaternion rotation, out Vector3D position);
        nodeObj.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z);
        nodeObj.transform.rotation = new UnityEngine.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        nodeObj.transform.position = new Vector3(position.X, position.Y, position.Z);

        if(node.HasMeshes)
        {
            foreach (var meshIdx in node.MeshIndices)
            {
                GameObject submeshObj = new GameObject("MeshPart");
                MeshFilter meshFilter = submeshObj.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateMesh(scene.Meshes[meshIdx]);

                AMaterial material = scene.Materials[scene.Meshes[meshIdx].MaterialIndex];

                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterial(material);
                submeshObj.transform.SetParent(nodeObj.transform, false);

                if (uvAnims.TryGetValue(meshIdx, out UVKey[] uvAnim))
                {
                    submeshObj.AddComponent<UVAnimation>().Init(uvAnim);
                }
            }
        }

        foreach (Node child in node.Children)
        {
            GameObject childObj = CreateNode(scene, child, uvAnims);
            childObj.transform.SetParent(nodeObj.transform, false);
        }

        return nodeObj;
    }

    public Mesh CreateMesh(AMesh mesh)
    {
        return new Mesh()
        {
            vertices = mesh.Vertices.ToUnityArray(),
            triangles = mesh.GetIndices(),
            normals = mesh.Normals.ToUnityArray(),
            uv = mesh.TextureCoordinateChannels[0].ToUnity2DArray(),
        };
    }

    public Material LoadMaterial(AMaterial material)
    {
        Material unityMat = new Material(Shader.Find("Standard"));

        if (material.HasNonTextureProperty("ApplyAlpha"))
        {
            unityMat.SetFloat("_mode", material.GetNonTextureProperty("ApplyAlpha").GetBooleanValue() ? 1 : 0);
        }

        if (material.HasNonTextureProperty("DiffuseId"))
        {
            int diffuseId = material.GetNonTextureProperty("DiffuseId").GetIntegerValue();
            Debug.Log($"Loading diffuse texture {diffuseId}");
            Texture2D diffuseTex = new Texture2D(1, 1);
            diffuseTex.LoadImage(_rdbController.Get<AOTexture>(diffuseId).JpgData);

            unityMat.SetFloat("_Glossiness", 0.35f);
            unityMat.mainTexture = diffuseTex;
        }

        if (material.HasNonTextureProperty("EmissionId"))
        {
            int emissionId = material.GetNonTextureProperty("EmissionId").GetIntegerValue();
            Debug.Log($"Loading emission texture {emissionId}");
            Texture2D emissionTex = new Texture2D(1, 1);
            emissionTex.LoadImage(_rdbController.Get<AOTexture>(emissionId).JpgData);

            unityMat.EnableKeyword("_Emission");
            unityMat.SetTexture("_EmissionMap", emissionTex);
            unityMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        return unityMat;
    }

    public Material LoadMaterialOld(int texId)
    {
        Debug.Log($"Loading texture {texId}");
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(_rdbController.Get<AOTexture>(texId).JpgData);

        Material mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = tex;

        return mat;
    }

    public bool AddMaterialTexture(AMaterial mat, ref TextureSlot texture, bool onlySetFilePath)
    {
        if (string.IsNullOrEmpty(texture.FilePath))
            return false;

        TextureType texType = texture.TextureType;
        int texIndex = texture.TextureIndex;

        string texName = AMaterial.CreateFullyQualifiedName(AiMatKeys.TEXTURE_BASE, texType, texIndex);

        AMaterialProperty texNameProp = mat.GetProperty(texName);

        if (texNameProp == null)
            mat.AddProperty(new AMaterialProperty(AiMatKeys.TEXTURE_BASE, texture.FilePath, texType, texIndex));
        else
            texNameProp.SetStringValue(texture.FilePath);

        if (onlySetFilePath)
            return true;

        string mappingName = AMaterial.CreateFullyQualifiedName(AiMatKeys.MAPPING_BASE, texType, texIndex);
        string uvIndexName = AMaterial.CreateFullyQualifiedName(AiMatKeys.UVWSRC_BASE, texType, texIndex);
        string blendFactorName = AMaterial.CreateFullyQualifiedName(AiMatKeys.TEXBLEND_BASE, texType, texIndex);
        string texOpName = AMaterial.CreateFullyQualifiedName(AiMatKeys.TEXOP_BASE, texType, texIndex);
        string uMapModeName = AMaterial.CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_U_BASE, texType, texIndex);
        string vMapModeName = AMaterial.CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_V_BASE, texType, texIndex);
        string texFlagsName = AMaterial.CreateFullyQualifiedName(AiMatKeys.TEXFLAGS_BASE, texType, texIndex);

        AMaterialProperty mappingNameProp = mat.GetProperty(mappingName);
        AMaterialProperty uvIndexNameProp = mat.GetProperty(uvIndexName);
        AMaterialProperty blendFactorNameProp = mat.GetProperty(blendFactorName);
        AMaterialProperty texOpNameProp = mat.GetProperty(texOpName);
        AMaterialProperty uMapModeNameProp = mat.GetProperty(uMapModeName);
        AMaterialProperty vMapModeNameProp = mat.GetProperty(vMapModeName);
        AMaterialProperty texFlagsNameProp = mat.GetProperty(texFlagsName);

        if (mappingNameProp == null)
        {
            mappingNameProp = new AMaterialProperty(AiMatKeys.MAPPING_BASE, (int)texture.Mapping);
            mappingNameProp.TextureIndex = texIndex;
            mappingNameProp.TextureType = texType;
            mat.AddProperty(mappingNameProp);
        }
        else
        {
            mappingNameProp.SetIntegerValue((int)texture.Mapping);
        }

        if (uvIndexNameProp == null)
        {
            uvIndexNameProp = new AMaterialProperty(AiMatKeys.UVWSRC_BASE, texture.UVIndex);
            uvIndexNameProp.TextureIndex = texIndex;
            uvIndexNameProp.TextureType = texType;
            mat.AddProperty(uvIndexNameProp);
        }
        else
        {
            uvIndexNameProp.SetIntegerValue(texture.UVIndex);
        }

        if (blendFactorNameProp == null)
        {
            blendFactorNameProp = new AMaterialProperty(AiMatKeys.TEXBLEND_BASE, texture.BlendFactor);
            blendFactorNameProp.TextureIndex = texIndex;
            blendFactorNameProp.TextureType = texType;
            mat.AddProperty(blendFactorNameProp);
        }
        else
        {
            blendFactorNameProp.SetFloatValue(texture.BlendFactor);
        }

        if (texOpNameProp == null)
        {
            texOpNameProp = new AMaterialProperty(AiMatKeys.TEXOP_BASE, (int)texture.Operation);
            texOpNameProp.TextureIndex = texIndex;
            texOpNameProp.TextureType = texType;
            mat.AddProperty(texOpNameProp);
        }
        else
        {
            texOpNameProp.SetIntegerValue((int)texture.Operation);
        }

        if (uMapModeNameProp == null)
        {
            uMapModeNameProp = new AMaterialProperty(AiMatKeys.MAPPINGMODE_U_BASE, (int)texture.WrapModeU);
            uMapModeNameProp.TextureIndex = texIndex;
            uMapModeNameProp.TextureType = texType;
            mat.AddProperty(uMapModeNameProp);
        }
        else
        {
            uMapModeNameProp.SetIntegerValue((int)texture.WrapModeU);
        }

        if (vMapModeNameProp == null)
        {
            vMapModeNameProp = new AMaterialProperty(AiMatKeys.MAPPINGMODE_V_BASE, (int)texture.WrapModeV);
            vMapModeNameProp.TextureIndex = texIndex;
            vMapModeNameProp.TextureType = texType;
            mat.AddProperty(vMapModeNameProp);
        }
        else
        {
            vMapModeNameProp.SetIntegerValue((int)texture.WrapModeV);
        }

        if (texFlagsNameProp == null)
        {
            texFlagsNameProp = new AMaterialProperty(AiMatKeys.TEXFLAGS_BASE, texture.Flags);
            texFlagsNameProp.TextureIndex = texIndex;
            texFlagsNameProp.TextureType = texType;
            mat.AddProperty(texFlagsNameProp);
        }
        else
        {
            texFlagsNameProp.SetIntegerValue(texture.Flags);
        }

        return true;
    }

}
