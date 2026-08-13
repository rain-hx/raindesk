using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Manages the folder overlay lifecycle: open, navigate, close,
/// and folder overlay drag/drop operations.
/// </summary>
internal sealed class FolderOverlayController
{
    private readonly LauncherFolderOverlayViewModel _state;
    private readonly Action _refreshOverlay;
    private readonly Func<IconInfo, bool> _isAddShortcutPlaceholder;
    private readonly Action<IconInfo> _activateIcon;
    private readonly Action _animateOpen;
    private readonly Action<Action> _animateClose;

    public FolderOverlayController(
        LauncherFolderOverlayViewModel state,
        Action refreshOverlay,
        Func<IconInfo, bool> isAddShortcutPlaceholder,
        Action<IconInfo> activateIcon,
        Action animateOpen,
        Action<Action> animateClose)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _refreshOverlay = refreshOverlay ?? throw new ArgumentNullException(nameof(refreshOverlay));
        _isAddShortcutPlaceholder = isAddShortcutPlaceholder ?? throw new ArgumentNullException(nameof(isAddShortcutPlaceholder));
        _activateIcon = activateIcon ?? throw new ArgumentNullException(nameof(activateIcon));
        _animateOpen = animateOpen ?? throw new ArgumentNullException(nameof(animateOpen));
        _animateClose = animateClose ?? throw new ArgumentNullException(nameof(animateClose));
    }

    public LauncherFolderOverlayViewModel State => _state;

    // ── Navigation ──────────────────────────────────────────────────

    public void OpenFolder(IconInfo folder)
    {
        if (folder?.IsFolder != true) return;
        _state.OpenRoot(folder);
        _refreshOverlay();
        _animateOpen();
    }

    public void OpenChildFolder(IconInfo folder)
    {
        _state.OpenChild(folder);
        _refreshOverlay();
    }

    public void Close()
    {
        if (!_state.IsOpen) return;
        _animateClose(_state.Close);
    }

    public void Back()
    {
        _state.Back();
        _refreshOverlay();
    }

    public void RefreshFolderOverlay()
    {
        IconInfo? folder = _state.Folder;
        if (folder?.IsFolder != true)
        {
            Close();
            return;
        }

        _state.Refresh();
        foreach (IconInfo child in folder.FolderItems)
        {
            ImageUtil.EnsureIconBitmap(child);
        }
    }

    // ── Event handlers (thin forwarding) ────────────────────────────

    public void HandleCloseClick(object sender, RoutedEventArgs e) => Close();

    public void HandleBackdropTapped(object sender, TappedRoutedEventArgs e) => Close();

    public void HandlePanelTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

    public void HandleItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not IconInfo icon) return;

        if (icon.IsFolder)
        {
            OpenChildFolder(icon);
            return;
        }

        _activateIcon(icon);
    }

    public void HandleBackButton(object sender, RoutedEventArgs e) => Back();

    // ── Drag/Drop ───────────────────────────────────────────────────

    public void HandleDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Properties.ContainsKey("IconInfo") || _state.Folder?.IsFolder != true)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        e.Handled = true;
    }

    public bool IsValidDragItem(DragItemsStartingEventArgs e)
    {
        IconInfo? icon = e.Items.OfType<IconInfo>().FirstOrDefault(item => !_isAddShortcutPlaceholder(item));
        return icon != null;
    }

    public void RefreshLayout(ListView? folderItemsListView)
    {
        if (folderItemsListView?.ItemsPanelRoot is Panel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }

        folderItemsListView?.UpdateLayout();
    }
}
