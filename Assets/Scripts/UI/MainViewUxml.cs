using AODB;
using ContextualMenuPlayer;
using SFB;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ContextualMenuManager = ContextualMenuPlayer.ContextualMenuManager;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;

public class MainViewUxml
{
    private Settings _settings;
    private VisualElement _root;
    private ContextualMenuManager _menuManager;
    private DropdownField _resourceTypeDropdown;
    private DropdownMenu _fileDropdownMenu;
    private ListView _listView;
    private ModelViewer _modelViewer;
    private Camera _renderCamera;
    private Image _imageView;

    private Dictionary<string, ResourceType> _resourceTypeChoices = new()
    {
        { "Models (.abiff)", ResourceType.Models },
        { "Textures (.png)", ResourceType.Textures },
        { "Characters (.cir)", ResourceType.Characters },
    };

    public enum ResourceType
    {
        Models,
        Textures,
        Characters
    }

    public MainViewUxml(VisualElement root, ModelViewer modelViewer, Camera renderCamera)
    {
        _settings = SettingsManager.Instance.Settings;

        _root = root;
        _menuManager = new ContextualMenuManager();
        _modelViewer = modelViewer;
        _renderCamera = renderCamera;

        root.AddManipulator(new ContextualMenuManipulator());

        InitRenderViewport(root, renderCamera);
        InitListView(root);
        InitTypeDropdown(root);
        InitFileMenu(root);
        InitSearchBar(root);
        FixScrollSpeed();
    }

    private void ExportClicked(DropdownMenuAction obj)
    {
        //_modelViewer.CurrentModel
    }

