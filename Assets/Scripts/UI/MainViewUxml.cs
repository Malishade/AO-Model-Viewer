using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RDBLoader;

public class MainViewUxml
{
    private ListView _listView;
    private VisualTreeAsset _listViewEntryTemplate;
    private ModelViewer _modelViewer;

    public MainViewUxml(VisualElement root, VisualTreeAsset listViewEntryTemplate, ModelViewer modelViewer)
    {
        _listView = root.Q<ListView>();
        _listViewEntryTemplate = listViewEntryTemplate;
        _modelViewer = modelViewer;

        EnumerateAllElements();
        FixScrollSpeed();
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
