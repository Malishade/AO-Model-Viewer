using System.Collections.Generic;
using System.Drawing;
using UnityEngine.UIElements;

internal class AnimTableView : TableView
{
    private Foldout _animFoldout;
    public AnimTableView(MultiColumnListView multiColumnListView, Foldout animFoldout) : base(multiColumnListView)
    {
        multiColumnListView.Q<VisualElement>("Id").style.backgroundColor = new UnityEngine.Color(0, 0, 0, 0);
        multiColumnListView.Q<VisualElement>("Name").style.backgroundColor = new UnityEngine.Color(0, 0, 0, 0);
        _animFoldout = animFoldout;
    }
    public void Hide()
    {
        McListView.style.display = DisplayStyle.None;
        _animFoldout.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        McListView.style.display = DisplayStyle.Flex;
        _animFoldout.style.display = DisplayStyle.Flex;
    }

    public void Update(List<ResourceEntry> resourceEntries)
    {
        Refresh(resourceEntries);
    }
}