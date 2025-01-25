using AODB;
using AODB.Common;
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
using AODB.Common.RDBObjects;
using static MainViewUxml;
using Transform = UnityEngine.Transform;
using Animation = UnityEngine.Animation;

[CreateAssetMenu]
public class RDBLoader : ScriptableSingleton<RDBLoader>
{
    public Dictionary<ResourceTypeId, Dictionary<int, string>> Names = null;
    public Dictionary<ResourceTypeId, List<int>> Records; 

    public bool IsOpen => _rdbController != null;

    private Settings _settings;
    private RdbController _rdbController;

    protected override void OnInitialize()
    {
#if UNITY_EDITOR
        AssimpLibrary.Instance.LoadLibrary($"{Application.dataPath}\\Plugins\\assimp");
#endif
#if UNITY_STANDALONE_WIN
        AssimpLibrary.Instance.LoadLibrary($"{Application.dataPath}\\Plugins\\x86_64\\assimp");
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
            Names[ResourceTypeId.RdbMesh] = Names[ResourceTypeId.RdbMesh].Where(x => _rdbController.RecordTypeToId[(int)ResourceTypeId.RdbMesh].ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            Names[ResourceTypeId.Texture] = Names[ResourceTypeId.Texture].Where(x => _rdbController.RecordTypeToId[(int)ResourceTypeId.Texture].ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }

        Records = _rdbController.RecordTypeToId.ToDictionary(x => (ResourceTypeId)x.Key, x => x.Value.Keys.ToList());
    }

    public void CloseDatabase()
    {
        if (_rdbController == null)
            return;

        _rdbController.Dispose();
    }

    public void ExportMesh(int meshId, string path)
    {
        Scene scene;

        try
        {
            RDBMesh mesh = _rdbController.Get<RDBMesh>(meshId);
            scene = AbiffConverter.ToAssimpScene(mesh.RDBMesh_t, out _, out _, out _);

            foreach (AMaterial mat in scene.Materials)
            {
                if (mat.HasNonTextureProperty("DiffuseId"))
                {
                    ExportMeshTexture(mat.GetNonTextureProperty("DiffuseId").GetIntegerValue(), path, out string diffuseName);

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
                    ExportMeshTexture(mat.GetNonTextureProperty("EmissionId").GetIntegerValue(), path, out string emissionName);

                    TextureSlot emission = new TextureSlot
                    {
                        FilePath = emissionName,
                        TextureType = TextureType.Emissive,
                        TextureIndex = 0,
                    };

                    AddMaterialTexture(mat, ref emission, false);
                }
            }
        }
        catch
        {
            return;
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

    public void ExportMeshTexture(int texId, string path, out string texName)
    {
        texName = Names[ResourceTypeId.Texture].TryGetValue(texId, out string rdbName) ? rdbName.Trim('\0') : $"UnnamedTex_{texId}";
        File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<AOTexture>(texId).JpgData);
    }

    public void ExportDataModel(ListViewDataModel dataModel, string path)
    {
        string texName = Names.TryGetValue(dataModel.ResourceType, out var resourceNames) && resourceNames.TryGetValue(dataModel.Id, out string rdbName) ? rdbName.Trim('\0') : $"UnnamedTex_{dataModel.Id}.png";

        switch (dataModel.ResourceType)
        {
            case ResourceTypeId.Texture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<AOTexture>(dataModel.Id).JpgData);
                break;
            case ResourceTypeId.WallTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<WallTexture>(dataModel.Id).JpgData);
                break;
            case ResourceTypeId.SkinTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<SkinTexture>(dataModel.Id).JpgData);
                break;
            case ResourceTypeId.Icon:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<IconTexture>(dataModel.Id).JpgData);
                break;
            case ResourceTypeId.GroundTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", _rdbController.Get<GroundTexture>(dataModel.Id).JpgData);
                break;
            default:
                break;
        }
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
                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterialOld(ResourceTypeId.Texture, (int)mesh.Material.Texture);

            submeshObj.transform.position = mesh.BasePos.ToUnity();
            submeshObj.transform.rotation = mesh.BaseRotation.ToUnity();

            meshes.Add(submeshObj);
            i++;
        }

        return meshes;
    }

    public GameObject CreateAbiffMesh(ResourceTypeId type, int meshId)
    {
        Dictionary<int, UVKey[]> uvAnims;
        Dictionary<string, List<QuaternionKey>> rotKeys;
        Dictionary<string, List<VectorKey>> transKeys;
        Scene scene;

        try
        {
            Debug.Log($"Loading mesh {meshId} {type}");

            RDBMesh rdbMesh;

            rdbMesh = type == ResourceTypeId.RdbMesh ? _rdbController.Get<RDBMesh>(meshId) : _rdbController.Get<RDBMesh2>(meshId); 
            
            scene = AbiffConverter.ToAssimpScene(rdbMesh.RDBMesh_t, out uvAnims, out transKeys, out rotKeys);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return null;
        }

        var rootNode = CreateNode(scene, scene.RootNode, uvAnims, transKeys, rotKeys);

        if (transKeys.Count != 0)
        {
            AnimationClip animClip = new AnimationClip();
            animClip.legacy = true;
            animClip.wrapMode = WrapMode.Loop;

            animClip.name = $"Anim_{meshId}";

            foreach (var rotKey in rotKeys)
            {
                animClip.SetCurve(FindChildPath(rootNode.transform, rotKey.Key), typeof(Transform), "localRotation.x", new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.X)).ToArray()));
                animClip.SetCurve(FindChildPath(rootNode.transform, rotKey.Key), typeof(Transform), "localRotation.y", new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Y)).ToArray()));
                animClip.SetCurve(FindChildPath(rootNode.transform, rotKey.Key), typeof(Transform), "localRotation.z", new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Z)).ToArray()));
                animClip.SetCurve(FindChildPath(rootNode.transform, rotKey.Key), typeof(Transform), "localRotation.w", new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.W)).ToArray()));
            }

            foreach (var transKey in transKeys)
            {
                animClip.SetCurve(FindChildPath(rootNode.transform, transKey.Key), typeof(Transform), "localPosition.x", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.X)).ToArray()));
                animClip.SetCurve(FindChildPath(rootNode.transform, transKey.Key), typeof(Transform), "localPosition.y", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Y)).ToArray()));
                animClip.SetCurve(FindChildPath(rootNode.transform, transKey.Key), typeof(Transform), "localPosition.z", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Z)).ToArray()));
            }

            var anim = rootNode.AddComponent<Animation>();
            anim.clip = animClip;
            anim.AddClip(animClip, animClip.name);
            anim.Play();
        }


        return rootNode;
    }
    public string FindChildPath(Transform parent, string childName)
    {
        Transform target = FindChildRecursive(parent, childName);
        if (target != null)
        {
            return GetPathToRoot(target, parent);
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private string GetPathToRoot(Transform child, Transform root)
    {
        string path = child.name;
        Transform current = child;

        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private GameObject CreateNode(Scene scene, Node node, Dictionary<int, UVKey[]> uvAnims, Dictionary<string, List<VectorKey>> transKeys, Dictionary<string, List<QuaternionKey>> rotKeys)
    {
        GameObject nodeObj = new GameObject(node.Name);

        node.Transform.Decompose(out Vector3D scale, out AQuaternion rotation, out Vector3D position);
        nodeObj.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z);
        nodeObj.transform.rotation = new UnityEngine.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        nodeObj.transform.position = new Vector3(position.X, position.Y, position.Z);

        if (node.HasMeshes)
        {
            foreach (var meshIdx in node.MeshIndices)
            {
                GameObject submeshObj = new GameObject("MeshPart");
                MeshFilter meshFilter = submeshObj.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateMesh(scene.Meshes[meshIdx]);

                AMaterial material = scene.Materials[scene.Meshes[meshIdx].MaterialIndex];

                submeshObj.AddComponent<MeshRenderer>().material = LoadMaterial(material);
                submeshObj.transform.SetParent(nodeObj.transform, false);

                if (uvAnims.TryGetValue(meshIdx, out UVKey[] uvAnim) && uvAnim != null)
                {
                    submeshObj.AddComponent<UVAnimation>().Init(uvAnim);
                }
            }
        }

        foreach (Node child in node.Children)
        {
            GameObject childObj = CreateNode(scene, child, uvAnims, transKeys, rotKeys);
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
        Material aoMat = new Material(Shader.Find("Custom/AOShader"));

        if (material.HasNonTextureProperty("ApplyAlpha"))
        {
            //Meow
            if (material.GetNonTextureProperty("ApplyAlpha").GetBooleanValue())
            {
                aoMat.SetFloat("_Cutoff", 0.05f);
                aoMat.SetFloat("_Transparency", 1f);
                aoMat.SetFloat("_Src", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                aoMat.SetFloat("_Dst", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            //Cutoff
            //aoMat.SetFloat("_Cutoff", material.GetNonTextureProperty("ApplyAlpha").GetBooleanValue() ? 0.5f : 0);
            //aoMat.SetFloat("_Transparency", 1f);
            //aoMat.SetFloat("_ZWrite", 1f);
            //aoMat.SetFloat("_Src", (int)UnityEngine.Rendering.BlendMode.One);
            //aoMat.SetFloat("_Dest", (int)UnityEngine.Rendering.BlendMode.Zero);

            //Transparency
            //aoMat.SetFloat("_Cutoff", 1f);
            //aoMat.SetFloat("_Transparency", 0.5f);
            //aoMat.SetFloat("_ZWrite", 0f);
            //aoMat.SetFloat("_Src", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            //aoMat.SetFloat("_Dest", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.IsTwoSided)
        {
            aoMat.SetFloat("_DoubleSided", 0.5f);
        }

        if (material.HasNonTextureProperty("DiffuseId"))
        {
            int diffuseId = material.GetNonTextureProperty("DiffuseId").GetIntegerValue();
            Debug.Log($"Loading diffuse texture {diffuseId}");
            Texture2D diffuseTex = new Texture2D(1, 1);

            var jpegData = _rdbController.Get<AOTexture>(diffuseId)?.JpgData;

            if (jpegData != null)
                diffuseTex.LoadImage(jpegData);

            aoMat.mainTexture = diffuseTex;
        }

        if (material.HasNonTextureProperty("EmissionId"))
        {
            int emissionId = material.GetNonTextureProperty("EmissionId").GetIntegerValue();
            Debug.Log($"Loading emission texture {emissionId}");
            //Texture2D emissionTex = new Texture2D(1, 1);
            //emissionTex.LoadImage(_rdbController.Get<AOTexture>(emissionId).JpgData);

            aoMat.SetFloat("_Emission", 1);
        }

        return aoMat;
    }

    public Material LoadMaterialOld(ResourceTypeId type, int texId)
    {
        Material mat;

        try
        {
            Debug.Log($"Loading texture {texId}");
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(_rdbController.Get<Image>(type, texId).JpgData);

            mat = new Material(Shader.Find("Custom/AOShader"));
            mat.mainTexture = tex;
        }
        catch
        {
            return null;
        }

        return mat;
    }


    public Material LoadGroundMaterial(ResourceTypeId type, int texId)
    {
        Material mat;

        try
        {
            Debug.Log($"Loading ground texture {texId}");
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(_rdbController.Get<GroundTexture>(type, texId).JpgData);

            mat = new Material(Shader.Find("Custom/AOShader"));
            mat.mainTexture = tex;
        }
        catch
        {
            return null;
        }

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
