using AODB.Common.RDBObjects;
using SFB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MainViewUxml
{
    private readonly TableView _resourceTableView;
    private readonly AnimTableView _animTableView;
    private readonly ModelViewer _modelViewer;
    private readonly ResourceTypeDropdown _resourceDropdown;
    private readonly SearchBar _searchBar;
    private readonly ModelInspectorView _modelInspectorView;
    private readonly InfoBox _infoBox;
    private readonly FileMenu _fileMenu;
    private VisualElement _modelViewerContainer;

    public MainViewUxml(VisualElement root, ModelViewer modelViewer)
    {
        _modelViewer = modelViewer;
        
        _resourceDropdown = new ResourceTypeDropdown(root.Q<DropdownField>("ResourceTypeSelector"));
        _resourceDropdown.ResourceTypeChange += OnResourceTypeChange;

        _resourceTableView = new TableView(root.Q<MultiColumnListView>("TableView"));
        _resourceTableView.EntrySelected += OnEntrySelection;
        _resourceTableView.MouseEnter += OnMouseEnterView;

        _animTableView = new AnimTableView(root.Q<MultiColumnListView>("AnimTableView"), root.Q<Foldout>("AnimFoldout"));
        _animTableView.EntrySelected += OnAnimEntrySelected;
        _animTableView.MouseEnter += OnMouseEnterView;
        _animTableView.Hide();

        _searchBar = new SearchBar(root.Q<TextField>("SearchText"), root.Q<Button>("ClearTextButton"), root.Q<Label>("ResultsLabel"));
        _searchBar.OnTextUpdate += OnSearchTextUpdate;

        var matRadioGroup = root.Q<RadioButtonGroup>("MaterialButtonGroup");

        _modelInspectorView = new ModelInspectorView(root.Q<Foldout>("ModelInspector"), root.Q<RadioButtonGroup>("MaterialButtonGroup"), root.Q<Label>("VerticesCount"), root.Q<Label>("EdgesCount"), root.Q<Toggle>("ShowSkeleton"));
        _modelInspectorView.MaterialChange += OnMaterialChange;
        _modelInspectorView.ShowSkeleton += OnShowSkeletonClick;
        _modelInspectorView.Hide();

        _fileMenu = new FileMenu(root, root.Q<Button>("FileMenu"));
        _fileMenu.LoadClick += OnLoadClick;
        _fileMenu.ExportClick += OnExportClick;
        _fileMenu.ExportAllClick += OnExportAllClick;
        _fileMenu.InfoClick += OnInfoClick;

        _infoBox = new InfoBox(root.Q<VisualElement>("InfoBox"), root.Q<Button>("InfoBoxClose"));
        _infoBox.Hide();

        InitViewContainer(root);
    }

    private void OnShowSkeletonClick(bool value)
    {
        LineRenderer[] lineRenderers = _modelViewer.ModelPreview.GameObjectRoot.GetComponentsInChildren<LineRenderer>();
     
        if (lineRenderers == null)
            return;

        foreach (var lineRender in lineRenderers)
            lineRender.enabled = value;
    }

    private void OnInfoClick()
    {
        _infoBox.Show();
    }

    private void OnExportClick()
    {
        if (_resourceTableView.SelectedEntry == null)
            return;

        var exportName = SetResourceExportName(_resourceTableView.SelectedEntry);

        StandaloneFileBrowser.SaveFilePanelAsync("Export Resource", null, Path.GetFileNameWithoutExtension(exportName), Path.GetExtension(exportName).TrimStart('.'), (path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            ResourceExport(_resourceTableView.SelectedEntry, path);
        });
    }

    private void OnExportAllClick()
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Export Resource", null, false, (pathArray) =>
        {
            string path = pathArray[0];

            if (string.IsNullOrEmpty(path))
                return;

            foreach (var entry in _resourceTableView.Resources)
            {
                ResourceExport(entry, $"{path}\\{SetResourceExportName(entry)}");
            }
        });
    }

    private void OnLoadClick()
    {
        RDBLoader.Instance.OpenDatabase();
        OnResourceTypeChange(ResourceTypeId.RdbMesh);
        _searchBar.Enable();
    }

    private string SetResourceExportName(ResourceEntry entry)
    {
        string defaultName = _resourceTableView.SelectedEntry.Name.Trim('\0');

        switch (entry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                defaultName = defaultName.Replace(".abiff", ".fbx");
                break;
            case ResourceTypeId.CatMesh:
                defaultName = defaultName.Replace(".cir", ".fbx");
                break;
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.GroundTexture:
                defaultName += ".png";
                break;
        }

        return defaultName;
    }

    private void OnMaterialChange(MaterialTypeId materialId)
    {
        _modelViewer.UpdateMaterial(materialId);
    }

    private void OnAnimEntrySelected(ResourceEntry entry)
    {
        _modelViewer.ModelPreview.PlayAnimation(entry.Name);
    }

    private void OnMouseEnterView(bool entered)
    {
        _modelViewer.PivotController.DisableMouseInput = entered;
    }

    private void OnSearchTextUpdate(string searchText)
    {
        _resourceTableView.Update(searchText, out int totalAmount);
        _searchBar.UpdateResults(totalAmount);
    }

    private void OnEntrySelection(ResourceEntry entry)
    {
        if (entry == null)
            return;

        switch (entry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                AbiffLoader abiffLoader = new AbiffLoader();
                var abiffMesh = abiffLoader.CreateAbiffMesh(entry.ResourceType, entry.Id);
                _modelViewer.UpdateMesh(abiffMesh);
                _modelViewer.UpdateMaterial(_modelInspectorView.ActiveMaterial);
                _modelInspectorView.UpdateData(_modelViewer.ModelPreview.VerticesCount, _modelViewer.ModelPreview.TrianglesCount);
                _animTableView.Hide();
                break;
            case ResourceTypeId.CatMesh:
                CirLoader cirLoader = new CirLoader();
                var cirMesh = cirLoader.CreateCirMesh(entry.Id, out List<ResourceEntry> animResources);
                _modelViewer.UpdateMesh(cirMesh);
                _modelViewer.UpdateMaterial(_modelInspectorView.ActiveMaterial);
                _modelInspectorView.UpdateData(_modelViewer.ModelPreview.VerticesCount, _modelViewer.ModelPreview.TrianglesCount);
                _animTableView.Update(animResources);
                _animTableView.Show();
                break;
            case ResourceTypeId.Texture:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.Icon:
                var rdbMat = RDBLoader.Instance.LoadMaterialOld(entry.ResourceType, entry.Id);
                _modelViewer.InitUpdateRdbTexture(rdbMat);
                _animTableView.Hide();
                break;
            case ResourceTypeId.GroundTexture:
                var groundMat = RDBLoader.Instance.LoadGroundMaterial(entry.ResourceType, entry.Id);
                _modelViewer.InitUpdateRdbTexture(groundMat);
                _animTableView.Hide();
                break;
        }
    }

    private void OnResourceTypeChange(ResourceTypeId id)
    {
        if (RDBLoader.Instance.Names == null)
            return;

        if (!RDBLoader.Instance.Names.TryGetValue(id, out var names))
            names = RDBLoader.Instance.Records[id].ToDictionary(x => x, x => $"UnnamedRecord_{x}");

        var resourceEntries = names.Select(x => new ResourceEntry(x.Key, x.Value, id)).ToList();

        _modelViewer.DestroyCurrentModel();
        _resourceTableView.SetResources(resourceEntries);
        _searchBar.ClearText();
        _searchBar.UpdateResults(resourceEntries.Count);

        switch (id)
        {
            case ResourceTypeId.RdbMesh:
                _modelViewer.PivotController.DisableMouseInput = false;
                _modelInspectorView.Show();
                _animTableView.Hide();
                _modelInspectorView.HideSkeletonToggle();
                break;
            case ResourceTypeId.CatMesh:
                _modelViewer.PivotController.DisableMouseInput = false;
                _modelInspectorView.Show();
                _animTableView.Show();
                _modelInspectorView.ShowSkeletonToggle();
                break;
            case ResourceTypeId.Texture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.GroundTexture:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.Icon:
                _modelViewer.PivotController.DisableMouseInput = true;
                _modelInspectorView.Hide();
                _animTableView.Hide();
                break;
        }
    }

    private void InitViewContainer(VisualElement root)
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
    private void ResourceExport(ResourceEntry resourceEntry, string path)
    {
        switch (resourceEntry.ResourceType)
        {
            case ResourceTypeId.RdbMesh:
                AbiffLoader abiffLoader = new AbiffLoader();
                abiffLoader.ExportMesh(resourceEntry.Id, path);
                break;
            case ResourceTypeId.CatMesh:
                CirLoader cirLoader = new CirLoader();
                cirLoader.ExportMesh(resourceEntry.Id, path);
                break;
            case ResourceTypeId.Texture:
            case ResourceTypeId.SkinTexture:
            case ResourceTypeId.WallTexture:
            case ResourceTypeId.Icon:
            case ResourceTypeId.GroundTexture:
                RDBLoader.Instance.ExportDataModel(resourceEntry, path);
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
}