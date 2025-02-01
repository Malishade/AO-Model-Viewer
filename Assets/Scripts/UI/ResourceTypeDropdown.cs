using AODB.Common.RDBObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class ResourceTypeDropdown
{
    public ResourceTypeId ActiveResourceTypeId = ResourceTypeId.RdbMesh;
    public Action<ResourceTypeId> ResourceTypeChange;

    private Dictionary<string, ResourceTypeId> _resourceTypeChoices = new()
    {
        { "Models (.abiff)", ResourceTypeId.RdbMesh },
    //  { "Models2 (.abiff)", ResourceTypeId.PlayfieldDynels },
        { "Textures (.png)", ResourceTypeId.Texture },
        { "Icons (.png)", ResourceTypeId.Icon },
        { "Wall Textures (.png)", ResourceTypeId.WallTexture },
        { "Skin Textures (.png)", ResourceTypeId.SkinTexture },
        { "Ground Textures (.png)", ResourceTypeId.GroundTexture },
        { "Characters (.cir)", ResourceTypeId.CatMesh },
    //  { "Animations (.ani)", ResourceTypeId.Anim },
    };

    private DropdownField _resourceTypeDropdown;

    public ResourceTypeDropdown(DropdownField resourceTypeDropdown)
    {
        _resourceTypeDropdown = resourceTypeDropdown;
        _resourceTypeDropdown.choices = _resourceTypeChoices.Keys.ToList();
        _resourceTypeDropdown.index = 0;
        _resourceTypeDropdown.RegisterValueChangedCallback(ResourceTypeChanged);
    }

    private void ResourceTypeChanged(ChangeEvent<string> e)
    {
        ActiveResourceTypeId = _resourceTypeChoices[e.newValue];
        ResourceTypeChange?.Invoke(_resourceTypeChoices[e.newValue]);
    }
}

public class ResourceEntry
{
    public int Id;
    public string Name;
    public ResourceTypeId ResourceType;

    public ResourceEntry(int id, string name, ResourceTypeId resourceType)
    {
        Id = id;
        Name = name;
        ResourceType = resourceType;
    }
}