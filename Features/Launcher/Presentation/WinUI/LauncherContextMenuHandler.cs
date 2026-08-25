using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Constant;
using raindesk.Controls;
using raindesk.Control.Other;
using raindesk.Features.Appearance.Contracts;
using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Handles context menu opening, icon activation (tap/double-tap/admin/show-in-explorer),
/// folder resize, remove, rename, property dialog, and card-level commands.
/// Extracted from RightCardControl to reduce page code-behind.
/// </summary>
internal sealed class LauncherContextMenuHandler
{
    private const long DoubleTapIntervalMs = 500;
    private readonly LauncherContextMenuCoordinator<IconInfo> _contextMenu;
    private readonly LauncherContextMenuViewController<IconInfo> _contextMenuView;
    private readonly LauncherIconActivationCoordinator<IconInfo> _iconActivation;
    private readonly ILauncherCommandHandler _launcherCommands;
    private readonly IAppearanceQueryHandler _appearanceQuery;
    private readonly IAppearanceCommandHandler _appearanceCommand;
    private readonly LauncherBatchModeState _batchMode;
    private readonly LauncherShortcutLayoutApplyController _layoutApply;
    private readonly FolderOverlayController _folderOverlay;
    private readonly LauncherInlineAddPanelState _inlinePanel;
    private readonly Func<AppData> _getAppData;
    private readonly Func<LauncherBatchModeController<IconInfo>> _getBatchModeController;
    private readonly Func<BulkObservableCollection<IconInfo>> _getShortcutTiles;
    private readonly Func<IReadOnlyList<IconInfo>> _getSelectedShortcutIcons;
    private readonly Action _clearShortcutSelection;
    private readonly Action _rebuildShortcutTiles;
    private readonly Action _syncFolderOverlayAfterMutation;
    private readonly Action _refreshShortcutItemSpans;
    private readonly Action _showAddUrlPanel;
    private readonly Action _showAddSystemPanel;

    // Cached context targets
    private IconInfo? _contextMenuIcon;
    private MenuInfo? _contextMenuGroup;
    private IconInfo? _lastPressedIcon;
    private long _previousPressTicks = -1;
    private long _lastPressTicks = -1;

    public LauncherContextMenuHandler(
        LauncherContextMenuCoordinator<IconInfo> contextMenu,
        LauncherContextMenuViewController<IconInfo> contextMenuView,
        LauncherIconActivationCoordinator<IconInfo> iconActivation,
        ILauncherCommandHandler launcherCommands,
        IAppearanceQueryHandler appearanceQuery,
        IAppearanceCommandHandler appearanceCommand,
        LauncherBatchModeState batchMode,
        LauncherShortcutLayoutApplyController layoutApply,
        FolderOverlayController folderOverlay,
        LauncherInlineAddPanelState inlinePanel,
        Func<AppData> getAppData,
        Func<LauncherBatchModeController<IconInfo>> getBatchModeController,
        Func<BulkObservableCollection<IconInfo>> getShortcutTiles,
        Func<IReadOnlyList<IconInfo>> getSelectedShortcutIcons,
        Action clearShortcutSelection,
        Action rebuildShortcutTiles,
        Action syncFolderOverlayAfterMutation,
        Action refreshShortcutItemSpans,
        Action showAddUrlPanel,
        Action showAddSystemPanel)
    {
        _contextMenu = contextMenu;
        _contextMenuView = contextMenuView;
        _iconActivation = iconActivation;
        _launcherCommands = launcherCommands;
        _appearanceQuery = appearanceQuery;
        _appearanceCommand = appearanceCommand;
        _batchMode = batchMode;
        _layoutApply = layoutApply;
        _folderOverlay = folderOverlay;
        _inlinePanel = inlinePanel;
        _getAppData = getAppData;
        _getBatchModeController = getBatchModeController;
        _getShortcutTiles = getShortcutTiles;
        _getSelectedShortcutIcons = getSelectedShortcutIcons;
        _clearShortcutSelection = clearShortcutSelection;
        _rebuildShortcutTiles = rebuildShortcutTiles;
        _syncFolderOverlayAfterMutation = syncFolderOverlayAfterMutation;
        _refreshShortcutItemSpans = refreshShortcutItemSpans;
        _showAddUrlPanel = showAddUrlPanel;
        _showAddSystemPanel = showAddSystemPanel;
    }

    // ── Context menu opening ──────────────────────────────────────────

    public void HandleIconContextFlyoutOpening(object? sender, object e,
        Func<DependencyObject, IconInfo?> resolveIconFromTarget)
    {
        if (sender is not MenuFlyout flyout) return;

        IconInfo? icon = resolveIconFromTarget(flyout.Target);
        _contextMenuIcon = icon;
        LauncherContextMenuPlan? plan = icon == null
            ? null
            : _contextMenu.Resolve(icon);
        _contextMenuView.Apply(plan, icon);
    }

