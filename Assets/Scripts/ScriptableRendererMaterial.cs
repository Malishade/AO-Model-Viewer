using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New RendererMaterials", menuName = "ScriptableObjects/RendererMaterials", order = 1)]
public class ScriptableRendererMaterial : ScriptableObject
{
    public RendererMaterial[] RendererMaterials;
}

[Serializable]
public class RendererMaterial
{
    public MaterialTypeId Index;
    public Material Material;
}

public enum MaterialTypeId
{
    Color,
    Unlit,
    Wireframe,
    Matcap1,
    Matcap2,
    Matcap3,
}