using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Controls;
using raindesk.Control.Windows;
using raindesk.Features.Launcher.Presentation.WinUI;
using raindesk.Features.Workspace.Contracts;
using raindesk.Services;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Manages group tab editing lifecycle: create, rename, delete, select,
/// reorder suppression, and selection indicator coordination.
/// Extracted from RightCardControl to reduce page code-behind.
/// </summary>
internal sealed class GroupTabEditController
{
    private static readonly TimeSpan DeleteAnimationDuration = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan SelectionAnimationDuration = MotionTokens.Standard;
    private static readonly WorkspaceGroupTabWidthMetrics WidthMetrics =
        WorkspaceGroupTabWidthMetrics.Default;

    // ── State / controllers ───────────────────────────────────────────
    private readonly WorkspaceGroupEditSessionState<MenuInfo> _groupEdit;
    private readonly WorkspaceGroupEditVisualController _groupEditVisual;
    private readonly WorkspaceGroupEditFocusScheduler _groupEditFocus;
    private readonly WorkspaceGroupTabDragCoordinator<MenuInfo> _groupTabDrag;
    private readonly WorkspaceGroupTabChromeController _groupTabChrome;
    private readonly WorkspaceGroupTabSelectionIndicatorController _selectionIndicator;
    private readonly WorkspaceGroupTabDeleteAnimationController _deleteAnimation;
    private readonly IWorkspaceCommandHandler _workspaceCommands;
    private readonly WorkspaceNavigationPresenter _workspaceNavigation;

    // ── Page dependencies (injected as callbacks) ─────────────────────
    private readonly Func<AppData> _getAppData;
    private readonly Func<bool> _isShowingAllGroups;
    private readonly Func<AppConfig, bool> _isReducedMotion;
    private readonly Action _onSelectAllGroups;
    private readonly Action<MenuInfo, int> _onAfterGroupCreated;
    private readonly Action _syncGroupTabItems;
    private readonly LauncherIconSelectionController<IconInfo> _iconSelection;
    private readonly WorkspaceGroupTabState _workspaceGroupTabs;
    private readonly ObservableCollection<MenuInfo> _groupTabItems;
    private readonly MenuInfo _allGroupsTabEntry;
    private readonly LauncherGroupBarDragController<MenuInfo, IconInfo> _groupBarDrag;
    private readonly PointerEventHandler _rootGridPointerPressedHandler;
    private readonly Func<bool> _isGroupTabReorderSuppressed;
    private readonly Action<bool> _setGroupTabReorderSuppressed;

    public GroupTabEditController(
        WorkspaceGroupEditSessionState<MenuInfo> groupEdit,
        WorkspaceGroupEditVisualController groupEditVisual,
        WorkspaceGroupEditFocusScheduler groupEditFocus,
        WorkspaceGroupTabDragCoordinator<MenuInfo> groupTabDrag,
        WorkspaceGroupTabChromeController groupTabChrome,
        WorkspaceGroupTabSelectionIndicatorController selectionIndicator,
        WorkspaceGroupTabDeleteAnimationController deleteAnimation,
        IWorkspaceCommandHandler workspaceCommands,
        WorkspaceNavigationPresenter workspaceNavigation,
        LauncherIconSelectionController<IconInfo> iconSelection,
        WorkspaceGroupTabState workspaceGroupTabs,
        LauncherGroupBarDragController<MenuInfo, IconInfo> groupBarDrag,
        PointerEventHandler rootGridPointerPressedHandler,
        Func<bool> isGroupTabReorderSuppressed,
        Action<bool> setGroupTabReorderSuppressed,
        Func<AppData> getAppData,
        Func<bool> isShowingAllGroups,
        Func<AppConfig, bool> isReducedMotion,
        Action onSelectAllGroups,
        Action<MenuInfo, int> onAfterGroupCreated,
        Action syncGroupTabItems)
    {
        _groupEdit = groupEdit;
        _groupEditVisual = groupEditVisual;
        _groupEditFocus = groupEditFocus;
        _groupTabDrag = groupTabDrag;
        _groupTabChrome = groupTabChrome;
        _selectionIndicator = selectionIndicator;
        _deleteAnimation = deleteAnimation;
        _workspaceCommands = workspaceCommands;
        _workspaceNavigation = workspaceNavigation;
        _iconSelection = iconSelection;
        _workspaceGroupTabs = workspaceGroupTabs;
        _groupTabItems = workspaceGroupTabs.Items;
        _allGroupsTabEntry = workspaceGroupTabs.AllGroupsEntry;
        _groupBarDrag = groupBarDrag;
        _rootGridPointerPressedHandler = rootGridPointerPressedHandler;
        _isGroupTabReorderSuppressed = isGroupTabReorderSuppressed;
        _setGroupTabReorderSuppressed = setGroupTabReorderSuppressed;
        _getAppData = getAppData;
        _isShowingAllGroups = isShowingAllGroups;
        _isReducedMotion = isReducedMotion;
        _onSelectAllGroups = onSelectAllGroups;
        _onAfterGroupCreated = onAfterGroupCreated;
        _syncGroupTabItems = syncGroupTabItems;
    }