    public void HandleGroupContextFlyoutOpening(object? sender, object e,
        Func<DependencyObject, MenuInfo?> resolveGroupFromTarget,
        FrameworkElement? editGroupIconMenuItem,
        FrameworkElement? editGroupIconMenuSeparator,
        FrameworkElement? deleteGroupMenuItem)
    {
        if (sender is not MenuFlyout flyout) return;

        MenuInfo? menu = resolveGroupFromTarget(flyout.Target);
        _contextMenuGroup = menu;
        if (menu == null) return;

        bool isAllGroupsTab = menu.IsAllGroupsTab_NoWrite;
        if (editGroupIconMenuItem != null)
            editGroupIconMenuItem.Visibility = isAllGroupsTab ? Visibility.Collapsed : Visibility.Visible;
        if (editGroupIconMenuSeparator != null)
            editGroupIconMenuSeparator.Visibility = isAllGroupsTab ? Visibility.Collapsed : Visibility.Visible;
        if (deleteGroupMenuItem != null)
            deleteGroupMenuItem.Visibility = isAllGroupsTab ? Visibility.Collapsed : Visibility.Visible;

        foreach (var item in flyout.Items)
        {
            if (item is MenuFlyoutItem menuItem)
                menuItem.Tag = menu;
        }
    }

    // ── Icon activation ───────────────────────────────────────────────

    public void RecordIconPointerPress(IconInfo icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        long now = Environment.TickCount64;
        if (!ReferenceEquals(icon, _lastPressedIcon)
            || _lastPressTicks < 0
            || now - _lastPressTicks > DoubleTapIntervalMs)
        {
            _previousPressTicks = -1;
        }
        else
        {
            _previousPressTicks = _lastPressTicks;
        }

        _lastPressedIcon = icon;
        _lastPressTicks = now;
    }

    public void HandleIconTapped(object sender, TappedRoutedEventArgs e,
        Func<object, IconInfo?> getIconFromSender)
    {
        IconInfo? icon = getIconFromSender(sender);
        if (icon == null) return;

        bool withinDoubleTapWindow = ReferenceEquals(icon, _lastPressedIcon)
            && _previousPressTicks >= 0
            && _lastPressTicks - _previousPressTicks <= DoubleTapIntervalMs;
        _previousPressTicks = -1;
        if (!withinDoubleTapWindow)
        {
            e.Handled = true;
            return;
        }

        AppData appData = _getAppData();
        e.Handled = _iconActivation.HandleTap(
            icon,
            RunTimeStatus.SEARCH_BOX_HIDED_300,
            LauncherIconSelectionController<IconInfo>.IsMultiSelectModifierDown(),
            _batchMode.IsEnabled,
            appData.AppConfig.DoubleOpen);
    }

    public void HandleIconDoubleTapped(object sender, DoubleTappedRoutedEventArgs e,
        Func<object, IconInfo?> getIconFromSender)
    {
        IconInfo? icon = getIconFromSender(sender);
        if (icon == null) return;

        AppData appData = _getAppData();
        e.Handled = _iconActivation.HandleDoubleTap(
            icon,
            RunTimeStatus.SEARCH_BOX_HIDED_300,
            _batchMode.IsEnabled,
            appData.AppConfig.DoubleOpen);
    }

    // ── Context menu icon actions ─────────────────────────────────────

