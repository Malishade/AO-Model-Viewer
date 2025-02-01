using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

internal class ModelInspectorView
{
    public MaterialTypeId ActiveMaterial;
    public Action<MaterialTypeId> MaterialChange;
    public Action<bool> ShowSkeleton;

    private Foldout _foldout;
    private Label _vertices;
    private Label _edges;
    private Toggle _showSkeleton;

    internal ModelInspectorView(Foldout foldout, RadioButtonGroup radioButtonGroup, Label vertsCount, Label edgesCount, Toggle showSkeleton)
    {
        var matRadioGroup = radioButtonGroup;
        _vertices = vertsCount;
        _edges = edgesCount;
        _foldout = foldout;
        _showSkeleton = showSkeleton;
        _showSkeleton.RegisterValueChangedCallback(OnClick);
        _showSkeleton.style.display = DisplayStyle.None;
        matRadioGroup.RegisterValueChangedCallback(MaterialTypeChangeEvent);
    }

    private void OnClick(ChangeEvent<bool> evt)
    {
        ShowSkeleton?.Invoke(evt.newValue);
    }

    public void UpdateData(int verts, int edges)
    {
        _vertices.text = verts.ToString();
        _edges.text = edges.ToString();
    }

    private void MaterialTypeChangeEvent(ChangeEvent<int> evt)
    {
        var activeMat = (MaterialTypeId)evt.newValue;
        MaterialChange?.Invoke((MaterialTypeId)evt.newValue);
        ActiveMaterial = activeMat;
    }

    public void ShowSkeletonToggle()
    {
        _showSkeleton.style.display = DisplayStyle.Flex;
    }

    public void HideSkeletonToggle()
    {
        _showSkeleton.style.display = DisplayStyle.None;
    }

    public void Hide()
    {
        _foldout.style.display = DisplayStyle.None;
    }
    public void Show()
    {
        _foldout.style.display = DisplayStyle.Flex;
    }
}