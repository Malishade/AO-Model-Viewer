using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ListViewEntry
{
    public Label m_NameLabel;

    public void SetVisualElement(VisualElement visualElement)
    {
        m_NameLabel = visualElement.Q<Label>("DisplayName");
    }

    public void Init(MainViewUxml.ListViewDataModel listViewData)
    {
        m_NameLabel.text = listViewData.Name;
    }
}

