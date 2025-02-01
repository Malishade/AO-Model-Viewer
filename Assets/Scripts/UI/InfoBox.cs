using System;
using UnityEngine.UIElements;

internal class InfoBox
{
    private readonly VisualElement _infoBox;
    private readonly Button _closeButton;

    public InfoBox(VisualElement infoBox, Button closeButton)
    {
        _closeButton = closeButton;
        _infoBox = infoBox;
        _closeButton.clicked += OnButtonClick;
    }

    private void OnButtonClick()
    {
        _infoBox.style.display = DisplayStyle.None; 
    }
    
    public void Show()
    {
        _infoBox.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _infoBox.style.display = DisplayStyle.None;
    }
}