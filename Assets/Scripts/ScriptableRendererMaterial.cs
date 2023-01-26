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
    public int Index;
    public Material Material;
}