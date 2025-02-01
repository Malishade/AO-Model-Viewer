using System;
using UnityEngine.UIElements;

internal class SearchBar
{
    private readonly Label _resultsLabel;
    private readonly TextField _searchText;
    private readonly Button _clearButton;
    public Action<string> OnTextUpdate;

    public SearchBar(TextField searchText, Button clearTextButton, Label resultsLabel)
    {
        _resultsLabel = resultsLabel;
        _searchText = searchText;

        _clearButton = clearTextButton;
        _searchText.SetEnabled(false);
        _searchText.RegisterCallback<ChangeEvent<string>>(x => OnTextUpdate?.Invoke(x.newValue));
        _clearButton.SetEnabled(false);
        _clearButton.RegisterCallback<ClickEvent>(x => _searchText.value = "");
    }

    public void UpdateResults(int count)
    {
        _resultsLabel.text = count.ToString();
    }

    public void ClearText()
    {
        _searchText.value = string.Empty;
    }

    public void Enable()
    {
        _searchText.SetEnabled(true);
        _clearButton.SetEnabled(true);
    }
}