using AODB;
using AODB.Common.RDBObjects;
using Assimp;
using ContextualMenuPlayer;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
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
    private Label _resultsLabel;
    private TextField _searchBarTextField;
    private Button _searchBarClearButton;
    private OrderName _currentName;
    private OrderType _currentType;
    private string _currentQuery = "";

    private ResourceTypeId _activeResourceTypeId = ResourceTypeId.RdbMesh;
    private MaterialTypeId _activeMatTypeId;
    private Foldout _modelInspectorFoldout;
    private Dictionary<string, ResourceTypeId> _resourceTypeChoices = new()
    {
        { "Models (.abiff)", ResourceTypeId.RdbMesh },
        //{ "Models2 (.abiff)", (ResourceTypeId)1010026 },
        { "Textures (.png)", ResourceTypeId.Texture },
        { "Icons (.png)", ResourceTypeId.Icon },
        { "Wall Textures (.png)", ResourceTypeId.WallTexture },
        { "Skin Textures (.png)", ResourceTypeId.SkinTexture },
        { "Ground Textures (.png)", ResourceTypeId.GroundTexture },
        //{ "Characters (.cir)", ResourceTypeId.RdbMesh },
    };
    private VisualElement _modelViewerContainer;

    public MainViewUxml(VisualElement root, ModelViewer modelViewer)
    {
        _settings = SettingsManager.Instance.Settings;
        _root = root;
        _menuManager = new ContextualMenuManager();
        _modelViewer = modelViewer;

        root.AddManipulator(new ContextualMenuManipulator());

        InitOrderByButtons(root);
        InitRightContainer(root);
        InitListView(root);
        InitTypeDropdown(root);
        InitFileMenu(root);
        InitSearchBar(root);
        InitModelInspector(root);
        FixScrollSpeed();
    }

    private void InitOrderByButtons(VisualElement root)
    {
        root.Q<Button>("OrderByIdButton").RegisterCallback<ClickEvent>(OrderByIdCallback);
        root.Q<Button>("OrderByNameButton").RegisterCallback<ClickEvent>(OrderByNameCallback);
    }

    private void ExportClicked(DropdownMenuAction obj)
    {
        if (_listView.selectedItem == null)
            return;

        ListViewDataModel selectedEntry = _listView.selectedItem as ListViewDataModel;

        string defaultName = selectedEntry.Name.Trim('\0');


        switch (selectedEntry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                defaultName = defaultName.Replace(".abiff", ".fbx");
                break;
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.GroundTexture:
                defaultName += ".png";
                break;
        }

        StandaloneFileBrowser.SaveFilePanelAsync("Export Resource", null, Path.GetFileNameWithoutExtension(defaultName), Path.GetExtension(defaultName).TrimStart('.'), (path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            ResourceExport(selectedEntry, path);
        });
    }

    private void ExportAll(DropdownMenuAction obj)
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Export Resource", null, false, (pathArray) =>
        {
            string path = pathArray[0];

            if (string.IsNullOrEmpty(path))
                return;

            foreach (var entry in _listView.itemsSource)
            {
                var item = entry as ListViewDataModel;  
                string defaultName = item.Name.Trim('\0');

                if (item.ResourceType == ResourceTypeId.RdbMesh)
                    defaultName = defaultName.Replace(".abiff", ".fbx");

                ResourceExport(item, $"{path}\\{defaultName}");
            }
        });
    }

    private void ResourceExport(ListViewDataModel dataModel, string path)
    {
        switch (dataModel.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                RDBLoader.Instance.ExportMesh(dataModel.Id, path);
                break;
            case ResourceTypeId.Texture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.Icon:
            case ResourceTypeId.GroundTexture:
                RDBLoader.Instance.ExportDataModel(dataModel, path);
                break;
            default:
                break;
        }
    }

    //private void ImportClicked(DropdownMenuAction obj)
    //{
    //    StandaloneFileBrowser.OpenFilePanelAsync("Locate the FBX file.", null, "fbx", false, (paths) =>
    //    {
    //        if (paths.Length == 0)
    //            return;

    //        //paths.First();
    //    });
    //}

    private void OnEntryClicked(IEnumerable<object> obj)
    {
        ListViewDataModel selectedEntry = _listView.selectedItem as ListViewDataModel;

        if (selectedEntry == null)
            return;

        switch (selectedEntry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
            case (ResourceTypeId)1010026:
                var rdbMesh = RDBLoader.Instance.CreateAbiffMesh(selectedEntry.ResourceType, selectedEntry.Id);
                _modelViewer.InitUpdateRdbMesh(rdbMesh);
                _statisticsDataModel.Vertices.text = _modelViewer.CurrentModelData.VerticesCount.ToString();
                _statisticsDataModel.Tris.text = _modelViewer.CurrentModelData.TrianglesCount.ToString();
                MaterialChangeAction(_activeMatTypeId);
                break;
            case ResourceTypeId.Texture:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.Icon:
                var rdbMat = RDBLoader.Instance.LoadMaterialOld(selectedEntry.ResourceType, selectedEntry.Id);
                _modelViewer.InitUpdateRdbTexture(rdbMat);
                break;
            case ResourceTypeId.GroundTexture:
                var groundMat = RDBLoader.Instance.LoadGroundMaterial(selectedEntry.ResourceType, selectedEntry.Id);
                _modelViewer.InitUpdateRdbTexture(groundMat);
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
        if (_modelViewer.CurrentModelData.PivotRoot == null)
            return;

        _modelViewer.UpdateMaterial(index);
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
        _currentQuery = evt.newValue;
        PopulateListView(_currentQuery);
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
            VisualElement newListEntry = new VisualElement();

            Label idLabel = new Label();
            idLabel.style.minWidth = 60;
            Label nameLabel = new Label();

            newListEntry.Add(idLabel);
            newListEntry.Add(nameLabel);

            newListEntry.style.flexDirection = FlexDirection.Row;
            newListEntry.userData = newListEntry;

            return newListEntry;
        };
        RegisterListViewPointerEvents();
        _resultsLabel = root.Q<Label>("ResultsLabel");
    }

    private void ListViewMouseEnter(MouseEnterEvent evt) => _modelViewer.PivotController.DisableMouseInput = true;

    private void ListViewMouseLeave(MouseLeaveEvent evt) => _modelViewer.PivotController.DisableMouseInput = false;

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
                RegisterListViewPointerEvents();
                _modelInspectorFoldout.style.display = DisplayStyle.Flex;
                break;
            case ResourceTypeId.Texture:
                UnRegisterListViewPointerEvents();
                _modelViewer.PivotController.DisableMouseInput = true;
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
        //_fileDropdownMenu.AppendAction("Import", ImportClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction("Export", ExportClicked, IsRdbOpen);
        _fileDropdownMenu.AppendAction("Export All", ExportAll, IsTextureResourceTypeOpen);

        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Exit", ExitClicked, DropdownMenuAction.AlwaysEnabled);
    }

    private DropdownMenuAction.Status IsTextureResourceTypeOpen(DropdownMenuAction arg)
    {
        if (!RDBLoader.Instance.IsOpen)
            return DropdownMenuAction.Status.Disabled;

        switch (((ListViewDataModel)_listView.itemsSource[0]).ResourceType)
        {
            case ResourceTypeId.Texture:
            case ResourceTypeId.Icon:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.GroundTexture:
                return DropdownMenuAction.Status.Normal;
            default:
                return DropdownMenuAction.Status.Disabled;
        }
    }

    private DropdownMenuAction.Status IsRdbOpen(DropdownMenuAction e) => RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

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

    private void InitRightContainer(VisualElement root)
    {
        var rightContainer = root.Q<VisualElement>("RightContainer");
        rightContainer.RegisterCallback<GeometryChangedEvent>(GeometryChange);

        _modelViewerContainer = root.Q<VisualElement>("ModelViewerContainer");
    }

    private void GeometryChange(GeometryChangedEvent evt)
    {
        var container = (VisualElement)evt.target;
        Vector2 visibleArea = new Vector2(container.contentRect.width, container.contentRect.height);

        if (visibleArea.x == 0 || visibleArea.y == 0)
            return;

        Vector2 aspectRatioArea = visibleArea.x / visibleArea.y < 16f / 9f ?
            new Vector2(Mathf.FloorToInt(visibleArea.y * 16f / 9f), Mathf.FloorToInt(visibleArea.y)) :
            new Vector2(Mathf.FloorToInt(visibleArea.x), Mathf.FloorToInt(visibleArea.x * 9f / 16f));

        _modelViewerContainer.style.height = aspectRatioArea.y;
        _modelViewerContainer.style.width = aspectRatioArea.x;

        var moveVector = new Vector3(visibleArea.x / aspectRatioArea.x, visibleArea.y / aspectRatioArea.y, 0);

        _modelViewer.UpdateModelViewer(moveVector);
    }

    private void SetAODirectoryClicked(DropdownMenuAction action)
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Locate the Anarchy Online Folder", null, false, (paths) =>
        {
            if (paths.Length == 0)
                return;

            _settings.AODirectory = paths.First();
            SettingsManager.Instance.Save();
        });
    }

    private void LoadClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.OpenDatabase();
        PopulateListView();
        _searchBarTextField.SetEnabled(true);
        _searchBarClearButton.SetEnabled(true);
    }

    private void CloseClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.CloseDatabase();
    }

    private void ExitClicked(DropdownMenuAction action)
    {
        Application.Quit();
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
            var root = (VisualElement)item.userData;
            (root.ElementAt(0) as Label).text = listViewData[index].Id.ToString();
            (root.ElementAt(1) as Label).text = listViewData[index].Name.ToString();
        };

        _resultsLabel.text = listViewData.Count().ToString();
    }

    private List<ListViewDataModel> ListViewDataQuery(string query)
    {
        List<ListViewDataModel> listViewData = new List<ListViewDataModel>();

        Dictionary<int, string> names;

        if (!RDBLoader.Instance.Names.TryGetValue(_activeResourceTypeId, out names))
        {
            names = RDBLoader.Instance.Records[_activeResourceTypeId].ToDictionary(x => x, x => $"UnnamedRecord_{x}");
        }

        switch (_currentName)
        {
            case OrderName.Name:
                if (_currentType == OrderType.Ascending)
                    names = names.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                else if (_currentType == OrderType.Descending)
                    names = names.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                break;
            case OrderName.Id:
                if (_currentType == OrderType.Ascending)
                {
                    // this is by default
                }
                else if (_currentType == OrderType.Descending)
                {
                    names = names.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                }
                break;
        }



        if (query == "")
        {
            foreach (var rdbKeyValue in names)
                listViewData.Add(new ListViewDataModel { ResourceType = _activeResourceTypeId, Id = rdbKeyValue.Key, Name = rdbKeyValue.Value });

        }
        else
        {
            foreach (var rdbKeyValue in names)
            {
                if (!rdbKeyValue.Value.Contains(query))
                    continue;

                listViewData.Add(new ListViewDataModel { ResourceType = _activeResourceTypeId, Id = rdbKeyValue.Key, Name = rdbKeyValue.Value });
            }
        }

        return listViewData;
    }

    private void OrderByIdCallback(ClickEvent evt)
    {
        _currentType = _currentName == OrderName.Name ? OrderType.Ascending : _currentType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
        _currentName = OrderName.Id;

        PopulateListView(_currentQuery);
    }

    private void OrderByNameCallback(ClickEvent evt)
    {
        _currentType = _currentName == OrderName.Id ? OrderType.Ascending : _currentType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
        _currentName = OrderName.Name;

        PopulateListView(_currentQuery);
    }

    internal enum OrderName
    {
        Id,
        Name,
    }

    internal enum OrderType
    {
        Ascending,
        Descending
    }

    private void RegisterListViewPointerEvents()
    {
        _listView.RegisterCallback<MouseEnterEvent>(ListViewMouseEnter);
        _listView.RegisterCallback<MouseLeaveEvent>(ListViewMouseLeave);
    }

    private void UnRegisterListViewPointerEvents()
    {
        _listView.UnregisterCallback<MouseEnterEvent>(ListViewMouseEnter);
        _listView.UnregisterCallback<MouseLeaveEvent>(ListViewMouseLeave);
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
        public int VerticesCount;
        public int TrianglesCount;
    }
}