    // ── Query ─────────────────────────────────────────────────────────

    public bool IsGroupEditing()
        => _groupEdit.Target?.MenuEdit == Visibility.Visible;

    // ── Group editing lifecycle ────────────────────────────────────────

    public void BeginGroupInlineEdit(MenuInfo menuInfo, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (_groupEdit.Target != null
            && !_groupEdit.IsTarget(menuInfo)
            && IsGroupEditing())
        {
            TryCommitActiveGroupEdit(groupListView, dispatcher);
        }

        _groupEdit.Begin(menuInfo, menuInfo.MenuName);
        SetGroupReorderEnabled(false, groupListView);
        menuInfo.MenuEdit = Visibility.Visible;
        FocusInlineGroupEditor(menuInfo, groupListView, dispatcher);
    }

    public void FocusInlineGroupEditor(MenuInfo menuInfo, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (menuInfo == null || groupListView == null || dispatcher == null) return;

        groupListView.ScrollIntoView(menuInfo);
        _groupEditFocus.Schedule(
            dispatcher,
            () => TryFocusInlineGroupEditor(menuInfo, groupListView, dispatcher),
            exhausted: () => AllowGroupEditLostFocus(menuInfo),
            settled: () => AllowGroupEditLostFocus(menuInfo));
    }

    private bool TryFocusInlineGroupEditor(MenuInfo menuInfo, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (dispatcher == null) return true;
        if (!_groupEdit.IsTarget(menuInfo) || menuInfo.MenuEdit != Visibility.Visible)
        {
            return true;
        }

        if (!_groupEditVisual.TryGetEditor(
                groupListView,
                menuInfo,
                out ListViewItem container,
                out TextBox editor))
        {
            return false;
        }

        AppData appData = _getAppData();
        _groupEditVisual.PrepareEditor(
            container,
            editor,
            _groupEdit.OriginalName,
            initializeText: _groupEdit.TryEstablishFocus(),
            widthMetrics: WidthMetrics,
            dispatcherQueue: dispatcher);
        ScheduleSelectionIndicatorUpdate(
            _workspaceNavigation.GetActiveGroupIndex(appData),
            animate: false,
            groupListView: groupListView,
            groupScrollViewer: null,
            selectionIndicatorElement: null,
            dispatcher: dispatcher);
        return true;
    }

    public void AllowGroupEditLostFocus(MenuInfo menuInfo)
    {
        if (_groupEdit.IsTarget(menuInfo))
        {
            _groupEdit.AllowLostFocus();
        }
    }

    public void CommitGroupRename(TextBox box, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (box?.Tag is not MenuInfo menuInfo || !_groupEdit.IsTarget(menuInfo))
        {
            FinishGroupEdit(groupListView, dispatcher);
            return;
        }

        string newName = (box.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            CancelGroupRename(box, groupListView, dispatcher);
            return;
        }

        WorkspaceCommandResult result = _workspaceCommands.Rename(
            new RenameWorkspaceGroupCommand(menuInfo.MenuId, newName));
        if (!result.Success)
        {
            CancelGroupRename(box, groupListView, dispatcher);
            GrowlService.Warning(result.Error ?? "分组重命名失败，请重试。");
            return;
        }

        menuInfo.MenuEdit = Visibility.Collapsed;
        FinishGroupEdit(groupListView, dispatcher);
    }

    public void CancelGroupRename(TextBox box, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (box?.Tag is MenuInfo menuInfo)
        {
            menuInfo.MenuName = _groupEdit.OriginalName;
            menuInfo.MenuEdit = Visibility.Collapsed;
        }

        FinishGroupEdit(groupListView, dispatcher);
    }