    public void HandleIconAdminStart(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
    {
        IconInfo? icon = resolveIcon(sender);
        if (icon != null
            && _contextMenu.CanUse(icon, LauncherContextMenuAction.RunAsAdministrator))
        {
            _iconActivation.Activate(icon, LauncherActivationMode.Administrator);
        }
    }

    public void HandleShowInExplore(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
    {
        IconInfo? icon = resolveIcon(sender);
        if (icon != null
            && _contextMenu.CanUse(icon, LauncherContextMenuAction.ShowInFileExplorer))
        {
            _iconActivation.Activate(icon, LauncherActivationMode.ShowInFileExplorer);
        }
    }

    public void HandleOpenFolderFromContext(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
    {
        IconInfo? folder = resolveIcon(sender);
        if (folder != null
            && _contextMenu.CanUse(folder, LauncherContextMenuAction.OpenFolder))
        {
            _folderOverlay.OpenFolder(folder);
        }
    }

    // ── Folder resize ─────────────────────────────────────────────────

    public void HandleSetFolderSmall(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
        => SetFolderSizeFromMenu(sender, ShortcutFolderSize.Small, resolveIcon);

    public void HandleSetFolderMedium(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
        => SetFolderSizeFromMenu(sender, ShortcutFolderSize.Medium, resolveIcon);

    public void HandleSetFolderLarge(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
        => SetFolderSizeFromMenu(sender, ShortcutFolderSize.Large, resolveIcon);

    private void SetFolderSizeFromMenu(object sender, ShortcutFolderSize size,
        Func<object, IconInfo?> resolveIcon)
    {
        IconInfo? folder = resolveIcon(sender);
        if (folder == null) return;

        LauncherFolderSize launcherSize = size switch
        {
            ShortcutFolderSize.Small => LauncherFolderSize.Small,
            ShortcutFolderSize.Medium => LauncherFolderSize.Medium,
            ShortcutFolderSize.Large => LauncherFolderSize.Large,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
        LauncherMutationCommand? command =
            _contextMenu.CreateResizeFolderCommand(folder, launcherSize);
        if (command == null || !ExecuteLauncherCommand(command)) return;

        _layoutApply.Compact(_getShortcutTiles());
        _refreshShortcutItemSpans();
        _folderOverlay.RefreshLayout(null); // page will pass ListView separately
        _layoutApply.Schedule(animate: true);
    }

    // ── Remove / Property / Rename ────────────────────────────────────

    public async System.Threading.Tasks.Task HandleRemoveIcon(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
    {
        IconInfo? icon = resolveIcon(sender);
        if (icon == null) return;

        // 若右键目标包含在当前多选集合中，则删除全部选中项；否则只删除右键目标。
        var selected = _getSelectedShortcutIcons()
            .Where(candidate => !LauncherDragIconClassifier.IsAddShortcutPlaceholder(candidate))
            .Distinct()
            .ToList();

        var targets = new List<IconInfo>();
        if (selected.Count > 0 && selected.Contains(icon))
        {
            targets.AddRange(selected);
        }
        else if (!LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon))
        {
            targets.Add(icon);
        }

        if (targets.Count == 0) return;

        bool confirmed = targets.Count > 1
            ? await DialogHelper.ConfirmDeleteAsync($"选中的 {targets.Count} 个快捷方式")
            : await DialogHelper.ConfirmDeleteAsync(targets[0].Name ?? "此快捷方式");
        if (!confirmed) return;

        bool anyRemoved = false;
        foreach (IconInfo target in targets)
        {
            LauncherMutationCommand? command = _contextMenu.CreateRemoveCommand(target);
            if (command == null) continue;
            if (ExecuteLauncherCommand(command))
            {
                anyRemoved = true;
            }
        }

        if (anyRemoved)
        {
            _clearShortcutSelection();
            _syncFolderOverlayAfterMutation();
            _rebuildShortcutTiles();
        }
    }

    public void HandlePropertyConfig(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon)
    {
        try
        {
            IconInfo? info = resolveIcon(sender);
            if (info == null
                || !_contextMenu.CanUse(info, LauncherContextMenuAction.EditProperties)) return;
            switch (info.IconType)
            {
                case IconType.URL:
                    IconInfoUrlDialog urlDialog = new IconInfoUrlDialog(info);
                    urlDialog.dialog = DialogService.Show(urlDialog, "MainWindowDialog");
                    break;
                default:
                    IconInfoDialog dialog = new IconInfoDialog(info);
                    dialog.dialog = DialogService.Show(dialog, "MainWindowDialog");
                    break;
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "打开快捷方式属性失败");
        }
    }

    public void HandleRenameIcon(object sender, RoutedEventArgs e,
        Func<object, IconInfo?> resolveIcon,
        DispatcherQueue? dispatcher)
    {
        IconInfo? info = resolveIcon(sender);
        if (info == null
            || !_contextMenu.CanUse(info, LauncherContextMenuAction.Rename)) return;

        IconInfo icon = info;
        dispatcher?.TryEnqueue(() =>
        {
            try
            {
                IconRenameDialog renameDialog = new(icon);
                renameDialog.dialog = DialogService.Show(renameDialog, "MainWindowDialog");
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "打开重命名对话框失败");
            }
        });
    }

    // ── Card-level commands ───────────────────────────────────────────

    public void HandleAddUrlIcon(object sender, RoutedEventArgs e)
        => _showAddUrlPanel();

    public void HandleAddSystemIcon(object sender, RoutedEventArgs e)
        => _showAddSystemPanel();

    public void HandleBatchHandle(object sender, RoutedEventArgs e)
        => _getBatchModeController().Toggle();

    public void HandleShowTitleClick(object sender, RoutedEventArgs e)
    {
        bool currentlyVisible = _appearanceQuery.GetCurrent().ShowIconTitles;
        AppearanceCommandResult result = _appearanceCommand.SetIconTitleVisibility(
            new SetIconTitleVisibilityCommand(!currentlyVisible));
        if (!result.Success)
        {
            GrowlService.Warning(result.Error ?? "图标标题显示设置更新失败。");
        }
    }

    public void HandleLockAppPanel(object sender, RoutedEventArgs e)
    {
        RunTimeStatus.LOCK_APP_PANEL = !RunTimeStatus.LOCK_APP_PANEL;
    }

    // ── Cached context accessors ──────────────────────────────────────

    public IconInfo? ContextMenuIcon => _contextMenuIcon;
    public MenuInfo? ContextMenuGroup => _contextMenuGroup;

    public IconInfo? ResolveIconForContextMenu(object sender)
    {
        if (sender is MenuFlyoutItem menuItem)
        {
            if (menuItem.Tag is IconInfo taggedIcon)
                return taggedIcon;
        }

        return _contextMenuIcon;
    }

    public MenuInfo? ResolveGroupForContextMenu(object sender)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuInfo taggedMenu)
            return taggedMenu;

        return _contextMenuGroup;
    }

    // ── Internal ──────────────────────────────────────────────────────

    private bool ExecuteLauncherCommand(LauncherMutationCommand command)
    {
        LauncherCommandResult result = _launcherCommands.Execute(command);
        return result.Success && result.Changed;
    }
}
