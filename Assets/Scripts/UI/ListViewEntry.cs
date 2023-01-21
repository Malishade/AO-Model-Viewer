using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static RDBLoader;

public class ListViewEntry
{
    public Label m_NameLabel;

    public void SetVisualElement(VisualElement visualElement)
    {
        m_NameLabel = visualElement.Q<Label>("character-name");
    }

    public void Init(RdbData rdbData)
    {
        m_NameLabel.text = rdbData.MeshName;
    }
}

