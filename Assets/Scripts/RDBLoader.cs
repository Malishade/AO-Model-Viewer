using AODB;
using AODB.Common;
using Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Material = UnityEngine.Material;
using AMaterial = Assimp.Material;
using AMaterialProperty = Assimp.MaterialProperty;
using Assimp.Unmanaged;
using AODB.Common.RDBObjects;
using static MainViewUxml;
using JetBrains.Annotations;
using Assets.Scripts;
using Newtonsoft.Json;


[CreateAssetMenu]
public class RDBLoader : ScriptableSingleton<RDBLoader>
{
    public Dictionary<ResourceTypeId, Dictionary<int, string>> Names = null;
    public Dictionary<ResourceTypeId, List<int>> Records;
    public Dictionary<int, List<int>> CatMeshToAnimIdDataMap;

    public bool IsOpen => RdbController != null;

    private Settings _settings;
    public RdbController RdbController;

    protected override void OnInitialize()
    {
#if UNITY_EDITOR
        AssimpLibrary.Instance.LoadLibrary($"{Application.dataPath}\\Plugins\\assimp");
#endif
#if UNITY_STANDALONE_WIN
        AssimpLibrary.Instance.LoadLibrary($"{Application.dataPath}\\Plugins\\x86_64\\assimp");
#endif

        _settings = SettingsManager.Instance.Settings;
        CatMeshToAnimIdDataMap = JsonConvert.DeserializeObject<Dictionary<int, List<int>>>(Resources.Load<TextAsset>("CatMeshToMonsterData").text);
    }

    public void OpenDatabase()
    {
        if (RdbController != null)
            return;

        RdbController = new RdbController(_settings.AODirectory);
        Records = RdbController.RecordTypeToId.ToDictionary(x => (ResourceTypeId)x.Key, x => x.Value.Keys.ToList());
        if (Names == null)
        {
            Names = new Dictionary<ResourceTypeId, Dictionary<int, string>>();
            var types = RdbController.Get<InfoObject>(1).Types;

            foreach (var resource in new ResourceTypeId[] { ResourceTypeId.Texture, ResourceTypeId.CatMesh, ResourceTypeId.RdbMesh, ResourceTypeId.Anim })
            {
                foreach (var rdbMeshKey in RdbController.RecordTypeToId[(int)resource].Keys)
                {
                    if (!types[resource].TryGetValue(rdbMeshKey, out string name))
                        name = $"UnnamedRecord_{rdbMeshKey}";

                    if (!Names.ContainsKey(resource))
                        Names.Add(resource, new Dictionary<int, string>());

                    Names[resource].Add(rdbMeshKey, name);
                }
            }
        }
    }

    public void CloseDatabase()
    {
        if (RdbController == null)
            return;

        RdbController.Dispose();
    }

    public void ExportAbiffMesh(int meshId, string path)
    {

    }

    public void ExportDataModel(ResourceEntry resourceEntry, string path)
    {
        string texName = Names.TryGetValue(resourceEntry.ResourceType, out var resourceNames) && resourceNames.TryGetValue(resourceEntry.Id, out string rdbName) ? rdbName.Trim('\0') : $"UnnamedTex_{resourceEntry.Id}.png";

        switch (resourceEntry.ResourceType)
        {
            case ResourceTypeId.Texture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RdbController.Get<AOTexture>(resourceEntry.Id).JpgData);
                break;
            case ResourceTypeId.WallTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RdbController.Get<WallTexture>(resourceEntry.Id).JpgData);
                break;
            case ResourceTypeId.SkinTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RdbController.Get<SkinTexture>(resourceEntry.Id).JpgData);
                break;
            case ResourceTypeId.Icon:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RdbController.Get<IconTexture>(resourceEntry.Id).JpgData);
                break;
            case ResourceTypeId.GroundTexture:
                File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RdbController.Get<GroundTexture>(resourceEntry.Id).JpgData);
                break;
            default:
                break;
        }
    }

    public static Material LoadMaterial(AMaterial material)
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
          //  Debug.Log($"Loading diffuse texture {diffuseId}");
            Texture2D diffuseTex = new Texture2D(1, 1);

            var jpegData = RDBLoader.Instance.RdbController.Get<AOTexture>(diffuseId)?.JpgData;

            if (jpegData != null)
                diffuseTex.LoadImage(jpegData);

            aoMat.mainTexture = diffuseTex;
            aoMat.mainTexture.name = diffuseId.ToString();
        }

        if (material.HasNonTextureProperty("EmissionId"))
        {
            int emissionId = material.GetNonTextureProperty("EmissionId").GetIntegerValue();
          //  Debug.Log($"Loading emission texture {emissionId}");
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
            tex.LoadImage(RdbController.Get<Image>(type, texId).JpgData);

            mat = new Material(Shader.Find("Custom/AOShader"));
            mat.mainTexture = tex;
        }
        catch
        {
            return null;
        }

        return mat;
    }

    public Dictionary<int, string> GetAnimIdsForCir(int meshId)
    {
        if (!CatMeshToAnimIdDataMap.TryGetValue(meshId, out List<int> animIds))
            return new Dictionary<int, string>();

        Dictionary<int, string> animIdToName = new Dictionary<int, string>();

        foreach (var animId in animIds)
        {
            var anim = Names[ResourceTypeId.Anim].FirstOrDefault(x => x.Key == animId);

            if (animIdToName.ContainsKey(anim.Key))
                continue;

            if (anim.Key == 0)
                continue;

            animIdToName.Add(anim.Key, anim.Value);
        }

        return animIdToName.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
    }

    public Material LoadGroundMaterial(ResourceTypeId type, int texId)
    {
        Material mat;

        try
        {
            Debug.Log($"Loading ground texture {texId}");
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(RdbController.Get<GroundTexture>(type, texId).JpgData);

            mat = new Material(Shader.Find("Custom/AOShader"));
            mat.mainTexture = tex;
        }
        catch
        {
            return null;
        }

        return mat;
    }

    public static bool AddMaterialTexture(AMaterial mat, ref TextureSlot texture, bool onlySetFilePath)
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

    public void ExportMeshTexture(int texId, string path, out string texName)
    {
        texName = Instance.Names[ResourceTypeId.Texture].TryGetValue(texId, out string rdbName) ? rdbName.Trim('\0') : $"UnnamedTex_{texId}";
        File.WriteAllBytes($"{Path.GetDirectoryName(path)}\\{texName}", RDBLoader.Instance.RdbController.Get<AOTexture>(texId).JpgData);
    }
}
