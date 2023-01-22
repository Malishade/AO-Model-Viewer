using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static RDBLoader;
using ContextualMenuPlayer;
using ContextualMenuManager = ContextualMenuPlayer.ContextualMenuManager;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;

public class MainViewUxml
{
    private VisualElement _root;
    private ContextualMenuManager _menuManager;
    private DropdownMenu _fileDropdownMenu;
    private ListView _listView;
    private DropdownField _fileDropdownField;
    private VisualTreeAsset _listViewEntryTemplate;
    private ModelViewer _modelViewer;

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

    public MainViewUxml(VisualElement root, VisualTreeAsset listViewEntryTemplate, ModelViewer modelViewer)
    {
        _root = root;
        _menuManager = new ContextualMenuManager();

        _listView = root.Q<ListView>();
        _listViewEntryTemplate = listViewEntryTemplate;
        _modelViewer = modelViewer;

        root.AddManipulator(new ContextualMenuManipulator());

        InitTypeDropdown(root);
        InitFileMenu(root);

        EnumerateAllElements();
        FixScrollSpeed();
    }

    private void InitTypeDropdown(VisualElement root)
    {
        DropdownField dropdown = root.Q<DropdownField>("ResourceTypeSelector");

        dropdown.choices = _resourceTypeChoices.Keys.ToList();
        dropdown.index = 0;
        dropdown.RegisterValueChangedCallback(ResourceTypeChanged);
    }

    private void ResourceTypeChanged(ChangeEvent<string> e)
    {
        Debug.Log($"ResourceTypeChanged: {_resourceTypeChoices[e.newValue]}");
    }

    private void InitFileMenu(VisualElement root)
    {
        Button button = root.Q<Button>("FileMenu");

        button.RegisterCallback<ClickEvent>(ExpandFileMenu);

        _fileDropdownMenu = new();
        _fileDropdownMenu.AppendAction("Set AO Directory", OpenClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction("Load Resource Database", LoadClicked, (e) => Settings.AODirectory == string.Empty ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Exit", ExitClicked, DropdownMenuAction.AlwaysEnabled);
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

    private void OpenClicked(DropdownMenuAction action)
    {
        Debug.Log("Open!");
        Settings.AODirectory = "Derp";
    }

    private void LoadClicked(DropdownMenuAction action)
    {
        Debug.Log("Load!");
    }

    private void ExitClicked(DropdownMenuAction action)
    {
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

    private void OnEntryClicked(IEnumerable<object> obj)
    {
        var selectedEntry = _listView.selectedItem as RdbData;
        var newModel = _modelViewer.RdbLoader.CreateAbiffMesh(selectedEntry.MeshId);

       _modelViewer.UpdateModel(newModel);
    }

    private void EnumerateAllElements()
    {
        _listView.makeItem = () =>
        {
            var newListEntry = _listViewEntryTemplate.Instantiate();
            var newListEntryLogic = new ListViewEntry();
            newListEntry.userData = newListEntryLogic;
            newListEntryLogic.SetVisualElement(newListEntry);

            return newListEntry;
        };

        List<RdbData> rdbData = _modelViewer.RdbLoader.LoadNames();

        _listView.bindItem = (item, index) =>
        {
            (item.userData as ListViewEntry).Init(rdbData[index]);
        };

        _listView.horizontalScrollingEnabled = false;
        _listView.selectionType = SelectionType.Single;
        _listView.onSelectionChange += OnEntryClicked;
        _listView.itemsSource = rdbData;
    }
}