    public void FinishGroupEdit(ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (_groupEdit.Target != null)
        {
            _groupEditVisual.DetachEditor(groupListView, _groupEdit.Target);
        }

        _groupEdit.Complete();
        SetGroupReorderEnabled(true, groupListView);

        if (!_isShowingAllGroups())
        {
            RefreshGroupTabNaturalWidths(groupListView);
            AppData appData = _getAppData();
            ScheduleSelectionIndicatorUpdate(
                _workspaceNavigation.GetActiveGroupIndex(appData),
                animate: !_isReducedMotion(appData.AppConfig),
                groupListView: groupListView,
                groupScrollViewer: null,
                selectionIndicatorElement: null,
                dispatcher: dispatcher);
        }
    }

    public bool TryCommitActiveGroupEdit(ListView? groupListView, DispatcherQueue? dispatcher)
    {
        MenuInfo? target = _groupEdit.Target;
        if (target == null || target.MenuEdit != Visibility.Visible)
        {
            return false;
        }

        _groupEdit.AllowLostFocus();

        if (_groupEditVisual.TryGetEditor(
                groupListView,
                target,
                out _,
                out TextBox editor))
        {
            CommitGroupRename(editor, groupListView, dispatcher);
            return true;
        }

        string newName = (target.MenuName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            target.MenuName = _groupEdit.OriginalName;
            target.MenuEdit = Visibility.Collapsed;
            FinishGroupEdit(groupListView, dispatcher);
            return true;
        }

        target.MenuEdit = Visibility.Collapsed;
        WorkspaceCommandResult result = _workspaceCommands.Rename(
            new RenameWorkspaceGroupCommand(target.MenuId, newName));
        if (!result.Success)
        {
            target.MenuName = _groupEdit.OriginalName;
            GrowlService.Warning(result.Error ?? "分组重命名失败，请重试。");
        }
        FinishGroupEdit(groupListView, dispatcher);
        return true;
    }

    public void CommitGroupEditOnOutsidePointer(DependencyObject? source, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        MenuInfo? target = _groupEdit.Target;
        if (target != null
            && IsGroupEditing()
            && _groupEdit.ShouldCommitOutside(
                _groupEditVisual.IsInsideEditor(source, target)))
        {
            TryCommitActiveGroupEdit(groupListView, dispatcher);
        }
    }

    public void CancelGroupEditForStateReplacement(ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (_groupEdit.Target == null) return;

        _groupEdit.Target.MenuName = _groupEdit.OriginalName;
        _groupEdit.Target.MenuEdit = Visibility.Collapsed;
        FinishGroupEdit(groupListView, dispatcher);
    }

    // ── Group create ──────────────────────────────────────────────────

