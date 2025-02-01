using AODB.Common.RDBObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TableView
{
    public Action<ResourceEntry> EntrySelected;
    public Action<bool> MouseEnter;
    public ResourceEntry SelectedEntry => McListView.itemsSource[McListView.selectedIndex] as ResourceEntry;

    protected MultiColumnListView McListView;

    private List<ResourceEntry> _currentResources;
    public ReadOnlyCollection<ResourceEntry> Resources => _currentResources.AsReadOnly();


    public TableView(MultiColumnListView multiColumnListView)
    {
        McListView = multiColumnListView;
        McListView.RegisterCallback<MouseEnterEvent>(x => MouseEnterAction(true));
        McListView.RegisterCallback<MouseLeaveEvent>(x => MouseEnterAction(false));

        McListView.columnSortingChanged += ApplySorting;
        McListView.selectedIndicesChanged += (x) => EntrySelected?.Invoke(McListView.itemsSource[x.FirstOrDefault()] as ResourceEntry);
    }

    private void MouseEnterAction(bool entered)
    {
        MouseEnter?.Invoke(entered);
    }

    public void SetResources(List<ResourceEntry> resources)
    {
        _currentResources = resources;
        Refresh(_currentResources);
    }

    public void Update(string searchText, out int totalAmount)
    {
        McListView.ClearSelection();
        var itemList = _currentResources.Where(x => x.Id.ToString().Contains(searchText) || x.Name.Contains(searchText)).OrderByDescending(item => item.Id).ToList();
        totalAmount = itemList.Count;
        Refresh(itemList);
    }

    protected void Refresh(List<ResourceEntry> resourceEntries)
    {
        McListView.itemsSource = resourceEntries;

        var cols = McListView.columns;
        cols["Id"].makeCell = () => new Label();
        cols["Name"].makeCell = () => new Label();

        cols["Id"].bindCell = (VisualElement e, int index) =>
        {
            if (index >= resourceEntries.Count)
                return;

            Label label = (Label)e;
            label.text = resourceEntries[index].Id.ToString();
            label.style.height = 21;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
        };

        cols["Name"].bindCell = (VisualElement e, int index) =>
        {
            if (index >= resourceEntries.Count)
                return;

            Label label = (Label)e;
            label.text = resourceEntries[index].Name;
            label.style.height = 21;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
        };

        McListView.RefreshItems();
    }

    private void ApplySorting()
    {
        var column = McListView.sortedColumns.FirstOrDefault();

        if (column == null)
            return;

        if (McListView.itemsSource == null)
            return;

        if (column.columnName == "Id")
        {
            if (column.direction == SortDirection.Descending)
            {
                List<ResourceEntry> itemList = McListView.itemsSource as List<ResourceEntry>;
                itemList = itemList.OrderByDescending(item => item.Id).ToList();
                Refresh(itemList);
            }
            else if (column.direction == SortDirection.Ascending)
            {
                List<ResourceEntry> itemList = McListView.itemsSource as List<ResourceEntry>;
                itemList = itemList.OrderBy(item => item.Id).ToList();
                Refresh(itemList);
            }
        }

        if (column.columnName == "Name")
        {
            if (column.direction == SortDirection.Descending)
            {
                List<ResourceEntry> itemList = McListView.itemsSource as List<ResourceEntry>;
                itemList = itemList.OrderByDescending(item => item.Name).ToList();
                Refresh(itemList);
            }
            else if (column.direction == SortDirection.Ascending)
            {
                List<ResourceEntry> itemList = McListView.itemsSource as List<ResourceEntry>;
                itemList = itemList.OrderBy(item => item.Name).ToList();
                Refresh(itemList);
            }
        }
    }
}