    private void ImportClicked(DropdownMenuAction obj)
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Locate the FBX file.", null, "fbx", false, (paths) =>
        {
            if (paths.Length == 0)
                return;

            //paths.First();
        });
    }

    private void OnEntryClicked(IEnumerable<object> obj)
    {
        var selectedEntry = _listView.selectedItem as ListViewDataModel;

        if (selectedEntry.ResourceType == ResourceType.Models)
        {
            var newModel = RDBLoader.Instance.CreateAbiffMesh(selectedEntry.Id);
            _modelViewer.UpdateModel(newModel);
        }
    }

    private void InitSearchBar(VisualElement root)
    {
        var searchBar = _root.Q<TextField>("SearchBar");
        searchBar.RegisterCallback<ChangeEvent<string>>(TextUpdate);
    }

    private void TextUpdate(ChangeEvent<string> evt)
    {
        PopulateListView(_resourceTypeChoices["Models (.abiff)"], evt.newValue);
    }

    private void InitRenderViewport(VisualElement root, Camera renderCamera)
    {
        _imageView = root.Q<Image>("RightContainer");
        _imageView.RegisterCallback<GeometryChangedEvent>(WindowSizeChange);
    }

    private void WindowSizeChange(GeometryChangedEvent evt)
    {
        _renderCamera.targetTexture.Release();
        _renderCamera.targetTexture.width = (int)_imageView.layout.width;
        _renderCamera.targetTexture.height = (int)_imageView.layout.height;
        _renderCamera.targetTexture.Create();
    }

    private void InitListView(VisualElement root)
    {
        _listView = root.Q<ListView>();
        _listView.Q<VisualElement>("unity-dragger").style.minHeight = 14;
        _listView.fixedItemHeight = 18;
        _listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        _listView.selectionType = SelectionType.Single;
        _listView.onSelectionChange += OnEntryClicked;
        _listView.makeItem = () =>
        {
            Label newListEntry = new Label();
            newListEntry.userData = newListEntry;

            return newListEntry;
        };
    }

    private void InitTypeDropdown(VisualElement root)
    {
        _resourceTypeDropdown = root.Q<DropdownField>("ResourceTypeSelector");
        _resourceTypeDropdown.choices = _resourceTypeChoices.Keys.ToList();
        _resourceTypeDropdown.index = 0;
        _resourceTypeDropdown.RegisterValueChangedCallback(ResourceTypeChanged);
    }

    private void ResourceTypeChanged(ChangeEvent<string> e)
    {
        Debug.Log($"ResourceTypeChanged: {_resourceTypeChoices[e.newValue]}");
        PopulateListView(_resourceTypeChoices[e.newValue]);
    }

    private void InitFileMenu(VisualElement root)
    {
        Button button = root.Q<Button>("FileMenu");

        button.RegisterCallback<ClickEvent>(ExpandFileMenu);

        _fileDropdownMenu = new();

        _fileDropdownMenu.AppendAction("Set AO Directory", SetAODirectoryClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction("Load Resource Database", LoadClicked, LoadStatusCallback);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Close Database", CloseClicked, CloseStatusCallback);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction("Import", ImportClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction("Export", ExportClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Exit", ExitClicked, DropdownMenuAction.AlwaysEnabled);
    }

    private DropdownMenuAction.Status LoadStatusCallback(DropdownMenuAction e)
    {
        Debug.Log(_settings.AODirectory == null);
        return _settings.AODirectory == null || RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
    }

    private DropdownMenuAction.Status CloseStatusCallback(DropdownMenuAction e)
    {
        return RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
    }

    private void ExpandFileMenu(ClickEvent clickEvent)
    {
        _fileDropdownMenu.PrepareForDisplay(null);
        Button button = clickEvent.target as Button;

        ContextualMenu.MenuCreationContext creationContext = new()
        {
            position = new Vector2(button.transform.position.x, button.transform.position.y + button.localBound.height),
            menu = _fileDropdownMenu,
            root = _root,
            direction = RUIContextualMenuGrowDirection.SE
        };

        _menuManager.DisplayMenu(creationContext);
    }

    private void SetAODirectoryClicked(DropdownMenuAction action)
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Locate the Anarchy Online Folder", null, false, (paths) =>
        {
            if (paths.Length == 0)
                return;

            _settings.AODirectory = paths.First();
            SettingsManager.Instance.Save();
            Debug.Log($"Set AO Directory to {paths.First()}");
        });
    }

    private void LoadClicked(DropdownMenuAction action)
    {
        Debug.Log("Load!");
        RDBLoader.Instance.OpenDatabase();
        PopulateListView((ResourceType)_resourceTypeDropdown.index);
    }

    private void CloseClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.CloseDatabase();
        Debug.Log("Close!");
    }

    private void ExitClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.CloseDatabase();
        Debug.Log("Exit!");
    }

    private void FixScrollSpeed()
    {
        var scroller = _listView.Q<Scroller>();

        _listView.RegisterCallback<WheelEvent>(@event =>
        {
            scroller.value += @event.delta.y * 2500;
            @event.StopPropagation();
        });
    }

    private void PopulateListView(ResourceType resourceType, string query = "")
    {
        List<ListViewDataModel> listViewData = null;

        if (resourceType == ResourceType.Models)
        {
            listViewData = ListViewDataQuery(query);
        }
        else
        {
            return;
        }

        _listView.itemsSource = listViewData;

        _listView.bindItem = (item, index) =>
        {
            (item.userData as Label).text = listViewData[index].Name;
        };
    }

    private List<ListViewDataModel> ListViewDataQuery(string query)
    {
        List<ListViewDataModel> listViewData = new List<ListViewDataModel>();

        if (query == "")
        {
            foreach (var rdbKeyValue in RDBLoader.Instance.Names[(int)ResourceTypeId.RdbMesh])
                listViewData.Add(new ListViewDataModel { Id = (uint)rdbKeyValue.Key, Name = rdbKeyValue.Value });

        }
        else
        {
            foreach (var rdbKeyValue in RDBLoader.Instance.Names[(int)ResourceTypeId.RdbMesh])
            {
                if (!rdbKeyValue.Value.Contains(query))
                    continue;

                listViewData.Add(new ListViewDataModel { Id = (uint)rdbKeyValue.Key, Name = rdbKeyValue.Value });
            }
        }

        return listViewData;
    }

    public class ListViewDataModel
    {
        public string Name;
        public uint Id;
        public ResourceType ResourceType;
    }
}
