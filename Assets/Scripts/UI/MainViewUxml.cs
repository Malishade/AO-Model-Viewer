using AODB;
using AODB.RDBObjects;
using Assimp;
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
    private StatisticsDataModel _statisticsDataModel;
    private MeshData _meshData;
    private Label _resultsLabel;
    private TextField _searchBarTextField;
    private Button _searchBarClearButton;
    private ResourceTypeId _activeResourceTypeId = ResourceTypeId.RdbMesh;
    private MaterialTypeId _activeMatTypeId;
    private Foldout _modelInspectorFoldout;
    private Dictionary<string, ResourceTypeId> _resourceTypeChoices = new()
    {
        { "Models (.abiff)", ResourceTypeId.RdbMesh },
        { "Textures (.png)", ResourceTypeId.Texture },
        //{ "Characters (.cir)", ResourceTypeId.RdbMesh },
    };

    public MainViewUxml(VisualElement root, ModelViewer modelViewer)
    {
        _settings = SettingsManager.Instance.Settings;

        _root = root;
        _menuManager = new ContextualMenuManager();
        _modelViewer = modelViewer;

        root.AddManipulator(new ContextualMenuManipulator());

        InitListView(root);
        InitTypeDropdown(root);
        InitFileMenu(root);
        InitSearchBar(root);
        InitModelInspector(root);

        FixScrollSpeed();

        SetResolution();
    }

    private void SetResolution()
    {
        Screen.SetResolution(1600, 720, false);
    }

    private void ExportClicked(DropdownMenuAction obj)
    {
        if (_listView.selectedItem == null)
            return;

        ListViewDataModel selectedEntry = _listView.selectedItem as ListViewDataModel;

        string defaultName = selectedEntry.Name.Replace(".abiff", ".fbx");

        StandaloneFileBrowser.SaveFilePanelAsync("Export Resource", null, defaultName, "fbx", (path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (selectedEntry.ResourceType == ResourceTypeId.RdbMesh)
                RDBLoader.Instance.ExportMesh(((ListViewDataModel)_listView.selectedItem).Id, path);
        });
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
        ListViewDataModel selectedEntry = _listView.selectedItem as ListViewDataModel;

        if (selectedEntry == null)
            return;

        switch (selectedEntry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                var rdbMesh = RDBLoader.Instance.CreateAbiffMesh(selectedEntry.Id);
                _modelViewer.InitUpdateRdbMesh(rdbMesh);
                _modelViewer.CurrentModelMeshes.GetMeshData(out _meshData);
                _statisticsDataModel.Vertices.text = _meshData.VerticesCount.ToString();
                _statisticsDataModel.Tris.text = _meshData.TrianglesCount.ToString();
                MaterialChangeAction(_activeMatTypeId);
                break;
            case ResourceTypeId.Texture:
                var rdbMat = RDBLoader.Instance.LoadMaterial(selectedEntry.Id);
                _modelViewer.InitUpdateTexture(rdbMat);
                break;
        }

    }

    private void InitModelInspector(VisualElement root)
    {
        _modelInspectorFoldout = root.Q<Foldout>("ModelInspector");
        var _materialRadioButtonGroup = root.Q<RadioButtonGroup>("MaterialButtonGroup");

        _activeMatTypeId = 0;
        _statisticsDataModel = new StatisticsDataModel
        {
            Vertices = root.Q<Label>("VerticesCount"),
            Tris = root.Q<Label>("EdgesCount"),
        };

        _materialRadioButtonGroup.RegisterValueChangedCallback(MaterialTypeChangeEvent);
    }

    private void MaterialTypeChangeEvent(ChangeEvent<int> evt)
    {
        MaterialChangeAction((MaterialTypeId)evt.newValue);
        _activeMatTypeId = (MaterialTypeId)evt.newValue;
    }

    private void MaterialChangeAction(MaterialTypeId index)
    {
        if (_modelViewer.CurrentModelRoot == null)
            return;

        var currMat = _modelViewer.RenderMaterials.RendererMaterials.First(x => x.Index == index).Material;

        foreach (var s in _meshData.MeshRendererData)
        {
            s.Key.material = currMat;
            s.Key.material.SetTexture("_MainTex", s.Value);
        }
    }

    private void InitSearchBar(VisualElement root)
    {
        _searchBarTextField = root.Q<TextField>("SearchBar");
        _searchBarTextField.SetEnabled(false);
        _searchBarTextField.RegisterCallback<ChangeEvent<string>>(TextUpdate);

        _searchBarClearButton = root.Q<Button>("SearchBarClear");
        _searchBarClearButton.SetEnabled(false);
        _searchBarClearButton.RegisterCallback<ClickEvent>(ClearSearchBar);
    }

    private void ClearSearchBar(ClickEvent evt) => _searchBarTextField.value = "";

    private void TextUpdate(ChangeEvent<string> evt)
    {
        PopulateListView(evt.newValue);
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

        _resultsLabel = root.Q<Label>("ResultsLabel");
    }

    private void OnMouseLeave(PointerLeaveEvent evt)
    {
        _modelViewer.PivotController.DisableMouseInput = false;
    }

    private void OnMouseEnter(PointerEnterEvent evt)
    {
        _modelViewer.PivotController.DisableMouseInput = true;
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

        _activeResourceTypeId = _resourceTypeChoices[e.newValue];
        _modelViewer.DestroyCurrentModel();

        PopulateListView();

        switch (_activeResourceTypeId)
        {
            case ResourceTypeId.RdbMesh:
                _listView.RegisterCallback<PointerEnterEvent>(OnMouseEnter);
                _listView.RegisterCallback<PointerLeaveEvent>(OnMouseLeave);
                _modelViewer.PivotController.RenderCamera.orthographic = false;
                _modelInspectorFoldout.style.display = DisplayStyle.Flex;
                break;
            case ResourceTypeId.Texture:
                _listView.UnregisterCallback<PointerEnterEvent>(OnMouseEnter);
                _listView.UnregisterCallback<PointerLeaveEvent>(OnMouseLeave);
                _modelViewer.PivotController.DisableMouseInput = true;
                _modelViewer.PivotController.RenderCamera.orthographic = true;
                _modelInspectorFoldout.style.display = DisplayStyle.None;
                break;
        }
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

    private DropdownMenuAction.Status LoadStatusCallback(DropdownMenuAction e) => _settings.AODirectory == null || RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;

    private DropdownMenuAction.Status CloseStatusCallback(DropdownMenuAction e) => RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

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
        PopulateListView();
        _searchBarTextField.SetEnabled(true);
        _searchBarClearButton.SetEnabled(true);
    }

    private void CloseClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.CloseDatabase();
        Debug.Log("Close!");
    }

    private void ExitClicked(DropdownMenuAction action)
    {
        Application.Quit();
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

    private void PopulateListView(string query = "")
    {
        if (!RDBLoader.Instance.IsOpen)
            return;

        List<ListViewDataModel> listViewData = null;

        _listView.ClearSelection();

        listViewData = ListViewDataQuery(query);

        _listView.itemsSource = listViewData;
        _listView.bindItem = (item, index) =>
        {
            (item.userData as Label).text = listViewData[index].Name;
        };

        _resultsLabel.text = listViewData.Count().ToString();
    }

    private List<ListViewDataModel> ListViewDataQuery(string query)
    {
        List<ListViewDataModel> listViewData = new List<ListViewDataModel>();

        if (query == "")
        {
            foreach (var rdbKeyValue in RDBLoader.Instance.Names[(int)_activeResourceTypeId])
                listViewData.Add(new ListViewDataModel { ResourceType=_activeResourceTypeId, Id = rdbKeyValue.Key, Name = rdbKeyValue.Value });

        }
        else
        {
            foreach (var rdbKeyValue in RDBLoader.Instance.Names[(int)_activeResourceTypeId])
            {
                if (!rdbKeyValue.Value.Contains(query))
                    continue;

                listViewData.Add(new ListViewDataModel { ResourceType = _activeResourceTypeId, Id = rdbKeyValue.Key, Name = rdbKeyValue.Value });
            }
        }

        return listViewData;
    }

    public class ListViewDataModel
    {
        public string Name;
        public int Id;
        public ResourceTypeId ResourceType;
    }

    public class StatisticsDataModel
    {
        public Label Vertices;
        public Label Tris;
    }

    public class MeshData
    {
        public Dictionary<MeshRenderer, Texture> MeshRendererData;
        public int VerticesCount;
        public int TrianglesCount;
    }
}