    public void HandleCreateGroupClick(
        object sender, RoutedEventArgs e,
        ListView? groupListView, ScrollViewer? groupScrollViewer,
        FrameworkElement? selectionIndicatorElement, DispatcherQueue? dispatcher)
    {
        TryCommitActiveGroupEdit(groupListView, dispatcher);

        AppData appData = _getAppData();
        WorkspaceCommandResult result = _workspaceCommands.Create(
            new CreateWorkspaceGroupCommand("自定义分组"));
        if (!result.Success || string.IsNullOrWhiteSpace(result.EntityId))
        {
            GrowlService.Warning(result.Error ?? "创建分组失败，请重试。");
            return;
        }

        MenuInfo? info = appData.MenuList.FirstOrDefault(menu =>
            string.Equals(menu.MenuId, result.EntityId, StringComparison.Ordinal));
        if (info == null)
        {
            GrowlService.Warning("创建分组后无法定位新分组，请重试。");
            return;
        }

        int newIndex = appData.MenuList.IndexOf(info);
        _onAfterGroupCreated(info, newIndex);

        dispatcher?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            groupListView?.UpdateLayout();
            if (groupScrollViewer != null)
            {
                double offset = groupScrollViewer.ScrollableHeight;
                if (offset > 0) groupScrollViewer.ChangeView(null, offset, null, appData.AppConfig.PMModel);
            }
            ScheduleSelectionIndicatorUpdate(newIndex, animate: false,
                groupListView, groupScrollViewer, selectionIndicatorElement, dispatcher);
            dispatcher?.TryEnqueue(DispatcherQueuePriority.Low, () =>
                BeginGroupInlineEdit(info, groupListView, dispatcher));
        });
    }

    // ── Group geometry & rename entry points ───────────────────────────

    public void HandleEditGroupGeometryClick(object sender, RoutedEventArgs e)
    {
        MenuInfo? menuInfo = ResolveGroupForContextMenu(sender);
        if (menuInfo == null || menuInfo.IsAllGroupsTab_NoWrite) return;

        IconfontWindow.Show(SvgToGeometry.GetIconfonts(), menuInfo);
    }

    public void HandleRenameGroupClick(object sender, RoutedEventArgs e, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        MenuInfo? menuInfo = ResolveGroupForContextMenu(sender);
        if (menuInfo == null || menuInfo.IsAllGroupsTab_NoWrite) return;

        BeginGroupInlineEdit(menuInfo, groupListView, dispatcher);
    }

    // ── XAML event handlers ───────────────────────────────────────────

    public void HandleGroupEditKeyDown(object sender, KeyRoutedEventArgs e, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (sender is not TextBox box) return;

        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            _groupEdit.AllowLostFocus();
            CommitGroupRename(box, groupListView, dispatcher);
            e.Handled = true;
        }
        else if (e.Key == global::Windows.System.VirtualKey.Escape)
        {
            _groupEdit.AllowLostFocus();
            CancelGroupRename(box, groupListView, dispatcher);
            e.Handled = true;
        }
    }

    public void HandleGroupEditLostFocus(object sender, RoutedEventArgs e, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (sender is not TextBox box
            || box.Tag is not MenuInfo menu
            || !_groupEdit.ShouldCommitLostFocus(menu)) return;

        dispatcher?.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (!_groupEdit.ShouldCommitLostFocus(box.Tag as MenuInfo)) return;
            TryCommitActiveGroupEdit(groupListView, dispatcher);
        });
    }

    // ── Reorder control ───────────────────────────────────────────────

    public void SetGroupReorderEnabled(bool enabled, ListView? groupListView)
    {
        _groupTabDrag.SetReorderEnabled(enabled);
        if (groupListView != null)
        {
            groupListView.AllowDrop = true;
        }
    }

    public void SuppressGroupTabReorderForIconDrag()
    {
        if (_isGroupTabReorderSuppressed()) return;
        _groupTabDrag.SuspendReorder();
        _setGroupTabReorderSuppressed(true);
    }

    public void RestoreGroupTabReorderAfterIconDrag()
    {
        if (!_isGroupTabReorderSuppressed()) return;
        _groupTabDrag.RestoreReorder(allowed: !IsGroupEditing());
        _setGroupTabReorderSuppressed(false);
    }

    // ── Group delete ──────────────────────────────────────────────────

    public async System.Threading.Tasks.Task HandleDeleteGroupClick(object sender, RoutedEventArgs e, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        MenuInfo? menuInfo = ResolveGroupForContextMenu(sender);
        if (menuInfo == null || menuInfo.IsAllGroupsTab_NoWrite) return;

        if (menuInfo.IconList != null && menuInfo.IconList.Count > 0)
        {
            bool confirmed = await GrowlService.AskAsync("确认删除此分组吗?", "MainWindowAskGrowl");
            if (confirmed)
            {
                _ = DeleteGroupAsync(menuInfo, groupListView, dispatcher);
            }
            return;
        }

        _ = DeleteGroupAsync(menuInfo, groupListView, dispatcher);
    }

    private async System.Threading.Tasks.Task DeleteGroupAsync(MenuInfo menuInfo, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        AppData appData = _getAppData();
        int index = appData.MenuList.IndexOf(menuInfo);
        if (index < 0) return;

        TryCommitActiveGroupEdit(groupListView, dispatcher);

        bool removed;
        if (!_isReducedMotion(appData.AppConfig) && MotionPreferences.ShouldAnimate)
        {
            removed = await AnimateGroupTabDeleteAsync(menuInfo, groupListView, dispatcher);
        }
        else
        {
            removed = RemoveGroupFromData(menuInfo);
        }

        if (removed)
        {
            _onSelectAllGroups();
        }
    }

    private bool RemoveGroupFromData(MenuInfo menuInfo)
    {
        int tabIndex = _groupTabItems.IndexOf(menuInfo);
        if (tabIndex >= 0)
        {
            _workspaceGroupTabs.Remove(menuInfo);
        }

        WorkspaceCommandResult result;
        using (_workspaceGroupTabs.SuppressSourceSynchronization())
        {
            result = _workspaceCommands.Delete(
                new DeleteWorkspaceGroupCommand(menuInfo.MenuId));
        }

        if (!result.Success)
        {
            _syncGroupTabItems();
            GrowlService.Warning(result.Error ?? "删除分组失败，请重试。");
            return false;
        }

        return result.Changed;
    }

    private async System.Threading.Tasks.Task<bool> AnimateGroupTabDeleteAsync(MenuInfo menuInfo, ListView? groupListView, DispatcherQueue? dispatcher)
    {
        if (groupListView == null)
        {
            return RemoveGroupFromData(menuInfo);
        }

        int tabIndex = _groupTabItems.IndexOf(menuInfo);
        ListViewItem? container = null;
        if (tabIndex >= 0)
        {
            groupListView.UpdateLayout();
            container = groupListView.ContainerFromIndex(tabIndex) as ListViewItem;
        }

        _groupTabDrag.EnableDeleteTransitions(groupListView, DeleteAnimationDuration);

        if (container != null)
        {
            await _deleteAnimation.AnimateOutAsync(container, DeleteAnimationDuration);
        }

        bool removed = RemoveGroupFromData(menuInfo);
        if (dispatcher != null)
        {
            _deleteAnimation.ScheduleTransitionRestore(
                dispatcher,
                () =>
                {
                    if (groupListView != null)
                    {
                        _groupTabDrag.RestoreTransitions(groupListView);
                    }
                });
        }
        return removed;
    }

    // ── Selection ─────────────────────────────────────────────────────

    public void RefreshGroupSelectionState(
        int selectedIndex,
        ListView? groupListView,
        ScrollViewer? groupScrollViewer,
        FrameworkElement? selectionIndicatorElement,
        DispatcherQueue? dispatcher)
    {
        AppData appData = _getAppData();
        _groupBarDrag.ClearIndicators();

        WorkspaceGroupSelectionProjection.Apply(
            _allGroupsTabEntry,
            appData.MenuList,
            selectedIndex,
            static (menu, isSelected) => menu.IsSelected_NoWrite = isSelected);

        _groupTabChrome.ApplyAll(groupListView, _groupTabItems.Count);
        RefreshGroupTabNaturalWidths(groupListView);

        bool animate = _selectionIndicator.BeginRefresh(
            animationsEnabled: !_isReducedMotion(appData.AppConfig));
        ScheduleSelectionIndicatorUpdate(selectedIndex, animate,
            groupListView, groupScrollViewer, selectionIndicatorElement, dispatcher);
    }

    public void ScheduleSelectionIndicatorUpdate(
        int selectedIndex,
        bool animate,
        ListView? groupListView,
        ScrollViewer? groupScrollViewer,
        FrameworkElement? selectionIndicatorElement,
        DispatcherQueue? dispatcher)
    {
        if (dispatcher == null) return;
        AppData appData = _getAppData();
        _selectionIndicator.ScheduleUpdate(
            dispatcher,
            selectedIndex,
            animate,
            _groupTabItems,
            groupListView,
            groupScrollViewer,
            selectionIndicatorElement,
            static menu => menu.MenuEdit == Visibility.Visible,
            WidthMetrics,
            animationsAllowed: !_isReducedMotion(appData.AppConfig) && MotionPreferences.ShouldAnimate,
            duration: SelectionAnimationDuration);
    }

    // ── Natural widths ────────────────────────────────────────────────

    public void RefreshGroupTabNaturalWidths(ListView? groupListView)
    {
        if (groupListView == null) return;

        for (int i = 0; i < groupListView.Items.Count; i++)
        {
            if (groupListView.ContainerFromIndex(i) is not ListViewItem item
                || groupListView.Items[i] is not MenuInfo menu)
            {
                continue;
            }

            if (menu.MenuEdit == Visibility.Visible)
            {
                WorkspaceGroupTabSizingAdapter.ApplyEditWidth(item, WidthMetrics);
            }
            else
            {
                WorkspaceGroupTabSizingAdapter.ApplyNaturalWidth(item, WidthMetrics);
            }
        }
    }

    // ── Context menu helper ───────────────────────────────────────────

    private static MenuInfo? ResolveGroupForContextMenu(object sender)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuInfo taggedMenu)
        {
            return taggedMenu;
        }

        return null;
    }
}
