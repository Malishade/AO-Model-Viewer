using ContextualMenuPlayer;
using SFB;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ContextualMenuManager = ContextualMenuPlayer.ContextualMenuManager;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;

internal class FileMenu
{
    private DropdownMenu _fileDropdownMenu;
    public Action LoadClick;
    public Action ExportClick;
    public Action InfoClick;
    public Action ExportAllClick;
    private ContextualMenuManager _menuManager;
    private VisualElement _root;
    internal FileMenu(VisualElement root, Button fileMenu)
    {
        _root = root;
        _menuManager = new ContextualMenuManager();

        root.AddManipulator(new ContextualMenuManipulator());
        fileMenu.RegisterCallback<ClickEvent>(ExpandFileMenu);

        _fileDropdownMenu = new();

        _fileDropdownMenu.AppendAction("Set AO Directory", SetAODirectoryClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction("Load Resource Database", LoadClicked, LoadStatusCallback);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Close Database", CloseClicked, CloseStatusCallback);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction("Export Selected", ExportClicked, IsRdbOpen);
        _fileDropdownMenu.AppendAction("Export Active List", ExportAll, IsRdbOpen);
        _fileDropdownMenu.AppendSeparator();
        _fileDropdownMenu.AppendAction($"Info", InfoClicked, DropdownMenuAction.AlwaysEnabled);
        _fileDropdownMenu.AppendAction($"Exit", ExitClicked, DropdownMenuAction.AlwaysEnabled);
        //_fileDropdownMenu.AppendAction("Import", ImportClicked, DropdownMenuAction.AlwaysEnabled);

    }

    private void InfoClicked(DropdownMenuAction action)
    {
        InfoClick?.Invoke();
    }

    private void ExportAll(DropdownMenuAction action)
    {
        ExportAllClick?.Invoke();
    }

    private void LoadClicked(DropdownMenuAction action)
    {
        LoadClick?.Invoke();
    }

    private void ExportClicked(DropdownMenuAction action)
    {
        ExportClick?.Invoke();
    }

    private DropdownMenuAction.Status IsRdbOpen(DropdownMenuAction e)
    {
        return RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
    }

    private void CloseClicked(DropdownMenuAction action)
    {
        RDBLoader.Instance.CloseDatabase();
    }

    private void ExitClicked(DropdownMenuAction action)
    {
        Application.Quit();
    }

    private void SetAODirectoryClicked(DropdownMenuAction action)
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Locate the Anarchy Online Folder", null, false, (paths) =>
        {
            if (paths.Length == 0)
                return;

            SettingsManager.Instance.Settings.AODirectory = paths.First();
            SettingsManager.Instance.Save();
        });
    }

    private DropdownMenuAction.Status LoadStatusCallback(DropdownMenuAction e) => SettingsManager.Instance.Settings.AODirectory == null || RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;

    private DropdownMenuAction.Status CloseStatusCallback(DropdownMenuAction e) => RDBLoader.Instance.IsOpen ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

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
}
