using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using rainspace.Constant;
using rainspace.Controls;
using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Presentation;
using rainspace.Features.Workspace.Presentation;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace rainspace.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Result of TryBeginIconDragStartingCore. The page applies WinUI-specific
/// operations (data properties, container visibility, layout updates) based on this.
/// </summary>
internal sealed class LauncherDragStartResult
{
    public bool Cancel { get; set; }
    public string? DragType { get; set; }
    public IconInfo? DraggedIcon { get; set; }
    public int DragCount { get; set; }
    public bool NeedsBatchLayoutUpdate { get; set; }
    public bool NeedsPostBatchLayoutUpdate { get; set; }
    public bool NeedsCaptureTargetLayout { get; set; }
    public bool RestrictDragItemsToPlaceholder { get; set; }
    public List<IconInfo>? BatchDragIcons { get; set; }
}

/// <summary>
/// Result of HandleIconListViewDragOver / HandleWrapCardDragOver.
/// The page applies WinUI-specific operations (DragUIOverride, AcceptedOperation, Handled).
/// </summary>
internal sealed class LauncherDragOverResult
{
    public bool SuppressSystemDrag { get; set; }
    public bool UpdatePreview { get; set; }
    public bool SetFolderDropIntent { get; set; }
    public bool UpdateSingleIconDropIndex { get; set; }
    public bool UpdateBatchPlaceholderBlock { get; set; }
    public bool UpdateFolderDropIntentFromPointer { get; set; }
    public bool ClearGroupBarHighlight { get; set; }
    public bool Handled { get; set; }
    public bool AcceptedMove { get; set; }
}

/// <summary>
/// Result of HandleIconListViewDrop. The page applies WinUI-specific operations.
/// </summary>
internal sealed class LauncherDropResult
{
    public bool Skip { get; set; }
    public bool StorePointer { get; set; }
    public double PointerX { get; set; }
    public double PointerY { get; set; }
    public bool CommittedFolderDrop { get; set; }
    public bool AcceptedMove { get; set; }
    public bool Handled { get; set; }
}

/// <summary>
/// Holds references to page-level dependencies that the drag business controller
/// needs to access. The page creates and populates this once.
/// </summary>
internal sealed class LauncherDragPageContext
{
    // XAML element accessors (may return null if not materialized)
    public required Func<ListView?> GetIconListView { get; init; }
    public required Func<ListView?> GetGroupListView { get; init; }
    public required Func<FrameworkElement?> GetGroupBar { get; init; }
    public required Func<FrameworkElement?> GetWrapUFG { get; init; }
    public required Func<ListView?> GetFolderItemsListView { get; init; }
    public required Func<ScrollViewer?> GetGroupScrollViewer { get; init; }
    public required Func<Canvas?> GetIconMarqueeCanvas { get; init; }
    public required Func<FrameworkElement?> GetIconMarqueeRect { get; init; }

    // Window-level accessors: drag overlay canvas + drag-active flag.
    // Push window access through the context so the business layer never
    // reaches MainWindow.mainWindow directly.
    public required Func<Canvas?> GetDragOverlayCanvas { get; init; }
    public required Action<bool> SetIconDragActive { get; init; }
    public required Func<UIElement?> GetWindowContent { get; init; }

    // Data accessors
    public required Func<AppData> GetAppData { get; init; }
    public required Func<BulkObservableCollection<IconInfo>> GetShortcutTiles { get; init; }
    public required Func<bool> IsShowingAllGroups { get; init; }

    // Layout metrics
    public required Func<int> GetShortcutColumnCount { get; init; }
    public required Func<double> GetShortcutAvailableWidth { get; init; }
    public required Func<double> GetShortcutItemSlotWidth { get; init; }
    public required Func<double> GetShortcutItemSlotHeight { get; init; }
    public required Func<double> GetShortcutItemCardWidth { get; init; }

    // Page operations (callbacks)
    public required Func<int> GetShortcutDropIndexFromPointer { get; init; }
    public required Action RebuildShortcutTiles { get; init; }
    public required Action RefreshShortcutItemSpans { get; init; }
    public required Action RefreshIconListLayout { get; init; }
    public required Action UpdateIconReorderEnabled { get; init; }
    public required Action<string> LogDragState { get; init; }
    public required Action<int> RefreshGroupSelectionState { get; init; }
    public required Action SuppressGroupTabReorderForIconDrag { get; init; }
    public required Action RestoreGroupTabReorderAfterIconDrag { get; init; }
    public required Action<bool> BeginDragPointerTracking { get; init; }
    public required Action EndDragPointerTracking { get; init; }
    public required Func<System.Threading.Tasks.Task> ScheduleDeferredShortcutContainerRestore { get; init; }
    public required Func<IReadOnlyList<IconInfo>?> GetSelectedIcons { get; init; }
    public required Action<IReadOnlyList<IconInfo>> ApplyShortcutTileSnapshot { get; init; }
    public required TryGetDragPointerOverlayPositionDelegate TryGetDragPointerOverlayPosition { get; init; }

    public delegate bool TryGetDragPointerOverlayPositionDelegate(out Point position);

    // Controllers owned by the page
    public required LauncherShortcutLayoutApplyController LayoutApply { get; init; }
    public required LauncherItemAnimationCoordinator ItemAnimations { get; init; }
    public required LauncherShortcutContainerStateController ShortcutContainers { get; init; }
    public required LauncherIconListReorderSuppressionController IconListReorderSuppression { get; init; }
    public required LauncherIconSelectionController<IconInfo> IconSelection { get; init; }
    public required FolderOverlayController FolderOverlay { get; init; }
    public required LauncherExternalImportCoordinator ExternalImport { get; init; }
    public required LauncherShortcutTransitionController ShortcutTransitions { get; init; }
    public required LauncherShortcutLayoutScheduler ShortcutLayoutScheduler { get; init; }

    // Workspace
    public required LauncherCrossGroupMoveCoordinator CrossGroupMoveCoordinator { get; init; }
    public required WorkspaceNavigationPresenter WorkspaceNavigation { get; init; }

    // Dispatcher
    public required DispatcherQueue DispatcherQueue { get; init; }

    // Constants
    public static readonly TimeSpan ShortcutWrapAnimationDuration = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan ShortcutLayoutFlyAnimationDuration = TimeSpan.FromMilliseconds(280);
    public static readonly TimeSpan ShortcutReorderAnimationDuration = TimeSpan.FromMilliseconds(230);
    public static readonly TimeSpan LiveDragReorderDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan ShortcutReorderStaggerDelay = TimeSpan.FromMilliseconds(0);
    public static readonly TimeSpan LiveDragReorderStaggerDelay = TimeSpan.FromMilliseconds(0);
    public const double ShortcutReorderYieldRiseOffset = 12d;
    public const int BatchDragMoveThrottleMs = 48;
    public const int FolderDropHoverActivationDelayMs = 300;
    public const int SingleIconReorderActivationDelayMs = 150;
    public const double LauncherShortcutDragPreviewSize = 64d;
    public const double ShortcutGridRightInset = 8d;
    public const int MaxLiveReorderAnimatedItems = 12;
}

/// <summary>
/// Owns all drag business logic extracted from RightCardControl.
/// Handles drag start, drag over, drop, completion, single-icon reorder,
/// batch placeholder management, folder drop intent, cross-group operations,
/// and drag preview coordination.
/// </summary>
internal sealed class LauncherDragBusinessController
{
    private readonly LauncherDragPageContext _ctx;
    private readonly LauncherDragSessionState _dragState;
    private readonly LauncherDragPreviewController _dragPreviewController;
    private readonly LauncherCrossGroupPreviewOverlayController _crossGroupPreviewOverlayController;
    private readonly LauncherCrossGroupPreviewPlacementController<IconInfo, MenuInfo> _crossGroupPreviewPlacement;
    private readonly LauncherGroupBarDragController<MenuInfo, IconInfo> _groupBarDrag;
    private readonly LauncherCrossGroupDropViewCoordinator<IconInfo> _crossGroupDropView;
    private readonly LauncherDragOrchestrator _dragOrchestrator;
    private readonly ILauncherCommandHandler _launcherCommands;
    private readonly IAppDataProvider _dataProvider;

    private ListViewItem? _draggedContainer;
    private List<IconInfo>? _pendingBatchDropReleaseIcons;
    private static BitmapImage? _emptyDragBitmapImage;

    // P2: 布局缓存 — 避免同一帧内多次构建完整 Dictionary 和网格排列结果。
    // 缓存键：集合版本 + 列数 + 槽位尺寸 + 滚动原点
    private Dictionary<IconInfo, LauncherGridRect>? _cachedLayoutMap;
    private int _cachedLayoutCollectionVersion = -1;
    private int _cachedLayoutColumnCount;
    private double _cachedLayoutSlotWidth;
    private double _cachedLayoutSlotHeight;
    private Point _cachedLayoutScrollOrigin;
    private bool _cachedLayoutUsesManualPositions;

    /// <summary>使布局缓存失效（在集合变更、列数变更、滚动或拖拽开始时调用）。</summary>
    public void InvalidateLayoutCache()
    {
        _cachedLayoutMap = null;
        _cachedLayoutCollectionVersion = -1;
    }

    // Convenience accessors for drag state sub-objects
    private Canvas? DragOverlayCanvas => _ctx.GetDragOverlayCanvas();
    private UIElement? WindowContent => _ctx.GetWindowContent();
    private BulkObservableCollection<IconInfo> ShortcutTiles => _ctx.GetShortcutTiles();
    private AppData AppData => _ctx.GetAppData();
    private ListView? IconListView => _ctx.GetIconListView();
    private FrameworkElement? WrapUFG => _ctx.GetWrapUFG();
    private ListView? FolderItemsListView => _ctx.GetFolderItemsListView();
    private bool IsShowingAllGroups => _ctx.IsShowingAllGroups();

    // Drag state delegates
    private IconInfo? _draggedIcon { get => _dragState.DraggedIcon; set => _dragState.DraggedIcon = value; }
    private List<IconInfo> _draggedIcons => _dragState.DraggedIcons;
    private int _dragSourceIndex { get => _dragState.DragSourceIndex; set => _dragState.DragSourceIndex = value; }
    private bool _isCustomDragActive { get => _dragState.IsCustomDragActive; set => _dragState.IsCustomDragActive = value; }
    private bool _isCrossGroupDrag { get => _dragState.IsCrossGroupDrag; set => _dragState.IsCrossGroupDrag = value; }
    private bool _dragStartedFromFolderOverlay { get => _dragState.DragStartedFromFolderOverlay; set => _dragState.DragStartedFromFolderOverlay = value; }
    private bool _singleIconLiveMoveMutatedTiles { get => _dragState.SingleIconLiveMoveMutatedTiles; set => _dragState.SingleIconLiveMoveMutatedTiles = value; }
    private bool _syncingPlaceholderBlockMove { get => _dragState.SyncingPlaceholderBlockMove; set => _dragState.SyncingPlaceholderBlockMove = value; }
    private int _liveReorderSlideGeneration { get => _dragState.LiveReorderSlideGeneration; set => _dragState.LiveReorderSlideGeneration = value; }
    private IconInfo? highlightedFolderDropTarget { get => _dragState.HighlightedFolderDropTarget; set => _dragState.HighlightedFolderDropTarget = value; }
    private bool _folderDropCommittedByDropEvent { get => _dragState.FolderDropCommittedByDropEvent; set => _dragState.FolderDropCommittedByDropEvent = value; }

    // Sub-state delegates
    private LauncherSingleIconDragPreviewState _singleIconDragPreview => _dragState.SingleIconPreview;
    private LauncherCrossGroupPreviewCoordinator<MenuInfo, WorkspaceNavigationSnapshot, IconInfo> _crossGroupPreview => _dragState.CrossGroupPreview;
    private LauncherAllGroupsOrderState<IconInfo> _allGroupsOrder => _dragState.AllGroupsOrder;
    private LauncherFolderDropIntentState<IconInfo> _folderDropIntent => _dragState.FolderDropIntent;
    private LauncherPointerCacheState _dragPointerCache => _dragState.PointerCache;
    private LauncherBatchPlaceholderCoordinator _batchDragPlaceholders => _dragState.BatchPlaceholders;
    private LauncherBatchDragSessionState _batchDragState => _dragState.BatchDrag;
    private bool _draggedIconsSuppressedFromTiles { get => _dragState.IsDraggedIconsSuppressedFromTiles; set => _dragState.IsDraggedIconsSuppressedFromTiles = value; }
    private int _batchDragInsertIndex { get => _dragState.BatchDragInsertIndex; set => _dragState.BatchDragInsertIndex = value; }
    private int _batchDragSourceInsertIndex { get => _dragState.BatchDragSourceInsertIndex; set => _dragState.BatchDragSourceInsertIndex = value; }
    private List<IconInfo> _batchDragYieldPlaceholders => _dragState.BatchDragYieldPlaceholders;
    private bool IsMergedBatchDragActive() => _dragState.IsMergedBatchDragActive;
    private bool IsYieldPlaceholderDragActive() => _dragState.IsYieldPlaceholderDragActive;

    // Cross-group overlay
    private ListView? CrossGroupPreviewOverlay => _crossGroupPreviewOverlayController.View;
    private BulkObservableCollection<IconInfo> CrossGroupPreviewTiles => _crossGroupPreviewOverlayController.Tiles;

    public LauncherDragBusinessController(
        LauncherDragPageContext ctx,
        LauncherDragSessionState dragState,
        LauncherDragPreviewController dragPreviewController,
        LauncherCrossGroupPreviewOverlayController crossGroupPreviewOverlayController,
        LauncherCrossGroupPreviewPlacementController<IconInfo, MenuInfo> crossGroupPreviewPlacement,
        LauncherGroupBarDragController<MenuInfo, IconInfo> groupBarDrag,
        LauncherCrossGroupDropViewCoordinator<IconInfo> crossGroupDropView,
        LauncherDragOrchestrator dragOrchestrator,
        ILauncherCommandHandler launcherCommands,
        IAppDataProvider dataProvider)
    {
        _ctx = ctx;
        _dragState = dragState;
        _dragPreviewController = dragPreviewController;
        _crossGroupPreviewOverlayController = crossGroupPreviewOverlayController;
        _crossGroupPreviewPlacement = crossGroupPreviewPlacement;
        _groupBarDrag = groupBarDrag;
        _crossGroupDropView = crossGroupDropView;
        _dragOrchestrator = dragOrchestrator;
        _launcherCommands = launcherCommands;
        _dataProvider = dataProvider;
    }

    // ── Utility / State Queries ───────────────────────────────────────

    public bool IsDraggedIcon(IconInfo? icon)
    {
        if (icon == null) return false;
        if (LauncherDragIconClassifier.IsDragYieldPlaceholder(icon) && _batchDragYieldPlaceholders.Contains(icon))
            return true;
        return _draggedIcons.Count > 0
            ? _draggedIcons.Contains(icon)
            : ReferenceEquals(icon, _draggedIcon);
    }

    public List<IconInfo> GetActiveDraggedIcons()
    {
        return _draggedIcons.Count > 0
            ? _draggedIcons
            : _draggedIcon != null
                ? new List<IconInfo> { _draggedIcon }
                : new List<IconInfo>();
    }

    public bool UsesManualLiveDragYieldAnimation()
    {
        return IsYieldPlaceholderDragActive()
            || (IsShowingAllGroups
                && _draggedIcons.Count == 1
                && !_draggedIconsSuppressedFromTiles);
    }

    public bool IsAllGroupsShortcutView()
    {
        return IsShowingAllGroups || _crossGroupPreview.SavedSelection.IsAllGroups;
    }

    public List<IconInfo> GetValidDropIcons(IReadOnlyList<IconInfo> icons)
    {
        return ShortcutLayoutCalculator
            .FilterValidDragItems(icons, LauncherDragIconClassifier.IsAddShortcutPlaceholder)
            .ToList();
    }

    public bool CanDropIconsOnGroupTab(IReadOnlyList<IconInfo> icons, MenuInfo targetMenu,
        Func<IconInfo, MenuInfo?> findGroupContainingIcon)
    {
        if (targetMenu.IsAllGroupsTab_NoWrite || targetMenu.IsEncrypt || targetMenu.IconList == null)
            return false;

        List<IconInfo> validIcons = GetValidDropIcons(icons);
        if (validIcons.Count == 0) return false;

        return ShortcutLayoutCalculator.CanDropOnGroupTab(
            validIcons,
            targetMenu,
            IsAllGroupsShortcutView(),
            findGroupContainingIcon);
    }

    public void ClearAllGroupsDragOrderSnapshots()
    {
        _allGroupsOrder.ClearDragSnapshots();
    }

    public void ClearDraggedIconsState()
    {
        _dragState.DraggedIcons.Clear();
        _draggedIcon = null;
        _dragState.SingleIconPreview.Reset();
        _singleIconLiveMoveMutatedTiles = false;
        _folderDropCommittedByDropEvent = false;
        LauncherBatchPlaceholderCoordinator.RemoveOrphanPlaceholders(ShortcutTiles);
        _dragState.BatchDrag.Reset();
        _allGroupsOrder.ClearDragStart();
        ClearFolderDropTargetHighlight();
        _folderDropIntent.Reset();
        _dragPointerCache.Clear();
        _dragStartedFromFolderOverlay = false;
        _isCustomDragActive = false;
        _isCrossGroupDrag = false;
        _syncingPlaceholderBlockMove = false;
        _liveReorderSlideGeneration++;
        _draggedIconsSuppressedFromTiles = false;
        _batchDragInsertIndex = -1;
        _batchDragSourceInsertIndex = -1;
        _pendingBatchDropReleaseIcons = null;
        _lastLoggedStage = null;
    }

    public void ClearFolderDropTargetHighlight()
    {
        if (highlightedFolderDropTarget == null) return;
        if (IconListView != null)
            LauncherDropTargetVisualAdapter.Apply(IconListView, highlightedFolderDropTarget, active: false, MotionPreferences.ShouldAnimate);
        highlightedFolderDropTarget = null;
    }

    public void SetFolderDropTargetHighlight(IconInfo? folder)
    {
        if (ReferenceEquals(highlightedFolderDropTarget, folder)) return;

        ApplyFolderDropTargetVisualState(highlightedFolderDropTarget, active: false);
        highlightedFolderDropTarget = folder;
        ApplyFolderDropTargetVisualState(highlightedFolderDropTarget, active: true);
    }

    public void ApplyFolderDropTargetVisualState(IconInfo? target, bool active)
    {
        if (IconListView == null) return;

        LauncherDropTargetVisualAdapter.Apply(
            IconListView,
            target,
            active,
            MotionPreferences.ShouldAnimate);
    }

    public void SetFolderDropIntent(IconInfo? target)
    {
        bool isValid = target != null && IsValidFolderDropTarget(target);
        var update = _folderDropIntent.SetActive(target, isValid);
        if (update.ActiveTarget != null && IsValidFolderDropTarget(update.ActiveTarget))
            LogUtil.WriteLog($"FolderDrop intent: dragged={_draggedIcon?.Name_NoWrite}, target={update.ActiveTarget.Name_NoWrite}, targetIsFolder={update.ActiveTarget.IsFolder}, allGroups={IsShowingAllGroups}");
        if (IsFolderDropReorderSuppressed())
        {
            StopAllLiveReorderAnimations();
            _batchDragState.ResetMoveThrottle();
            Point? fdOrigin = null;
            if (update.ActiveTarget != null)
            {
                Canvas? fdOv = DragOverlayCanvas;
                if (fdOv != null && IconListView?.ContainerFromItem(update.ActiveTarget) is ListViewItem fdItem
                    && fdItem.ContentTemplateRoot is DependencyObject fdTr
                    && LauncherVisualTreeHelper.FindShortcutIconBorder(fdTr) is Border fdIb)
                    fdOrigin = fdIb.TransformToVisual(fdOv).TransformPoint(new Point(0, 0));
            }
            if (update.ActiveTargetChanged && update.ActiveTarget != null && _dragPreviewController.HasPreview && fdOrigin is Point fdVal)
                _dragPreviewController.AnimateToPlacement(fdVal.X, fdVal.Y, update.ActiveTarget.IsFolder ? 0.85d : 0.88d);
        }
        else
        {
            _dragPreviewController.ResumeFollowMode();
        }
        SetFolderDropTargetHighlight(update.ActiveTarget);
    }

    public bool IsValidFolderDropTarget(IconInfo? target)
    {
        if (target == null || !_isCustomDragActive) return false;
        if (LauncherDragIconClassifier.IsAddShortcutPlaceholder(target)) return false;
        if (_draggedIcons.Contains(target)) return false;
        if (target.IsFolder) return true;
        if (_draggedIcons.Count != 1) return false;
        return !IsShowingAllGroups || _dragState.AllGroupsOrder.HasCurrentOrder;
    }

    public void CommitCrossGroupPreviewState()
    {
        _crossGroupPreview.Commit();
        _crossGroupPreviewOverlayController.HidePreview(IconListView, WrapUFG as Grid);
        _ctx.IconListReorderSuppression.Restore(
            IconListView,
            LauncherIconListReorderSuppressionReason.CrossGroupPreview);
    }

    public void ResumeDragPreviewFollowIfNeeded()
    {
        if (!_isCustomDragActive || !_dragPreviewController.HasPreview) return;

        bool hasOverlayPos = _ctx.TryGetDragPointerOverlayPosition(out Point pos);
        if (hasOverlayPos)
        {
            _dragPreviewController.ExitPlacementFollowAt(pos.X, pos.Y);
        }
        else
        {
            _dragPreviewController.ResumeFollowMode();
        }
    }

    public void CompleteIconDragCleanup(bool restoreContainers)
    {
        ClearDraggedIconsState();
        _singleIconLiveMoveMutatedTiles = false;
        _dragSourceIndex = -1;
        _ctx.IconListReorderSuppression.Restore(
            IconListView,
            LauncherIconListReorderSuppressionReason.BatchDrag);
        _ctx.IconSelection.RestoreDragVisuals();
        if (restoreContainers)
        {
            _ctx.ShortcutContainers.Restore(ShortcutTiles);
        }
        _ctx.ShortcutTransitions.Restore();
        ClearAllGroupsDragOrderSnapshots();
        ClearFolderDropTargetHighlight();
        _ctx.RefreshIconListLayout();
    }

    public bool ExecuteLauncherCommand(LauncherMutationCommand command)
    {
        LauncherCommandResult result = _launcherCommands.Execute(command);
        return result.Success && result.Changed;
    }

    // ── Drag Animation Delegates ──────────────────────────────────────

    public void BeginShortcutItemSlide(
        ListViewItem item, FrameworkElement root,
        double dx, double dy, TimeSpan duration, TimeSpan beginTime, EasingFunctionBase easing)
    {
        _ctx.ItemAnimations.Slide(item, root, dx, dy, duration, beginTime, easing);
    }

    public void BeginShortcutItemFlyPlacement(
        ListViewItem item, FrameworkElement root,
        double dx, double dy, TimeSpan beginTime, EasingFunctionBase easing)
    {
        _ctx.ItemAnimations.FlyPlacement(
            item, root, dx, dy, beginTime, easing,
            LauncherDragPageContext.ShortcutLayoutFlyAnimationDuration,
            TimeSpan.FromMilliseconds(150),
            _ctx.GetShortcutItemCardWidth(),
            _ctx.GetShortcutItemSlotHeight());
    }

    public void BeginShortcutItemPreviewOffset(
        ListViewItem item, FrameworkElement root,
        double dx, double dy, TimeSpan duration, TimeSpan beginTime, EasingFunctionBase easing)
    {
        _ctx.ItemAnimations.PreviewOffset(item, root, dx, dy, duration, beginTime, easing);
    }

    public void StopItemReorderAnimation(ListViewItem item, string reason = "unknown")
    {
        _ctx.ItemAnimations.Stop(item);
    }

    public void StopAllLiveReorderAnimations()
    {
        _ctx.ItemAnimations.StopAll();
    }

    // ── Drag Start ───────────────────────────────────────────────────

    public LauncherDragStartResult TryBeginIconDragStartingCore(
        IReadOnlyList<object> selectedItems,
        Func<IconInfo, ListViewItem?> getContainer,
        out LauncherDragPreviewRequest? previewRequest)
    {
        previewRequest = null;
        var result = new LauncherDragStartResult();

        foreach (object item in selectedItems)
        {
            if (item is IconInfo icon && icon.IsAddShortcut_NoWrite)
            {
                result.Cancel = true;
                return result;
            }
        }

        _draggedIcons.Clear();
        foreach (object item in selectedItems)
        {
            if (item is IconInfo icon && !LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon))
            {
                _draggedIcons.Add(icon);
            }
        }

        if (_draggedIcons.Count == 0)
        {
            result.Cancel = true;
            return result;
        }

        _draggedIcon = _draggedIcons[0];
        _ctx.IconSelection.EnsurePrimaryDragSelection(_draggedIcon, _draggedIcons.Count);
        _dragSourceIndex = ShortcutTiles.IndexOf(_draggedIcon);
        _singleIconDragPreview.Begin(_dragSourceIndex);
        _batchDragState.ResetMoveThrottle();
        _liveReorderSlideGeneration++;
        _folderDropCommittedByDropEvent = false;
        _isCustomDragActive = true;
        _isCrossGroupDrag = false;

        if (IsShowingAllGroups)
        {
            _allGroupsOrder.CaptureDragStart(GetOrderedIconsFromTiles());
        }

        _ctx.SuppressGroupTabReorderForIconDrag();
        _ctx.ShortcutTransitions.Apply();
        StopAllLiveReorderAnimations();

        result.DragType = "IconDrag";
        result.DraggedIcon = _draggedIcon;
        result.DragCount = _draggedIcons.Count;

        _ctx.IconSelection.PrepareDragVisuals(_draggedIcons);

        ImageUtil.EnsureIconBitmap(_draggedIcon);
        double captureWidth = LauncherDragPageContext.LauncherShortcutDragPreviewSize;
        double captureHeight = LauncherDragPageContext.LauncherShortcutDragPreviewSize;
        bool animatePreview = !AppData.AppConfig.PMModel;
        var overlay = DragOverlayCanvas;
        ImageSource? iconSource = _draggedIcon.BitmapImage_NoWrite ?? _draggedIcon.BitmapImage;

        ListViewItem? primaryContainer = getContainer(_draggedIcon);
        FrameworkElement? captureTarget = primaryContainer != null
            ? LauncherVisualTreeHelper.FindShortcutDragCaptureTarget(primaryContainer)
            : null;

        var dragPolicy = ShortcutDragPolicy.ForDragCount(_draggedIcons.Count);
        bool useBatchPlaceholderDrag = dragPolicy.UseBatchPlaceholderDrag;
        bool useDragOverlayPreview = overlay != null
            && (_draggedIcons.Count == 1 || iconSource != null || captureTarget != null);

        if (useBatchPlaceholderDrag)
        {
            _ctx.IconListReorderSuppression.Suppress(
                IconListView,
                LauncherIconListReorderSuppressionReason.BatchDrag);
            _dragOrchestrator.RegisterSystemDragVisualSuppression(
                IconListView,
                WindowContent);
            result.NeedsBatchLayoutUpdate = true;
            result.BatchDragIcons = _draggedIcons.ToList();
            _batchDragPlaceholders.TryBegin(ShortcutTiles, _draggedIcons);
            if (dragPolicy.RestrictDragItemsToPlaceholder)
            {
                result.RestrictDragItemsToPlaceholder = true;
            }
            _ctx.ShortcutTransitions.Apply();
            result.NeedsPostBatchLayoutUpdate = true;
        }

        if (useDragOverlayPreview || useBatchPlaceholderDrag)
        {
            ScheduleHideDraggedShortcutContainers();
        }

        _ctx.BeginDragPointerTracking(useDragOverlayPreview || useBatchPlaceholderDrag);

        if (useDragOverlayPreview)
        {
            _dragPreviewController.End(animate: false);
            result.NeedsCaptureTargetLayout = true;
            previewRequest = new LauncherDragPreviewRequest
            {
                IconSource = iconSource,
                CaptureTarget = captureTarget,
                Overlay = overlay!,
                CaptureWidth = captureWidth,
                CaptureHeight = captureHeight,
                AnimatePreview = animatePreview,
                DragCount = _draggedIcons.Count
            };
        }

        LogIconDragState("started");
        return result;
    }

    // ── Hit Test / Pointer ────────────────────────────────────────────

    public Point? GetIconContainerWindowOrigin(IconInfo icon)
    {
        if (WindowContent is not UIElement root) return null;
        if (IconListView?.ContainerFromItem(icon) is not ListViewItem item) return null;
        if (item.ContentTemplateRoot is DependencyObject templateRoot)
        {
            Border? iconBorder = LauncherVisualTreeHelper.FindShortcutIconBorder(templateRoot);
            if (iconBorder != null) return iconBorder.TransformToVisual(root).TransformPoint(new Point(0, 0));
        }
        return item.TransformToVisual(root).TransformPoint(new Point(0, 0));
    }

    public Point GetShortcutContainerPoint(ListViewItem item)
    {
        if (WrapUFG is not FrameworkElement host) return new Point(0, 0);
        return item.TransformToVisual(host).TransformPoint(new Point(0, 0));
    }

    // GetShortcutDropIndexFromPointer remains on the page due to complex hit-test dependencies

    public int GetTileInsertUpperBound(IReadOnlyList<IconInfo> tiles)
    {
        if (tiles.Count == 0) return 0;
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (LauncherDragIconClassifier.IsAddShortcutPlaceholder(tiles[i]))
                return Math.Max(0, i - 1);
        }
        return tiles.Count - 1;
    }

    // ── Single Icon Live Reorder ──────────────────────────────────────

    public void StartLiveYieldFromPositions(
        IReadOnlyDictionary<IconInfo, Point> previousPositions,
        int fromIndex, int toIndex, int generation, bool isRetry = false)
    {
        if (generation != _liveReorderSlideGeneration || IconListView == null) return;

        // 性能优化：不调用 UpdateLayout()，直接使用已知的 previousPositions 计算偏移。
        // 仅在重试路径（容器未实现时）通过 ScrollIntoView 触发容器创建，
        // 然后用 TryEnqueue 延迟到下一帧再计算偏移（此时布局已完成）。
        if (isRetry)
        {
            int min = Math.Min(fromIndex, toIndex);
            int max = Math.Max(fromIndex, toIndex);
            void ScrollIconAt(int index)
            {
                if (index < 0 || index >= ShortcutTiles.Count) return;
                IconInfo icon = ShortcutTiles[index];
                if (LauncherDragIconClassifier.IsDragYieldPlaceholder(icon) || IsDraggedIcon(icon)) return;
                try { IconListView.ScrollIntoView(icon, ScrollIntoViewAlignment.Default); } catch { }
            }
            ScrollIconAt(min);
            ScrollIconAt(max);
            // 不调用 UpdateLayout() — 延迟到下一帧
            _ctx.DispatcherQueue.TryEnqueue(() =>
            {
                if (generation != _liveReorderSlideGeneration) return;
                for (int i = min; i <= max; i++) ScrollIconAt(i);
                ApplyYieldAnimations(previousPositions, generation);
            });
            return;
        }

        ApplyYieldAnimations(previousPositions, generation);
    }

    private void ApplyYieldAnimations(
        IReadOnlyDictionary<IconInfo, Point> previousPositions,
        int generation)
    {
        if (generation != _liveReorderSlideGeneration || IconListView == null) return;

        int started = 0;
        if (previousPositions.Count > 0 && !AppData.AppConfig.PMModel)
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            double riseBoost = Math.Min(LauncherDragPageContext.ShortcutReorderYieldRiseOffset, _ctx.GetShortcutItemSlotHeight() * 0.14d);
            var orderedIcons = previousPositions.Keys
                .Select(icon => (icon, index: ShortcutTiles.IndexOf(icon)))
                .Where(x => x.index >= 0)
                .OrderByDescending(x => x.index)
                .ToList();
            for (int step = 0; step < orderedIcons.Count; step++)
            {
                IconInfo icon = orderedIcons[step].icon;
                if (IconListView.ContainerFromItem(icon) is not ListViewItem item
                    || item == _draggedContainer
                    || item.ContentTemplateRoot is not FrameworkElement root)
                    continue;
                Point newPoint = GetShortcutContainerPoint(item);
                if (!previousPositions.TryGetValue(icon, out Point oldPoint)) continue;
                double dx = oldPoint.X - newPoint.X;
                double dy = oldPoint.Y - newPoint.Y;
                if (Math.Abs(dx) < 0.5d && Math.Abs(dy) < 0.5d) continue;
                if (dy > 0d) dy += riseBoost;
                TimeSpan beginTime = TimeSpan.FromMilliseconds(Math.Min(step, 8) * LauncherDragPageContext.LiveDragReorderStaggerDelay.TotalMilliseconds);
                BeginShortcutItemSlide(item, root, dx, dy, LauncherDragPageContext.LiveDragReorderDuration, beginTime, easing);
                started++;
            }
        }

        if (started > 0) return;
        _ctx.DispatcherQueue.TryEnqueue(() =>
            StartLiveYieldFromPositions(previousPositions, 0, 0, generation, isRetry: true));
    }

    // ── Collection Change Handler ─────────────────────────────────────

    public void HandleShortcutTilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // P2: 集合变更时使布局缓存失效
        InvalidateLayoutCache();

        if (_draggedIconsSuppressedFromTiles
            && _isCustomDragActive
            && !_isCrossGroupDrag
            && !_syncingPlaceholderBlockMove)
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
                SyncPlaceholderBlockOnCollectionMove(e.OldStartingIndex, e.NewStartingIndex, e.NewItems);
            _batchDragState.UpdateInsertIndex(ShortcutTiles);
        }

        if (!_isCustomDragActive
            || _isCrossGroupDrag
            || e.Action != NotifyCollectionChangedAction.Move)
            return;
    }

    private void SyncPlaceholderBlockOnCollectionMove(int oldIndex, int newIndex, System.Collections.IList? newItems)
    {
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;
        if (!IsMergedBatchDragActive())
        {
            _batchDragInsertIndex = newIndex;
            return;
        }

        IconInfo? movedPlaceholder = newItems?.OfType<IconInfo>().FirstOrDefault(LauncherDragIconClassifier.IsDragYieldPlaceholder);
        if (movedPlaceholder == null
            && newIndex >= 0
            && newIndex < ShortcutTiles.Count
            && LauncherDragIconClassifier.IsDragYieldPlaceholder(ShortcutTiles[newIndex]))
        {
            movedPlaceholder = ShortcutTiles[newIndex];
        }

        if (movedPlaceholder == null || !_batchDragYieldPlaceholders.Contains(movedPlaceholder)) return;
        _batchDragInsertIndex = ShortcutTiles.IndexOf(movedPlaceholder);
    }

    // ── Batch Placeholder Management ──────────────────────────────────

    public void EnsureBatchDragPlaceholderContainerVisible(int attempt = 0)
    {
        if (!IsYieldPlaceholderDragActive() || _batchDragYieldPlaceholders.Count == 0 || IconListView == null)
            return;

        bool foundVisiblePlaceholder = false;
        foreach (IconInfo placeholder in _batchDragYieldPlaceholders)
        {
            if (IconListView.ContainerFromItem(placeholder) is not ListViewItem container) continue;
            ApplyDraggedItemContainerState(container, placeholder);
            foundVisiblePlaceholder = true;
        }

        if (foundVisiblePlaceholder) return;

        IconInfo firstPlaceholder = _batchDragYieldPlaceholders[0];
        try { IconListView.ScrollIntoView(firstPlaceholder, ScrollIntoViewAlignment.Leading); } catch { }
        IconListView.UpdateLayout();

        foundVisiblePlaceholder = false;
        foreach (IconInfo placeholder in _batchDragYieldPlaceholders)
        {
            if (IconListView.ContainerFromItem(placeholder) is not ListViewItem createdContainer) continue;
            ApplyDraggedItemContainerState(createdContainer, placeholder);
            foundVisiblePlaceholder = true;
        }

        if (foundVisiblePlaceholder) return;
        if (attempt < 3)
            _ctx.DispatcherQueue.TryEnqueue(() => EnsureBatchDragPlaceholderContainerVisible(attempt + 1));
    }

    public bool MoveBatchDragPlaceholder(int targetIndex, string logReason)
    {
        if (!IsYieldPlaceholderDragActive() || _batchDragYieldPlaceholders.Count == 0) return false;

        if (_crossGroupPreviewOverlayController.IsVisible)
        {
            if (CrossGroupPreviewOverlay == null || _batchDragYieldPlaceholders.Count == 0) return false;
            IconInfo ovPh = _batchDragYieldPlaceholders[0];
            int ovCur = CrossGroupPreviewTiles.IndexOf(ovPh);
            int ovSafe = Math.Clamp(targetIndex, 0, Math.Max(0, GetTileInsertUpperBound(CrossGroupPreviewTiles)));
            if (ovCur < 0 || ovCur == ovSafe) return false;
            var ovMoved = ShortcutLayoutCalculator.MovePlaceholderBlock(CrossGroupPreviewTiles, _batchDragYieldPlaceholders, ovPh, ovSafe);
            // 性能优化：使用 SynchronizeWith 替代 ReplaceAll，发送增量 Move 事件而非 Reset，
            // 保留已实现的容器避免整个 ListView 重建。
            CrossGroupPreviewTiles.SynchronizeWith(ovMoved);
            _batchDragInsertIndex = CrossGroupPreviewTiles.IndexOf(ovPh);
            _ctx.LayoutApply.ApplyToListView(CrossGroupPreviewOverlay);
            ApplyOverlayItemChrome();
            return true;
        }

        IconInfo placeholder = _batchDragYieldPlaceholders[0];
        int currentIndex = ShortcutTiles.IndexOf(placeholder);
        int safeTargetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, ShortcutTiles.Count - 1));
        if (currentIndex < 0 || currentIndex == safeTargetIndex) return false;

        _syncingPlaceholderBlockMove = true;
        try
        {
            int moveFrom = currentIndex;
            int moveTo = safeTargetIndex;
            Dictionary<IconInfo, Point>? positionsBeforeMove = null;
            if (ShouldAnimateInteraction() && IconListView != null)
            {
                positionsBeforeMove = new Dictionary<IconInfo, Point>();
                int cMin = Math.Min(moveFrom, moveTo);
                int cMax = Math.Max(moveFrom, moveTo);
                for (int ci = cMin; ci <= cMax; ci++)
                {
                    if (ci == moveTo || ci >= ShortcutTiles.Count) continue;
                    IconInfo cIcon = ShortcutTiles[ci];
                    if (LauncherDragIconClassifier.IsDragYieldPlaceholder(cIcon) || IsDraggedIcon(cIcon)) continue;
                    ListViewItem? cItem = IconListView.ContainerFromItem(cIcon) as ListViewItem ?? IconListView.ContainerFromIndex(ci) as ListViewItem;
                    if (cItem != null) positionsBeforeMove[cIcon] = GetShortcutContainerPoint(cItem);
                }
            }

            var movedItems = ShortcutLayoutCalculator.MovePlaceholderBlock(
                ShortcutTiles, _batchDragYieldPlaceholders, placeholder, moveTo);
            // 性能优化：使用 SynchronizeWith 替代 ReplaceAll，发送增量 Move 事件而非 Reset，
            // 保留已实现的容器避免整个 ListView 重建。
            ShortcutTiles.SynchronizeWith(movedItems);
            _batchDragState.RecordCommittedMove(moveFrom, ShortcutTiles.IndexOf(placeholder), Environment.TickCount64);

            if (positionsBeforeMove is { Count: > 0 })
            {
                int gen = ++_liveReorderSlideGeneration;
                _ctx.DispatcherQueue.TryEnqueue(() =>
                    StartLiveYieldFromPositions(positionsBeforeMove, moveFrom, moveTo, gen));
            }

            return true;
        }
        finally
        {
            _syncingPlaceholderBlockMove = false;
        }
    }

    public void FinalizeBatchPlaceholderBlockPosition(string logReason)
    {
        if (!IsYieldPlaceholderDragActive() || _batchDragYieldPlaceholders.Count == 0) return;

        int upperBound = _crossGroupPreviewOverlayController.IsVisible
            ? GetTileInsertUpperBound(CrossGroupPreviewTiles)
            : GetTileInsertUpperBound(ShortcutTiles);
        int targetIndex = Math.Clamp(_ctx.GetShortcutDropIndexFromPointer(), 0, upperBound);

        if (targetIndex == _batchDragInsertIndex) return;
        MoveBatchDragPlaceholder(targetIndex, logReason);
    }

    public void TryUpdateBatchPlaceholderBlockFromPointer()
    {
        if (!IsYieldPlaceholderDragActive()
            || _batchDragYieldPlaceholders.Count == 0
            || _isCrossGroupDrag
            || IconListView == null)
            return;

        int upperBound = _crossGroupPreviewOverlayController.IsVisible
            ? GetTileInsertUpperBound(CrossGroupPreviewTiles)
            : GetTileInsertUpperBound(ShortcutTiles);
        int targetIndex = Math.Clamp(_ctx.GetShortcutDropIndexFromPointer(), 0, upperBound);
        if (!_batchDragState.ShouldCommitTargetMove(targetIndex, Environment.TickCount64, LauncherDragPageContext.BatchDragMoveThrottleMs)) return;

        if (!MoveBatchDragPlaceholder(targetIndex, _crossGroupPreview.IsActive
            ? "cross-group placeholder moved from pointer"
            : "placeholder moved from pointer"))
            return;
    }

    // ── Drag Preview ──────────────────────────────────────────────────

    public void UpdateDragPreviewPosition(double x, double y)
    {
        if (!_dragPreviewController.HasPreview) return;

        if (_dragPreviewController.IsPlacementPreviewActive)
        {
            if (IsFolderDropReorderSuppressed())
            {
                return;
            }

            if (_crossGroupPreview.IsActive)
            {
                return;
            }

            _dragPreviewController.ExitPlacementFollowAt(x, y);
            return;
        }

        _dragPreviewController.UpdatePosition(x, y);
    }

    public static BitmapImage GetEmptyDragBitmapImage()
    {
        if (_emptyDragBitmapImage != null) return _emptyDragBitmapImage;

        byte[] png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var stream = new InMemoryRandomAccessStream();
        using (var output = stream.GetOutputStreamAt(0))
        using (var writer = new DataWriter(output))
        {
            writer.WriteBytes(png);
            writer.StoreAsync().AsTask().GetAwaiter().GetResult();
            writer.FlushAsync().AsTask().GetAwaiter().GetResult();
        }

        stream.Seek(0);
        var bitmap = new BitmapImage();
        bitmap.SetSource(stream);
        _emptyDragBitmapImage = bitmap;
        return _emptyDragBitmapImage;
    }

    // ── Overlay Item Chrome ───────────────────────────────────────────

    private const double CrossGroupPreviewIconOpacity = 0.82d;

    public void ApplyOverlayItemChrome()
    {
        if (CrossGroupPreviewOverlay == null) return;

        double slotWidth = _ctx.GetShortcutItemSlotWidth();
        double slotHeight = _ctx.GetShortcutItemSlotHeight();
        if (slotWidth <= 0) slotWidth = 88;
        if (slotHeight <= 0) slotHeight = 104;
        int count = CrossGroupPreviewTiles.Count;
        for (int i = 0; i < count; i++)
        {
            if (CrossGroupPreviewOverlay.ContainerFromIndex(i) is not ListViewItem item
                || CrossGroupPreviewTiles[i] is not IconInfo icon)
            {
                continue;
            }

            item.Opacity = 1;
            item.Visibility = Visibility.Visible;
            item.IsHitTestVisible = false;
            LauncherShortcutItemSpanAdapter.ApplyMargin(item, i, count);

            if (item.ContentTemplateRoot is not FrameworkElement root)
            {
                continue;
            }

            root.Width = slotWidth;
            root.Height = slotHeight;
            root.Opacity = 1;
            root.Visibility = Visibility.Visible;
            root.IsHitTestVisible = false;

            if (LauncherDragIconClassifier.IsDataShortcutIcon(icon)
                && LauncherVisualTreeHelper.FindDescendant<Image>(root) is Image image
                && (icon.BitmapImage_NoWrite ?? icon.BitmapImage) is ImageSource source)
            {
                image.Source = source;
                image.Visibility = Visibility.Visible;
                image.Opacity = CrossGroupPreviewIconOpacity;
            }
        }
    }

    // ── Container State ───────────────────────────────────────────────

    public void ApplyDraggedItemContainerState(ListViewItem item, IconInfo? icon)
    {
        if (_isCustomDragActive && IsDraggedIcon(icon))
        {
            if (LauncherDragIconClassifier.IsDragYieldPlaceholder(icon))
            {
                item.Opacity = 1;
                item.Visibility = Visibility.Visible;
                item.IsHitTestVisible = false;
                if (item.ContentTemplateRoot is UIElement placeholderRoot)
                {
                    placeholderRoot.Opacity = 1;
                    placeholderRoot.Visibility = Visibility.Visible;
                    placeholderRoot.IsHitTestVisible = false;
                }

                return;
            }

            if (_crossGroupPreview.IsActive)
            {
                item.Opacity = 0;
                if (item.ContentTemplateRoot is UIElement hiddenRoot)
                {
                    hiddenRoot.Opacity = 0;
                }

                _draggedContainer = item;
                return;
            }

            bool keepLayoutSlot = _draggedIconsSuppressedFromTiles
                || _dragPreviewController.HasPreview
                || (_draggedIcons.Count == 1 && !_draggedIconsSuppressedFromTiles);
            item.Opacity = 0;
            item.Visibility = keepLayoutSlot ? Visibility.Visible : Visibility.Collapsed;
            item.IsHitTestVisible = false;
            if (item.ContentTemplateRoot is UIElement root)
            {
                root.Opacity = 0;
                root.Visibility = keepLayoutSlot ? Visibility.Visible : Visibility.Collapsed;
                root.IsHitTestVisible = false;
            }

            _draggedContainer = item;
            return;
        }

        item.IsHitTestVisible = true;
        if (item.Visibility != Visibility.Visible)
        {
            item.Visibility = Visibility.Visible;
        }
        if (item.ContentTemplateRoot is UIElement templateRoot)
        {
            if (templateRoot.Visibility != Visibility.Visible)
            {
                templateRoot.Visibility = Visibility.Visible;
            }

            if (templateRoot.Opacity < 1d)
            {
                templateRoot.Opacity = 1;
            }

            templateRoot.IsHitTestVisible = true;
        }

        if (item.Opacity < 1d)
        {
            item.Opacity = 1;
        }
    }

    public void HideDraggedShortcutContainers()
    {
        if (IconListView == null) return;

        if (_draggedIconsSuppressedFromTiles)
        {
            foreach (IconInfo placeholder in _batchDragYieldPlaceholders)
            {
                if (IconListView.ContainerFromItem(placeholder) is ListViewItem container)
                {
                    ApplyDraggedItemContainerState(container, placeholder);
                }
            }
        }

        foreach (IconInfo icon in _draggedIcons)
        {
            if (IconListView.ContainerFromItem(icon) is ListViewItem container)
            {
                ApplyDraggedItemContainerState(container, icon);
            }
        }
    }

    // ── Container Callback Support ───────────────────────────────────

    /// <summary>
    /// Resets a recycled container to default state. Returns true if the container
    /// is a dragged container and should NOT be reset.
    /// </summary>
    public bool TryResetRecycledContainer(ListViewItem recycledItem)
    {
        if (!IsYieldPlaceholderDragActive())
        {
            _ctx.ItemAnimations.Stop(recycledItem);
        }
        bool isDraggedContainer = _isCustomDragActive
            && (ReferenceEquals(recycledItem, _draggedContainer)
                || recycledItem.Content is IconInfo recycledIcon
                    && IsDraggedIcon(recycledIcon));
        if (!isDraggedContainer)
        {
            recycledItem.Opacity = 1;
            if (recycledItem.ContentTemplateRoot is UIElement templateRoot)
            {
                templateRoot.Opacity = 1;
                templateRoot.Visibility = Visibility.Visible;
            }
            recycledItem.ClearValue(VariableSizedWrapGrid.ColumnSpanProperty);
            recycledItem.ClearValue(VariableSizedWrapGrid.RowSpanProperty);
            recycledItem.ClearValue(FrameworkElement.WidthProperty);
            recycledItem.ClearValue(FrameworkElement.HeightProperty);
            recycledItem.RenderTransform = null;
            if (recycledItem.ContentTemplateRoot is FrameworkElement recycledRoot)
            {
                LauncherShortcutItemSpanAdapter.ResetTemplate(recycledRoot);
            }
        }
        return isDraggedContainer;
    }

    /// <summary>
    /// Applies span, margin, and folder tile visual adapters to a container.
    /// </summary>
    public void ApplyContainerSpans(ListViewItem item, IconInfo boundIcon, int itemIndex)
    {
        if (IconListView == null) return;
        ApplyDraggedItemContainerState(item, boundIcon);
        double availableWidth = _ctx.GetShortcutAvailableWidth();
        int columnCount = _ctx.GetShortcutColumnCount();
        LauncherShortcutItemSpanAdapter.Apply(
            IconListView,
            item,
            boundIcon,
            columnCount,
            availableWidth);
        if (boundIcon.IsFolder)
        {
            _ctx.DispatcherQueue.TryEnqueue(() => LauncherFolderTileVisualAdapter.Apply(
                IconListView,
                item,
                boundIcon,
                columnCount,
                availableWidth));
        }

        if (itemIndex >= 0)
        {
            LauncherShortcutItemSpanAdapter.ApplyMargin(
                item,
                itemIndex,
                ShortcutTiles.Count);
        }
    }

    /// <summary>
    /// Ensures icon bitmaps are loaded for an icon and its folder children.
    /// </summary>
    public void EnsureIconBitmaps(IconInfo icon)
    {
        ImageUtil.EnsureIconBitmap(icon);
        if (icon.IsFolder)
        {
            foreach (IconInfo child in icon.FolderItems.Take(6))
            {
                ImageUtil.EnsureIconBitmap(child);
            }
        }
    }

    /// <summary>
    /// Refreshes icon list layout by resetting container update duration and invalidating panel.
    /// </summary>
    public void RefreshIconListLayout()
    {
        if (IconListView == null) return;

        IconListView.SetDesiredContainerUpdateDuration(TimeSpan.Zero);
        IconListView.UpdateLayout();

        LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView.ItemsPanelRoot);

        _ctx.DispatcherQueue.TryEnqueue(() =>
        {
            IconListView.SetDesiredContainerUpdateDuration(TimeSpan.FromMilliseconds(200));
        });
    }

    /// <summary>
    /// Refreshes shortcut item spans by iterating containers and applying span/folder tile adapters.
    /// </summary>
    public void RefreshShortcutItemSpans()
    {
        if (IconListView == null) return;

        IconListView.UpdateLayout();
        double availableWidth = _ctx.GetShortcutAvailableWidth();
        int columnCount = _ctx.GetShortcutColumnCount();
        for (int i = 0; i < ShortcutTiles.Count; i++)
        {
            IconInfo icon = ShortcutTiles[i];
            if (IconListView.ContainerFromItem(icon) is not ListViewItem item)
            {
                continue;
            }

            LauncherShortcutItemSpanAdapter.Apply(
                IconListView,
                item,
                icon,
                columnCount,
                availableWidth);
            if (icon.IsFolder)
            {
                LauncherFolderTileVisualAdapter.Apply(
                    IconListView,
                    item,
                    icon,
                    columnCount,
                    availableWidth);
            }
        }

        LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView.ItemsPanelRoot);

        IconListView.UpdateLayout();
    }

    /// <summary>
    /// Schedules a deferred container restore via DispatcherQueue.
    /// </summary>
    public void ScheduleDeferredShortcutContainerRestore()
    {
        _ctx.DispatcherQueue.TryEnqueue(() =>
        {
            if (_isCustomDragActive) return;

            IconListView?.UpdateLayout();
            _ctx.ShortcutContainers.Restore(ShortcutTiles);
        });
    }

    /// <summary>
    /// Restores visible shortcut container state.
    /// </summary>
    public void RestoreVisibleShortcutContainerState()
    {
        _ctx.ShortcutContainers.Restore(ShortcutTiles);
    }

    /// <summary>
    /// Rebuilds shortcut tiles from current selection or All Groups projection.
    /// </summary>
    public void RebuildShortcutTiles()
    {
        var tiles = new List<IconInfo>();

        if (IsShowingAllGroups)
        {
            tiles.AddRange(BuildAllGroupsFlatOrderTiles());
        }
        else
        {
            var selectedIcons = _ctx.GetSelectedIcons();
            if (selectedIcons != null)
            {
                foreach (var icon in selectedIcons)
                {
                    if (icon != null && !LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon))
                    {
                        icon.IsAddShortcut_NoWrite = false;
                        tiles.Add(icon);
                    }
                }
            }
        }

        _ctx.LayoutApply.Compact(tiles);
        _ctx.ApplyShortcutTileSnapshot(tiles);

        _ctx.LayoutApply.Schedule();
    }

    /// <summary>
    /// Determines whether drop hit test layout sync should be skipped.
    /// </summary>
    public bool ShouldSkipDropHitTestLayoutSync()
    {
        return _isCustomDragActive
            && (IsMergedBatchDragActive() || UsesManualLiveDragYieldAnimation());
    }

    /// <summary>
    /// Builds a logical layout map for the given list view and tiles.
    /// P2: 使用缓存避免同一帧内多次构建完整 Dictionary 和网格排列结果。
    /// 缓存键：集合版本（Count）+ 列数 + 槽位尺寸 + 滚动原点。
    /// </summary>
    public Dictionary<IconInfo, LauncherGridRect> BuildCurrentLauncherLayoutMap(
        ListView listView,
        IReadOnlyList<IconInfo> tiles)
    {
        int columnCount = Math.Max(1, _ctx.GetShortcutColumnCount());
        double slotWidth = _ctx.GetShortcutItemSlotWidth() > 0d ? _ctx.GetShortcutItemSlotWidth() : 88d;
        double slotHeight = _ctx.GetShortcutItemSlotHeight() > 0d ? _ctx.GetShortcutItemSlotHeight() : 104d;

        // 计算滚动原点作为缓存键的一部分
        Point scrollOrigin = default;
        if (listView.ItemsPanelRoot is UIElement panel)
        {
            try { scrollOrigin = panel.TransformToVisual(listView).TransformPoint(default); } catch { }
        }
        bool usesManualPositions = listView.ItemsPanelRoot is not LauncherGridPanel launcherPanel
            || launcherPanel.UseManualPositions;

        // 检查缓存是否有效
        if (_cachedLayoutMap != null
            && _cachedLayoutCollectionVersion == tiles.Count
            && _cachedLayoutColumnCount == columnCount
            && Math.Abs(_cachedLayoutSlotWidth - slotWidth) < 0.01d
            && Math.Abs(_cachedLayoutSlotHeight - slotHeight) < 0.01d
            && _cachedLayoutUsesManualPositions == usesManualPositions
            && Math.Abs(_cachedLayoutScrollOrigin.X - scrollOrigin.X) < 0.5d
            && Math.Abs(_cachedLayoutScrollOrigin.Y - scrollOrigin.Y) < 0.5d)
        {
            return _cachedLayoutMap;
        }

        var layout = LauncherDropHitTestAdapter.BuildLogicalLayout(
            listView,
            tiles,
            new LauncherGridMetrics(columnCount, slotWidth, slotHeight),
            synchronizeLayout: !ShouldSkipDropHitTestLayoutSync(),
            include: LauncherDragIconClassifier.IsDataShortcutIcon);

        // 更新缓存
        _cachedLayoutMap = layout;
        _cachedLayoutCollectionVersion = tiles.Count;
        _cachedLayoutColumnCount = columnCount;
        _cachedLayoutSlotWidth = slotWidth;
        _cachedLayoutSlotHeight = slotHeight;
        _cachedLayoutUsesManualPositions = usesManualPositions;
        _cachedLayoutScrollOrigin = scrollOrigin;

        return layout;
    }

    /// <summary>
    /// Resolves the drop index from the current pointer position.
    /// </summary>
    public int GetShortcutDropIndexFromPointer(IconInfo? skipIcon)
    {
        ListView listView;
        IReadOnlyList<IconInfo> tiles;
        int tileLimit;
        if (_crossGroupPreviewOverlayController.IsVisible && CrossGroupPreviewOverlay != null)
        {
            listView = CrossGroupPreviewOverlay;
            tiles = CrossGroupPreviewTiles;
            tileLimit = GetTileInsertUpperBound(CrossGroupPreviewTiles);
        }
        else if (IconListView != null)
        {
            listView = IconListView;
            tiles = ShortcutTiles;
            tileLimit = GetTileInsertUpperBound(ShortcutTiles);
        }
        else
        {
            return GetTileInsertUpperBound(ShortcutTiles);
        }

        Point point = default;
        bool pointerResolved = _ctx.TryGetDragPointerOverlayPosition(out Point overlayPos)
            && DragOverlayCanvas is UIElement overlay
            && LauncherDropHitTestAdapter.TryTranslatePoint(overlay, listView, overlayPos, out point);
        if (!pointerResolved)
        {
            if (skipIcon != null && _singleIconDragPreview.TargetIndex >= 0)
                return Math.Clamp(_singleIconDragPreview.TargetIndex, 0, tileLimit);
            return _batchDragInsertIndex >= 0 ? _batchDragInsertIndex : tileLimit;
        }

        if (skipIcon == null && _batchDragYieldPlaceholders.Count > 0)
        {
            foreach (IconInfo placeholder in _batchDragYieldPlaceholders)
            {
                if (LauncherDropHitTestAdapter.IsPointInsideItem(listView, placeholder, point))
                {
                    int phIdx = -1;
                    for (int i = 0; i < tiles.Count; i++)
                        if (ReferenceEquals(tiles[i], placeholder)) { phIdx = i; break; }
                    return Math.Clamp(_batchDragInsertIndex >= 0 ? _batchDragInsertIndex : phIdx, 0, tileLimit);
                }
            }
        }

        Dictionary<IconInfo, LauncherGridRect> layout = BuildCurrentLauncherLayoutMap(listView, tiles);
        if (layout.Count > 0)
        {
            var excluded = new HashSet<IconInfo>();
            if (skipIcon != null) excluded.Add(skipIcon);
            foreach (var ph in _batchDragYieldPlaceholders) excluded.Add(ph);
            foreach (var di in _draggedIcons) excluded.Add(di);
            return Math.Clamp(LauncherDragSession.ResolveGridReorderIndex(tiles, layout, new LauncherPoint(point.X, point.Y), tileLimit, excluded), 0, tileLimit);
        }

        return LauncherDropHitTestAdapter.ResolveRealizedListDropIndex(listView, tiles, point, tileLimit,
            synchronizeLayout: !ShouldSkipDropHitTestLayoutSync(),
            exclude: icon => LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon) || (skipIcon != null && ReferenceEquals(icon, skipIcon)));
    }

    // ── Helper ────────────────────────────────────────────────────────

    private bool ShouldAnimateInteraction()
    {
        return !AppData.AppConfig.PMModel && MotionPreferences.ShouldAnimate;
    }

    public void SetDraggedContainer(ListViewItem? container) => _draggedContainer = container;

    public void ScheduleHideDraggedShortcutContainers()
    {
        if (!_draggedIconsSuppressedFromTiles && _draggedIcons.Count <= 1) return;

        HideDraggedShortcutContainers();
        EnsureBatchDragPlaceholderContainerVisible();
        _ctx.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isCustomDragActive) return;
            HideDraggedShortcutContainers();
            IconListView?.UpdateLayout();
            EnsureBatchDragPlaceholderContainerVisible();
        });
    }

    public void RestoreDraggedIconHiddenState()
    {
        if (!_isCustomDragActive || _draggedIcons.Count == 0) return;

        foreach (IconInfo icon in _draggedIcons)
        {
            if (IconListView?.ContainerFromItem(icon) is ListViewItem container)
            {
                ApplyDraggedItemContainerState(container, icon);
            }
        }

        if (_draggedContainer != null && _draggedIcon != null)
        {
            ApplyDraggedItemContainerState(_draggedContainer, _draggedIcon);
        }
    }

    public void EnsureDraggedIconsHiddenDuringActiveDrag()
    {
        if (!_isCustomDragActive) return;

        HideDraggedShortcutContainers();
        RestoreDraggedIconHiddenState();
        _ctx.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isCustomDragActive) return;
            IconListView?.UpdateLayout();
            HideDraggedShortcutContainers();
            RestoreDraggedIconHiddenState();
        });
    }

    public bool IsDragPointOverShortcutIconArea(DragEventArgs e)
    {
        ListView? host = _crossGroupPreviewOverlayController.IsVisible
            ? CrossGroupPreviewOverlay
            : IconListView;
        if (host == null || host.ActualWidth <= 0d || host.ActualHeight <= 0d)
            return false;

        Point pos;
        if (_ctx.TryGetDragPointerOverlayPosition(out Point overlayPos)
            && DragOverlayCanvas is UIElement overlay)
        {
            pos = overlay.TransformToVisual(host).TransformPoint(overlayPos);
        }
        else
        {
            pos = e.GetPosition(host);
        }

        return LauncherDropHitTestAdapter.IsPointInside(host, pos);
    }

    private string? _lastLoggedStage;

    public void LogIconDragState(string stage)
    {
        // 性能优化：仅在状态变化时记录，避免拖动热路径每帧产生日志
        if (string.Equals(stage, _lastLoggedStage, StringComparison.Ordinal)) return;
        _lastLoggedStage = stage;

        try
        {
            LogUtil.WriteLog(
                $"IconDrag {stage}: active={_isCustomDragActive}, " +
                $"dragged={_draggedIcon?.Name_NoWrite ?? "<null>"}, " +
                $"count={_draggedIcons.Count}, suppressed={_draggedIconsSuppressedFromTiles}, " +
                $"placeholders={_batchDragYieldPlaceholders.Count}, " +
                $"sourceIndex={_dragSourceIndex}, liveTarget={_singleIconDragPreview.TargetIndex}, " +
                $"folderTarget={_folderDropIntent.ActiveTarget?.Name_NoWrite ?? "<null>"}, " +
                $"lastValidFolderTarget={_folderDropIntent.LastValidTarget?.Name_NoWrite ?? "<null>"}, " +
                $"folderMode={IsFolderDropReorderSuppressed()}");
        }
        catch
        {
        }
    }

    public bool IsFolderDropReorderSuppressed() =>
        ShortcutDragPolicy.ShouldSuppressReorderForFolderTarget(
            _draggedIcons.Count,
            _folderDropIntent.ActiveTarget != null);

    // ── Cross-Group Preview / Drop ──────────────────────────────────

    public List<IconInfo> GetOrderedIconsFromTiles()
    {
        return ShortcutTiles
            .Where(icon => LauncherDragIconClassifier.IsDataShortcutIcon(icon))
            .ToList();
    }

    public List<IconInfo> BuildAllGroupsFlatOrderTiles()
    {
        IReadOnlyList<IconInfo>? preferredOrder = _allGroupsOrder.HasCurrentOrder
            ? _allGroupsOrder.CopyCurrentOrder()
            : null;
        List<IconInfo> tiles = LauncherAllGroupsProjectionBuilder.Build(
            AppData,
            preferredOrder);
        _allGroupsOrder.SetCurrent(tiles);
        return tiles;
    }

    public void ApplyAllGroupsFlatOrderToTiles(IReadOnlyList<IconInfo> flat)
    {
        ShortcutTiles.Clear();
        foreach (var icon in flat)
        {
            if (icon == null || LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon)) continue;
            icon.IsAddShortcut_NoWrite = false;
            ShortcutTiles.Add(icon);
        }

        _ctx.LayoutApply.Compact(ShortcutTiles, preserveExistingVisualOrder: false);
        _allGroupsOrder.SetCurrent(flat);
    }

    public void CommitAllGroupsProjection()
    {
        if (!IsShowingAllGroups) return;
        CommitCurrentShortcutOrder();
    }

    public void CommitCurrentShortcutOrder()
    {
        List<IconInfo> orderedIcons = GetOrderedIconsFromTiles();
        var ids = orderedIcons
            .Select(icon => icon.IconId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        if (ids.Count != orderedIcons.Count)
        {
            LogUtil.WriteLog("Launcher 顺序包含缺少稳定 ID 的项目，已拒绝提交。");
            return;
        }
        UpdateLauncherShortcutOrderCommand command;
        if (IsShowingAllGroups)
        {
            _allGroupsOrder.SetCurrent(orderedIcons);
            command = new UpdateLauncherShortcutOrderCommand(
                LauncherShortcutOrderScope.AllGroups,
                ids);
        }
        else
        {
            MenuInfo? group = _ctx.WorkspaceNavigation.GetActiveGroup(AppData);
            if (group == null || string.IsNullOrWhiteSpace(group.MenuId))
            {
                return;
            }
            command = new UpdateLauncherShortcutOrderCommand(
                LauncherShortcutOrderScope.Group,
                ids,
                group.MenuId);
        }

        LauncherCommandResult result = _launcherCommands.UpdateOrder(command);
        if (!result.Success)
        {
            LogUtil.WriteLog(result.Error ?? "Launcher 顺序更新失败。");
        }
    }

    public void ApplyCrossGroupPreview(MenuInfo targetMenu)
    {
        if (_draggedIconsSuppressedFromTiles)
        {
            if (targetMenu.IsEncrypt || targetMenu.IconList == null) return;
            IReadOnlyList<IconInfo> batchDragged = GetActiveDraggedIcons();
            if (batchDragged.Count == 0 || _batchDragYieldPlaceholders.Count == 0) return;
            var batchUpdate = _crossGroupPreview.Retarget(targetMenu, _ctx.WorkspaceNavigation.Capture(),
                targetMenu.IconList, batchDragged, LauncherDragIconClassifier.IsAddShortcutPlaceholder);
            if (batchUpdate.SessionStarted)
            {
                _batchDragSourceInsertIndex = _batchDragInsertIndex;
                if (IsShowingAllGroups) _allGroupsOrder.CaptureBeforePreview(GetOrderedIconsFromTiles());
                _ctx.IconListReorderSuppression.Suppress(
                    IconListView, LauncherIconListReorderSuppressionReason.CrossGroupPreview);
            }
            if (batchUpdate.TargetChanged)
            {
                var batchTiles = _batchDragPlaceholders.BuildTargetPreview(targetMenu.IconList, batchDragged, batchUpdate.TargetInsertIndex);
                _ctx.DispatcherQueue.TryEnqueue(() => ShowCrossGroupPreviewOverlay(batchTiles));
            }
            return;
        }

        if (targetMenu.IsEncrypt || targetMenu.IconList == null) return;

        IReadOnlyList<IconInfo> draggedIcons = GetActiveDraggedIcons();
        if (draggedIcons.Count == 0) return;

        LauncherCrossGroupPreviewUpdate<IconInfo> update =
            _crossGroupPreview.Retarget(
                targetMenu,
                _ctx.WorkspaceNavigation.Capture(),
                targetMenu.IconList,
                draggedIcons,
                LauncherDragIconClassifier.IsAddShortcutPlaceholder);
        if (update.SessionStarted)
        {
            if (IsShowingAllGroups)
            {
                _allGroupsOrder.CaptureBeforePreview(GetOrderedIconsFromTiles());
            }

            _ctx.IconListReorderSuppression.Suppress(
                IconListView,
                LauncherIconListReorderSuppressionReason.CrossGroupPreview);
        }

        if (!update.TargetChanged) return;

        _ctx.DispatcherQueue.TryEnqueue(() => ShowCrossGroupPreviewOverlay(update.Tiles.ToList()));
    }

    private void ShowCrossGroupPreviewOverlay(List<IconInfo> tiles)
    {
        if (!_crossGroupPreview.IsCurrent(_crossGroupPreview.Token) || IconListView == null) return;
        var host = WrapUFG as Grid ?? _ctx.GetWrapUFG() as Grid;
        if (host == null) return;
        _crossGroupPreviewOverlayController.ShowPreview(
            IconListView, host, tiles,
            LauncherShortcutItemSpanAdapter.ApplyMargin,
            LauncherDragIconClassifier.IsDataShortcutIcon,
            _ctx.LayoutApply.ApplyToListView,
            ApplyOverlayItemChrome,
            _ctx.DispatcherQueue,
            _crossGroupPreview.Token,
            _crossGroupPreview.IsCurrent,
            pt => _crossGroupPreviewPlacement.TryAnimatePlacement(
                pt, _crossGroupPreview.IsCurrent, _dragPreviewController.HasPreview,
                animationsAllowed: !AppData.AppConfig.PMModel,
                hideDraggedSourceItems: !_draggedIconsSuppressedFromTiles,
                animate: _dragPreviewController.AnimateToPlacement),
            CrossGroupPreviewIconOpacity);
    }

    public void RestoreCrossGroupPreviewView()
    {
        LauncherCrossGroupPreviewCancelPlan plan = _crossGroupPreview.Cancel(
            _draggedIconsSuppressedFromTiles,
            _isCustomDragActive,
            AppData.AppConfig.PMModel,
            _draggedIcon != null);
        if (plan.Mode == LauncherCrossGroupPreviewCancelMode.None) return;

        _crossGroupPreviewOverlayController.HidePreview(IconListView, WrapUFG as Grid);
        _ctx.IconListReorderSuppression.Restore(
            IconListView,
            LauncherIconListReorderSuppressionReason.CrossGroupPreview);
        _dragPreviewController.SetSuppressed(false);

        if (plan.Mode == LauncherCrossGroupPreviewCancelMode.PreserveBatchPlaceholders)
        {
            _groupBarDrag.ClearIndicators();
            EnsureDraggedIconsHiddenDuringActiveDrag();
            ResumeDragPreviewFollowIfNeeded();
            return;
        }

        if (plan.RestoreBatchItems)
        {
            RestoreDraggedIconsAfterBatchReorder(cancelled: true);
        }

        Dictionary<IconInfo, Point>? previousPositions = null;
        if (plan.CapturePreviousPositions && IconListView != null)
        {
            previousPositions = new Dictionary<IconInfo, Point>();
            IconListView.UpdateLayout();
            int cc = ShortcutTiles.Count;
            for (int i = 0; i < cc; i++)
            {
                try
                {
                    if (ShortcutTiles[i] != null && IconListView.ContainerFromItem(ShortcutTiles[i]) is ListViewItem cItem)
                        previousPositions[ShortcutTiles[i]] = GetShortcutContainerPoint(cItem);
                }
                catch (ArgumentOutOfRangeException) { break; }
            }
        }

        bool selectionRestored = _ctx.WorkspaceNavigation.Restore(AppData, _crossGroupPreview.SavedSelection);
        MenuInfo? activeMenu = _ctx.WorkspaceNavigation.GetActiveGroup(AppData);
        LauncherCrossGroupNavigationRestoreMode restoreMode =
            LauncherCrossGroupPreviewCompletionPolicy.ResolveNavigation(
                selectionRestored,
                IsShowingAllGroups,
                activeMenu != null);
        if (restoreMode == LauncherCrossGroupNavigationRestoreMode.RestoreAllGroups)
        {
            _ctx.IconSelection.ClearSelection();
            _ctx.WorkspaceNavigation.SelectAll(AppData);
            if (_allGroupsOrder.CancellationOrder is { Count: > 0 } flatOrder)
            {
                ApplyAllGroupsFlatOrderToTiles(flatOrder);
            }
            else
            {
                _ctx.RebuildShortcutTiles();
            }
        }
        else if (restoreMode == LauncherCrossGroupNavigationRestoreMode.RestoreActiveGroup
            && activeMenu != null)
        {
            _ctx.IconSelection.ClearSelection();
            _ctx.RebuildShortcutTiles();
        }
        else
        {
            _ctx.RebuildShortcutTiles();
        }

        _ctx.RefreshGroupSelectionState(_ctx.WorkspaceNavigation.GetActiveGroupIndex(AppData));

        if (plan.CapturePreviousPositions
            && !_isCustomDragActive
            && previousPositions is { Count: > 0 })
        {
            IconListView?.UpdateLayout();
            if (IconListView != null && previousPositions.Count > 0)
            {
                var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
                int vs = 0;
                for (int i = 0; i < ShortcutTiles.Count; i++)
                {
                    try
                    {
                        IconInfo icon = ShortcutTiles[i];
                        if (icon == null || (_isCustomDragActive && IsDraggedIcon(icon))) continue;
                        if (IconListView.ContainerFromItem(icon) is not ListViewItem item) continue;
                        if (item.ContentTemplateRoot is not FrameworkElement root) continue;
                        Point np = GetShortcutContainerPoint(item);
                        double dx = 0d; double dy = -10d;
                        if (previousPositions.TryGetValue(icon, out Point op))
                        {
                            dx = op.X - np.X; dy = op.Y - np.Y;
                            if (Math.Abs(dx) < 0.5d && Math.Abs(dy) < 0.5d) dy = -10d;
                        }
                        BeginShortcutItemFlyPlacement(item, root, dx, dy,
                            TimeSpan.FromMilliseconds(Math.Min(vs, 8) * LauncherDragPageContext.ShortcutReorderStaggerDelay.TotalMilliseconds), easing);
                        vs++;
                    }
                    catch (ArgumentOutOfRangeException) { break; }
                }
                if (vs == 0) _ctx.ShortcutTransitions.Restore();
                else _ctx.ShortcutLayoutScheduler.ScheduleTransitionRestore(
                    LauncherDragPageContext.ShortcutLayoutFlyAnimationDuration
                    + TimeSpan.FromMilliseconds(Math.Min(vs - 1, 8) * LauncherDragPageContext.ShortcutReorderStaggerDelay.TotalMilliseconds)
                    + TimeSpan.FromMilliseconds(40));
            }
        }

        _ctx.RefreshIconListLayout();
        _ctx.ShortcutContainers.Restore(ShortcutTiles);
        EnsureDraggedIconsHiddenDuringActiveDrag();

        if (plan.RecommitSingleIconTarget && _draggedIcon != null)
        {
            _singleIconDragPreview.CommitTarget(Math.Clamp(
                ShortcutTiles.IndexOf(_draggedIcon),
                0,
                Math.Max(0, ShortcutTiles.Count - 1)));
        }
        ResumeDragPreviewFollowIfNeeded();
    }

    public bool RestoreDraggedIconsAfterBatchReorder(bool cancelled = false)
    {
        LauncherBatchRestorePlan? restorePlan =
            _batchDragPlaceholders.PlanRestore(ShortcutTiles, cancelled);
        if (restorePlan == null) return false;

        if (cancelled)
        {
            _batchDragPlaceholders.ApplyRestore(ShortcutTiles, restorePlan);
            return false;
        }

        List<IconInfo> orderedIcons = restorePlan.Icons.ToList();
        int insertIndex = restorePlan.InsertIndex;

        var trailingIcons = new List<IconInfo>();
        var trailingPositions = new Dictionary<IconInfo, Point>();
        if (!AppData.AppConfig.PMModel && IconListView != null)
        {
            IconListView.UpdateLayout();
            for (int i = insertIndex + 1; i < ShortcutTiles.Count; i++)
            {
                IconInfo icon = ShortcutTiles[i];
                if (LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon) || LauncherDragIconClassifier.IsDragYieldPlaceholder(icon)) continue;
                trailingIcons.Add(icon);
                if (IconListView.ContainerFromItem(icon) is ListViewItem item)
                {
                    trailingPositions[icon] = GetShortcutContainerPoint(item);
                }
            }
        }

        _batchDragPlaceholders.ApplyRestore(ShortcutTiles, restorePlan);

        if (!AppData.AppConfig.PMModel && orderedIcons.Count > 0 && IconListView != null)
        {
            var iconsSnapshot = orderedIcons.ToList();
            var trailingSnapshot = trailingIcons.ToList();
            var trailingPositionSnapshot = new Dictionary<IconInfo, Point>(trailingPositions);
            int releaseInsertIndex = insertIndex;
            _ctx.DispatcherQueue.TryEnqueue(() =>
            {
                StartBatchDropReleaseAnimation(
                    iconsSnapshot,
                    trailingSnapshot,
                    trailingPositionSnapshot,
                    releaseInsertIndex);
            });
            return true;
        }

        return false;
    }

    private void StartBatchDropReleaseAnimation(
        IReadOnlyList<IconInfo> icons,
        IReadOnlyList<IconInfo> trailingIcons,
        IReadOnlyDictionary<IconInfo, Point> trailingPositions,
        int insertIndex,
        int attempt = 0)
    {
        if (IconListView == null || icons.Count == 0) return;

        IconListView.UpdateLayout();
        int started = 0;
        if (icons.Count > 0)
        {
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            int columns = Math.Max(1, _ctx.GetShortcutColumnCount());
            for (int i = 0; i < icons.Count; i++)
            {
                if (IconListView.ContainerFromItem(icons[i]) is not ListViewItem item) continue;
                if (item.ContentTemplateRoot is not FrameworkElement root) continue;
                var off = LauncherBatchDropAnimationPlanner.GetReleaseOffset(i, columns);
                BeginShortcutItemFlyPlacement(item, root, off.X, off.Y,
                    LauncherBatchDropAnimationPlanner.GetStepDelay(i, LauncherDragPageContext.ShortcutReorderStaggerDelay), easing);
                started++;
            }
            for (int i = 0; i < trailingIcons.Count; i++)
            {
                IconInfo icon = trailingIcons[i];
                if (!trailingPositions.TryGetValue(icon, out Point oldPt)) continue;
                if (IconListView.ContainerFromItem(icon) is not ListViewItem tItem) continue;
                if (tItem.ContentTemplateRoot is not FrameworkElement tRoot) continue;
                Point newPt = GetShortcutContainerPoint(tItem);
                double dx = oldPt.X - newPt.X; double dy = oldPt.Y - newPt.Y;
                if (Math.Abs(dx) < 0.5d && Math.Abs(dy) < 0.5d) continue;
                BeginShortcutItemSlide(tItem, tRoot, dx, dy,
                    LauncherDragPageContext.ShortcutReorderAnimationDuration,
                    LauncherBatchDropAnimationPlanner.GetStepDelay(icons.Count + i, LauncherDragPageContext.ShortcutReorderStaggerDelay), easing);
                started++;
            }
        }

        if (started > 0)
        {
            ScheduleBatchDropReleaseCleanup(icons, started, trailingIcons.Count);
            return;
        }

        if (attempt < 2)
        {
            _ctx.DispatcherQueue.TryEnqueue(() =>
                StartBatchDropReleaseAnimation(icons, trailingIcons, trailingPositions, insertIndex, attempt + 1));
            return;
        }

        FinishBatchDropReleaseCleanup(icons);
    }

    private void ScheduleBatchDropReleaseCleanup(
        IReadOnlyList<IconInfo> icons,
        int startedAnimations,
        int trailingCount)
    {
        TimeSpan delay = LauncherBatchDropAnimationPlanner.GetCleanupDelay(
            startedAnimations,
            trailingCount,
            icons.Count,
            LauncherDragPageContext.ShortcutLayoutFlyAnimationDuration,
            LauncherDragPageContext.ShortcutReorderStaggerDelay,
            TimeSpan.FromMilliseconds(60));

        _pendingBatchDropReleaseIcons = icons.ToList();
        _dragOrchestrator.StartBatchDropReleaseCleanup(delay, _pendingBatchDropReleaseIcons, () =>
            FinishBatchDropReleaseCleanup(_pendingBatchDropReleaseIcons!));
    }

    private void FinishBatchDropReleaseCleanup(IReadOnlyList<IconInfo> icons)
    {
        StopAllLiveReorderAnimations();
        _ctx.ShortcutTransitions.SetEnabled(false);

        if (IconListView != null)
        {
            foreach (IconInfo icon in icons)
            {
                if (IconListView.ContainerFromItem(icon) is not ListViewItem container) continue;
                container.Opacity = 1;
                container.Visibility = Visibility.Visible;
                container.IsHitTestVisible = true;
                if (container.ContentTemplateRoot is UIElement root)
                {
                    root.Opacity = 1;
                    root.Visibility = Visibility.Visible;
                    root.IsHitTestVisible = true;
                }
            }
        }

        IconListView?.UpdateLayout();
        _ctx.ShortcutContainers.Restore(ShortcutTiles);
        _ctx.ShortcutTransitions.Restore();
    }

    public bool TryCommitCrossGroupDrop(
        IReadOnlyList<IconInfo> icons,
        MenuInfo? targetMenu,
        string batchFinalizeReason,
        out bool wasAllGroups)
    {
        wasAllGroups = IsAllGroupsShortcutView();
        bool targetAccepted = targetMenu != null
            && targetMenu.IconList != null
            && !targetMenu.IsAllGroupsTab_NoWrite
            && !targetMenu.IsEncrypt
            && CanDropIconsOnGroupTab(icons, targetMenu, FindGroupContainingIcon);
        LauncherCrossGroupDropPreparation preparation =
            LauncherCrossGroupDropPlanner.PrepareCommit(
                targetAccepted,
                _batchDragYieldPlaceholders.Count > 0);
        if (!preparation.CanCommit
            || targetMenu == null
            || targetMenu.IconList == null
            || targetMenu.IsAllGroupsTab_NoWrite)
        {
            return false;
        }

        if (preparation.ShouldFinalizeBatchPlaceholder)
        {
            FinalizeBatchPlaceholderBlockPosition(batchFinalizeReason);
        }

        int? previewInsertIndex =
            LauncherCrossGroupDropPlanner.ResolvePreviewInsertIndex(
                _batchDragYieldPlaceholders.Count > 0,
                _batchDragInsertIndex,
                _crossGroupPreview.Target == null
                    || ReferenceEquals(_crossGroupPreview.Target, targetMenu));

        List<IconInfo> validIcons = GetValidDropIcons(icons);
        if (validIcons.Count == 0) return false;
        var moveResult = _ctx.CrossGroupMoveCoordinator.Execute(new LauncherCrossGroupMoveRequest(
            validIcons.Select(icon => icon.IconId).ToArray(),
            targetMenu.MenuId,
            targetMenu.IconList?.Count ?? 0,
            previewInsertIndex,
            PreserveAllGroupsOrder: wasAllGroups));
        if (!moveResult.Committed) return false;
        if (moveResult.ViewAction == LauncherCrossGroupMoveViewAction.RestoreCapturedAllGroupsOrder)
            _allGroupsOrder.RestoreCurrentFromBestSnapshot();

        CommitCrossGroupPreviewState();
        _isCrossGroupDrag = true;
        _allGroupsOrder.ClearBeforePreview();
        return true;
    }

    public bool TryCommitCrossGroupDropAtDragEnd()
    {
        if (_isCrossGroupDrag || _draggedIcons.Count == 0) return false;

        MenuInfo? pointerTarget = null;
        ListView? groupListView = _ctx.GetGroupListView();
        if (groupListView != null && _ctx.TryGetDragPointerOverlayPosition(out Point gtOverlayPos)
            && DragOverlayCanvas is Canvas gtOverlay
            && LauncherDropHitTestAdapter.TryTranslatePoint(gtOverlay, groupListView, gtOverlayPos, out Point gtPos))
        {
            pointerTarget = LauncherDropHitTestAdapter.FindRealizedItem<MenuInfo>(groupListView, gtPos);
        }
        Canvas? groupBarOverlay = DragOverlayCanvas;
        FrameworkElement? groupBar = _ctx.GetGroupBar();
        bool pointerOverGroupBar = pointerTarget == null
            && _crossGroupPreview.IsActive
            && groupBar != null
            && groupBarOverlay != null
            && _ctx.TryGetDragPointerOverlayPosition(out Point gbOverlayPos)
            && LauncherDropHitTestAdapter.TryTranslatePoint(groupBarOverlay, groupBar, gbOverlayPos, out Point gbPos)
            && LauncherDropHitTestAdapter.IsPointInside(groupBar, gbPos);
        LauncherCrossGroupDropTarget<MenuInfo> resolution =
            LauncherCrossGroupDropPlanner.ResolveAtDragEnd(
                pointerTarget,
                _crossGroupPreview.IsActive,
                _crossGroupPreview.Target,
                _groupBarDrag.HoverTarget,
                pointerOverGroupBar);

        return TryCommitCrossGroupDrop(
            _draggedIcons,
            resolution.Target,
            "cross-group drop at drag end",
            out _);
    }

    // ── Drag Completion ─────────────────────────────────────────────

    public void CompleteDragReorder(
        bool committedFolderDrop,
        bool committedCrossGroupDrop,
        bool batchBlockDrag,
        bool animate,
        IconInfo? droppedIcon,
        IReadOnlyList<IconInfo> draggedIconsSnapshot,
        Action<IReadOnlyList<IconInfo>, bool, IconInfo?> restoreContainerVisibility)
    {
        if (!committedFolderDrop)
        {
            bool batchReleaseAnimationScheduled = false;
            if (!committedCrossGroupDrop && _draggedIcons.Count > 0)
            {
                var resolution = LauncherCrossGroupDropPlanner.ResolveOnComplete(
                    _crossGroupPreview.IsActive, _crossGroupPreview.Target);
                TryCommitCrossGroupDrop(_draggedIcons, resolution.Target, "cross-group drop on complete", out _);
            }

            if (_draggedIconsSuppressedFromTiles && !_isCrossGroupDrag)
            {
                FinalizeBatchPlaceholderBlockPosition("placeholder reconciled before restore");
                batchReleaseAnimationScheduled = RestoreDraggedIconsAfterBatchReorder();
                CommitCrossGroupPreviewState();
            }
            else if (_crossGroupPreview.IsActive && !_isCrossGroupDrag)
            {
                RestoreDraggedIconsAfterBatchReorder(cancelled: true);
                RestoreCrossGroupPreviewView();
            }

            _ctx.RestoreGroupTabReorderAfterIconDrag();
            _ctx.IconListReorderSuppression.Restore(
                IconListView,
                LauncherIconListReorderSuppressionReason.BatchDrag);

            if (!batchReleaseAnimationScheduled)
            {
                StopAllLiveReorderAnimations();
                _ctx.ShortcutTransitions.SetEnabled(false);
                restoreContainerVisibility(draggedIconsSnapshot, animate, droppedIcon);
            }

            if (_isCrossGroupDrag)
            {
                bool wasAllGroups = IsShowingAllGroups || _crossGroupPreview.SavedSelection.IsAllGroups;
                _isCrossGroupDrag = false;
                ClearDraggedIconsState();
                _dragSourceIndex = -1;
                _allGroupsOrder.ClearBeforePreview();

                _crossGroupDropView.Apply(
                    wasAllGroups,
                    _ctx.WorkspaceNavigation.GetActiveGroupIndex(AppData),
                    () =>
                    {
                        _ctx.IconSelection.RestoreDragVisuals();
                        _ctx.ShortcutContainers.Restore(ShortcutTiles);
                        _ctx.ShortcutTransitions.Restore();
                        ClearAllGroupsDragOrderSnapshots();
                    });
                return;
            }

            if (!batchBlockDrag && !_singleIconLiveMoveMutatedTiles
                && droppedIcon != null && !_draggedIconsSuppressedFromTiles)
            {
                int curIdx = ShortcutTiles.IndexOf(droppedIcon);
                if (curIdx >= 0)
                {
                    int tgtIdx = _singleIconDragPreview.TargetIndex >= 0
                        ? _singleIconDragPreview.TargetIndex
                        : _ctx.GetShortcutDropIndexFromPointer();
                    tgtIdx = Math.Clamp(tgtIdx, 0, Math.Max(0, ShortcutTiles.Count - 1));
                    if (tgtIdx != curIdx) ShortcutTiles.Move(curIdx, tgtIdx);
                }
            }

            ClearDraggedIconsState();
            _dragSourceIndex = -1;

            CommitCurrentShortcutOrder();

            _ctx.LayoutApply.Compact(ShortcutTiles, preserveExistingVisualOrder: false);

            _ctx.IconSelection.RestoreDragVisuals();
            _ctx.RefreshIconListLayout();

            if (!batchReleaseAnimationScheduled)
            {
                _ctx.ShortcutContainers.Restore(ShortcutTiles);
                _ctx.ScheduleDeferredShortcutContainerRestore();
                _ctx.ShortcutTransitions.Restore();
            }

            ClearAllGroupsDragOrderSnapshots();
        }
        else
        {
            CompleteIconDragCleanup(restoreContainers: true);
        }

        LogIconDragState("completed-clean");
    }

    private MenuInfo? FindGroupContainingIcon(IconInfo icon)
    {
        return ShortcutFolderTreeService.FindGroupContainingIcon(AppData.MenuList, icon);
    }

    /// <summary>
    /// Completes folder overlay item drag by cleaning up drag state.
    /// </summary>
    public void CompleteFolderOverlayItemDrag()
    {
        _ctx.EndDragPointerTracking();
        _ctx.SetIconDragActive(false);
        _dragPreviewController.End(animate: false);
        _isCustomDragActive = false;
        CompleteIconDragCleanup(restoreContainers: true);
    }

    /// <summary>
    /// Resets the grid position for an icon.
    /// </summary>
    public void ResetLauncherGridPosition(IconInfo icon)
    {
        LauncherCommandResult result = _launcherCommands.UpdateGridLayout(
            new UpdateLauncherGridLayoutCommand(new[]
            {
                new LauncherGridPlacement(icon.IconId, -1, -1)
            }));
        if (!result.Success)
        {
            LogUtil.WriteLog(result.Error ?? "重置 Launcher 网格位置失败。");
        }
    }

    // ── Drag Event Handlers ─────────────────────────────────────────

    /// <summary>
    /// Handles the business logic for IconListView_DragOver.
    /// The page should pass the result of TryUpdateFolderDropIntentFromPointer as folderDropIntent.
    /// </summary>
    public LauncherDragOverResult HandleIconListViewDragOver(Point pointerPosition, bool folderDropIntent)
    {
        var result = new LauncherDragOverResult { AcceptedMove = true };

        if (_dragStartedFromFolderOverlay)
        {
            SetFolderDropIntent(null);
            result.SuppressSystemDrag = true;
            result.UpdatePreview = true;
            UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
            result.UpdateSingleIconDropIndex = true;
            result.Handled = true;
            return result;
        }

        if (_crossGroupPreview.IsActive && _draggedIconsSuppressedFromTiles)
        {
            SetFolderDropIntent(null);
            result.SuppressSystemDrag = true;
            result.UpdatePreview = true;
            UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
            result.UpdateBatchPlaceholderBlock = true;
            result.Handled = true;
            return result;
        }

        if (_crossGroupPreview.IsActive && !_draggedIconsSuppressedFromTiles)
        {
            SetFolderDropIntent(null);
            _groupBarDrag.ClearIndicators();
            result.SuppressSystemDrag = true;
            result.UpdatePreview = true;
            UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
            result.Handled = true;
            return result;
        }

        result.SuppressSystemDrag = true;
        result.UpdatePreview = true;
        UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
        if (!folderDropIntent)
        {
            result.UpdateBatchPlaceholderBlock = true;
            result.UpdateSingleIconDropIndex = true;
        }
        result.Handled = folderDropIntent;
        return result;
    }

    /// <summary>
    /// Handles the business logic for WrapCard_DragOver.
    /// The page should call _groupBarDrag.TryUpdateHover and pass the result as groupBarHovered.
    /// </summary>
    public LauncherDragOverResult HandleWrapCardDragOver(Point pointerPosition, bool groupBarHovered, bool folderDropIntent)
    {
        var result = new LauncherDragOverResult { AcceptedMove = true };

        if (groupBarHovered)
        {
            if (_crossGroupPreview.IsActive && _draggedIconsSuppressedFromTiles)
            {
                result.SuppressSystemDrag = true;
                result.UpdateBatchPlaceholderBlock = true;
            }
            return result;
        }

        if (_crossGroupPreview.IsActive && _draggedIconsSuppressedFromTiles && IsDragPointOverShortcutIconAreaFromPoint(pointerPosition))
        {
            result.SuppressSystemDrag = true;
            result.UpdatePreview = true;
            UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
            result.UpdateBatchPlaceholderBlock = true;
            return result;
        }

        if (_crossGroupPreview.IsActive)
        {
            _groupBarDrag.ClearHighlight();
        }

        result.SuppressSystemDrag = true;
        result.UpdatePreview = true;
        UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
        if (!folderDropIntent && _crossGroupPreview.IsActive)
        {
            result.ClearGroupBarHighlight = true;
        }
        return result;
    }

    /// <summary>
    /// Handles the business logic for IconListView_Drop.
    /// </summary>
    public LauncherDropResult HandleIconListViewDrop(Point pointerPosition)
    {
        var result = new LauncherDropResult();
        if (!_isCustomDragActive
            || _isCrossGroupDrag
            || _draggedIcons.Count != 1
            || _draggedIcon == null)
        {
            result.Skip = true;
            return result;
        }

        result.StorePointer = true;
        result.PointerX = pointerPosition.X;
        result.PointerY = pointerPosition.Y;

        IconInfo? target = ResolveFolderDropTargetAtPoint(pointerPosition);
        if (!ReferenceEquals(target, _folderDropIntent.ActiveTarget))
        {
            result.Skip = true;
            return result;
        }

        result.CommittedFolderDrop = TryCommitFolderDrop(target);
        if (result.CommittedFolderDrop)
        {
            result.AcceptedMove = true;
            result.Handled = true;
        }
        return result;
    }

    /// <summary>
    /// Handles the business logic for OnDragPointerMoved.
    /// The page should check IsPointerOverFolderOverlayPanelAtCurrentPointer for folder overlay case.
    /// </summary>
    public void HandleDragPointerMoved(Point pointerPosition, bool isOverFolderOverlay)
    {
        if (!_isCustomDragActive) return;

        if (_dragStartedFromFolderOverlay)
        {
            SetFolderDropIntent(null);
            // The page should check isOverFolderOverlay and call TryUpdateSingleIconDropIndexFromPointer if needed
        }
        else
        {
            // The page should call TryUpdateFolderDropIntentFromPointer and pass the result
        }

        if (_dragPreviewController.HasPreview)
        {
            UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
        }
    }

    /// <summary>
    /// Handles the business logic for IconListView_DragItemsCompleted.
    /// The page should call TryUpdateFolderDropIntentFromPointer before this method.
    /// </summary>
    public void HandleIconListViewDragItemsCompleted()
    {
        _ctx.LogDragState("completed-enter");

        bool batchBlockDrag = IsYieldPlaceholderDragActive();
        bool committedCrossGroupDrop = TryCommitCrossGroupDropAtDragEnd();
        bool committedFolderDrop = _folderDropCommittedByDropEvent
            || (!committedCrossGroupDrop
                && !_isCrossGroupDrag
                && TryCommitFolderDrop(ResolveActivatedFolderDropTargetAtDragEnd()));
        _ctx.LogDragState(committedFolderDrop ? "completed-folder-committed" : "completed-folder-missed");
        _groupBarDrag.ClearHighlight();

        if (batchBlockDrag)
        {
            FinalizeBatchPlaceholderBlockPosition("block finalized on drag completed");
        }

        _ctx.EndDragPointerTracking();
        _ctx.SetIconDragActive(false);
        _draggedContainer = null;

        bool animate = _isCustomDragActive && !AppData.AppConfig.PMModel;

        CompleteDragReorder(
            committedFolderDrop,
            committedCrossGroupDrop,
            batchBlockDrag,
            animate,
            _draggedIcon,
            _draggedIcons.ToList(),
            (icons, shouldAnimate, dropIcon) =>
            {
                foreach (IconInfo icon in icons)
                {
                    if (IconListView?.ContainerFromItem(icon) is not ListViewItem container) continue;
                    container.Opacity = 1;
                    container.Visibility = Visibility.Visible;
                    container.IsHitTestVisible = true;
                    if (container.ContentTemplateRoot is UIElement root)
                    {
                        root.Opacity = 1;
                        root.Visibility = Visibility.Visible;
                        root.IsHitTestVisible = true;
                    }
                    if (shouldAnimate && ReferenceEquals(icon, dropIcon))
                    {
                        LauncherVisualTreeHelper.AnimateContainerFadeIn(container);
                    }
                }
            });
    }

    /// <summary>
    /// Resolves the activated folder drop target at drag end.
    /// </summary>
    public IconInfo? ResolveActivatedFolderDropTargetAtDragEnd()
    {
        IconInfo? activeTarget = _folderDropIntent.ActiveTarget;
        if (activeTarget == null || !IsValidFolderDropTarget(activeTarget))
        {
            return null;
        }

        IconInfo? pointerTarget = ResolveFolderDropTargetFromPointer(
            allowCapturedIntentWithoutPointer: false,
            allowCachedPointer: false);
        return ReferenceEquals(activeTarget, pointerTarget) ? activeTarget : null;
    }

    private bool IsDragPointOverShortcutIconAreaFromPoint(Point point)
    {
        ListView? host = _crossGroupPreviewOverlayController.IsVisible
            ? CrossGroupPreviewOverlay
            : IconListView;
        if (host == null || host.ActualWidth <= 0d || host.ActualHeight <= 0d)
            return false;
        return LauncherDropHitTestAdapter.IsPointInside(host, point);
    }

    private IconInfo? ResolveFolderDropTargetAtPoint(Point pointer)
    {
        if (IconListView == null || _draggedIcons.Count != 1 || _draggedIcon == null)
        {
            return null;
        }

        Dictionary<IconInfo, LauncherGridRect> layout = BuildCurrentLauncherLayoutMap(
            IconListView,
            ShortcutTiles);
        if (layout.Count == 0)
        {
            return null;
        }

        LauncherDragPreview preview = LauncherDragSession.ResolvePointer(
            ShortcutTiles,
            _draggedIcons,
            layout,
            new LauncherPoint(pointer.X, pointer.Y),
            GetTileInsertUpperBound(ShortcutTiles));
        return preview.Kind is LauncherDropKind.FolderTarget or LauncherDropKind.CreateFolderTarget
            && IsValidFolderDropTarget(preview.TargetIcon)
                ? preview.TargetIcon
                : null;
    }

    /// <summary>
    /// Handles the business logic for FolderItemsListView_DragItemsStarting.
    /// Returns the icon being dragged, or null if the drag should be cancelled.
    /// </summary>
    public IconInfo? HandleFolderItemsDragStarting(IReadOnlyList<object> items)
    {
        IconInfo? icon = items.OfType<IconInfo>().FirstOrDefault(item => !LauncherDragIconClassifier.IsAddShortcutPlaceholder(item));
        if (icon == null) return null;

        _draggedIcons.Clear();
        _draggedIcons.Add(icon);
        _draggedIcon = icon;
        _dragSourceIndex = -1;
        _singleIconDragPreview.Reset();
        _singleIconLiveMoveMutatedTiles = false;
        _folderDropCommittedByDropEvent = false;
        _isCustomDragActive = true;
        _isCrossGroupDrag = false;
        _dragStartedFromFolderOverlay = true;

        return icon;
    }

    /// <summary>
    /// Checks if the resolved drag items differ from the original items.
    /// Returns true if items were changed and the caller should update the drag source.
    /// </summary>
    public bool ApplyResolvedDragStartingItems(
        IReadOnlyList<object> originalItems,
        IReadOnlyList<object> resolvedItems)
    {
        bool sameItems = originalItems.Count == resolvedItems.Count;
        if (sameItems)
        {
            for (int i = 0; i < resolvedItems.Count; i++)
            {
                if (ReferenceEquals(originalItems[i], resolvedItems[i])) continue;

                sameItems = false;
                break;
            }
        }

        if (sameItems)
        {
            return false;
        }

        _ctx.IconSelection.ReplaceSelectionObjects(resolvedItems);
        return true;
    }

    /// <summary>
    /// Handles the complete business logic for IconListView_DragItemsStarting.
    /// Returns a result indicating whether the drag should proceed and what WinUI operations to apply.
    /// </summary>
    public (bool Cancel, LauncherDragStartResult? StartResult, LauncherDragPreviewRequest? PreviewRequest) HandleIconListViewDragStarting(
        IReadOnlyList<object> selectedItems,
        Func<IconInfo, ListViewItem?> getContainer)
    {
        LogIconDragState("starting");

        ShortcutDragStartItems<object> resolvedDragStart = ShortcutDragPolicy.ResolveDragStartItems(
            selectedItems,
            item => item is IconInfo icon && icon.IsAddShortcut_NoWrite,
            item => item is IconInfo icon && !LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon));
        if (resolvedDragStart.Cancel)
        {
            return (true, null, null);
        }

        bool itemsChanged = ApplyResolvedDragStartingItems(selectedItems, resolvedDragStart.Items);

        var startResult = TryBeginIconDragStartingCore(
            resolvedDragStart.Items,
            getContainer,
            out LauncherDragPreviewRequest? previewRequest);

        return (false, startResult, previewRequest);
    }

    /// <summary>
    /// Handles the business logic for FolderItemsListView_DragItemsCompleted.
    /// </summary>
    public void HandleFolderItemsDragItemsCompleted()
    {
        _ctx.EndDragPointerTracking();
        _ctx.SetIconDragActive(false);
        _dragPreviewController.End(animate: false);
        _isCustomDragActive = false;
        CompleteIconDragCleanup(restoreContainers: true);
    }

    /// <summary>
    /// Handles the business logic for FolderOverlay_DragOver.
    /// </summary>
    public void HandleFolderOverlayDragOver(Point pointerPosition)
    {
        if (!_dragStartedFromFolderOverlay || _draggedIcon == null) return;

        SetFolderDropIntent(null);
        UpdateDragPreviewPosition(pointerPosition.X, pointerPosition.Y);
    }

    /// <summary>
    /// Tries to get the folder drop pointer position in the icon list view.
    /// </summary>
    public bool TryGetFolderDropPointerInIconListView(out Point point, bool allowCachedPointer = true)
    {
        point = default;
        if (IconListView != null
            && _ctx.TryGetDragPointerOverlayPosition(out Point overlayPos)
            && DragOverlayCanvas is UIElement overlay
            && LauncherDropHitTestAdapter.TryTranslatePoint(overlay, IconListView, overlayPos, out point))
        {
            _dragPointerCache.Store(new LauncherPoint(point.X, point.Y));
            return true;
        }

        if (allowCachedPointer
            && _dragPointerCache.TryGet(out LauncherPoint cachedPoint))
        {
            point = new Point(cachedPoint.X, cachedPoint.Y);
            return true;
        }

        point = default;
        return false;
    }

    /// <summary>
    /// Tries to resolve a drag preview from the current pointer position.
    /// </summary>
    public bool TryResolveLauncherDragPreviewFromPointer(
        out LauncherDragPreview preview,
        bool allowCachedPointer = true)
    {
        preview = new LauncherDragPreview(LauncherDropKind.None, null, -1, false);
        if (IconListView == null
            || _draggedIcons.Count == 0
            || !TryGetFolderDropPointerInIconListView(out Point pointer, allowCachedPointer))
        {
            return false;
        }

        Dictionary<IconInfo, LauncherGridRect> layout = BuildCurrentLauncherLayoutMap(IconListView, ShortcutTiles);
        if (layout.Count == 0)
        {
            return false;
        }

        preview = LauncherDragSession.ResolvePointer(
            ShortcutTiles,
            _draggedIcons,
            layout,
            new LauncherPoint(pointer.X, pointer.Y),
            GetTileInsertUpperBound(ShortcutTiles));

        return true;
    }

    /// <summary>
    /// Resolves the folder drop target from pointer position.
    /// </summary>
    public IconInfo? ResolveFolderDropTargetFromPointer(
        bool allowCapturedIntentWithoutPointer = false,
        bool allowCachedPointer = true)
    {
        IconInfo? resolved = null;
        bool previewResolved = TryResolveLauncherDragPreviewFromPointer(out LauncherDragPreview preview, allowCachedPointer);
        if (previewResolved)
        {
            if (preview.Kind is LauncherDropKind.FolderTarget or LauncherDropKind.CreateFolderTarget)
            {
                resolved = preview.TargetIcon;
            }

            return resolved != null && IsValidFolderDropTarget(resolved)
                ? resolved
                : null;
        }

        if (resolved == null || !IsValidFolderDropTarget(resolved))
        {
            if (IconListView != null && _draggedIcon != null
                && TryGetFolderDropPointerInIconListView(out Point fdp, allowCachedPointer))
            {
                resolved = LauncherDropHitTestAdapter.FindCenteredTarget(
                    IconListView, ShortcutTiles, fdp,
                    candidate => LauncherDragIconClassifier.IsDataShortcutIcon(candidate)
                        && !ReferenceEquals(candidate, _draggedIcon)
                        && IsValidFolderDropTarget(candidate));
            }
        }

        if (resolved != null && IsValidFolderDropTarget(resolved))
        {
            return resolved;
        }

        if (_folderDropIntent.ActiveTarget != null
            && IsValidFolderDropTarget(_folderDropIntent.ActiveTarget)
            && (allowCapturedIntentWithoutPointer
                || (IconListView != null
                    && TryGetFolderDropPointerInIconListView(out Point hotZonePointer)
                    && LauncherDropHitTestAdapter.IsPointInsideCenterHotZone(
                        IconListView, _folderDropIntent.ActiveTarget, hotZonePointer))))
        {
            return _folderDropIntent.ActiveTarget;
        }

        return null;
    }

    /// <summary>
    /// Updates folder drop intent activation state.
    /// The page should pass the resolved target from ResolveFolderDropTargetFromPointer.
    /// </summary>
    public bool UpdateFolderDropIntentActivation(IconInfo? target)
    {
        bool isValid = target != null && IsValidFolderDropTarget(target);
        LauncherFolderDropIntentUpdate<IconInfo> update =
            _folderDropIntent.EvaluateCandidate(
                target,
                isValid,
                Environment.TickCount64,
                LauncherDragPageContext.FolderDropHoverActivationDelayMs);
        if (update.ActiveTarget != null && IsValidFolderDropTarget(update.ActiveTarget))
            LogUtil.WriteLog($"FolderDrop intent: dragged={_draggedIcon?.Name_NoWrite}, target={update.ActiveTarget.Name_NoWrite}, targetIsFolder={update.ActiveTarget.IsFolder}, allGroups={IsShowingAllGroups}");
        if (IsFolderDropReorderSuppressed())
        {
            StopAllLiveReorderAnimations();
            _batchDragState.ResetMoveThrottle();
            Point? fdiOrigin = null;
            if (update.ActiveTarget != null)
            {
                Canvas? fdiOv = DragOverlayCanvas;
                if (fdiOv != null && IconListView?.ContainerFromItem(update.ActiveTarget) is ListViewItem fdiItem
                    && fdiItem.ContentTemplateRoot is DependencyObject fdiTr
                    && LauncherVisualTreeHelper.FindShortcutIconBorder(fdiTr) is Border fdiIb)
                    fdiOrigin = fdiIb.TransformToVisual(fdiOv).TransformPoint(new Point(0, 0));
            }
            if (update.ActiveTargetChanged && update.ActiveTarget != null && _dragPreviewController.HasPreview && fdiOrigin is Point fdiVal)
                _dragPreviewController.AnimateToPlacement(fdiVal.X, fdiVal.Y, update.ActiveTarget.IsFolder ? 0.85d : 0.88d);
        }
        else
        {
            _dragPreviewController.ResumeFollowMode();
        }
        SetFolderDropTargetHighlight(update.ActiveTarget);
        return IsFolderDropReorderSuppressed();
    }

    /// <summary>
    /// Updates single icon drop index from pointer position.
    /// The page should pass folderOverlayMoveOut and previewResolved/preview from TryResolveLauncherDragPreviewFromPointer.
    /// </summary>
    public void TryUpdateSingleIconDropIndexFromPointer(
        bool folderOverlayMoveOut,
        bool previewResolved,
        LauncherDragPreview preview)
    {
        if (!_isCustomDragActive
            || _draggedIconsSuppressedFromTiles
            || _draggedIcon == null
            || IconListView == null)
        {
            return;
        }

        if (!folderOverlayMoveOut
            && !ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
                _draggedIcons.Count,
                IsShowingAllGroups,
                IsFolderDropReorderSuppressed(),
                _crossGroupPreview.IsActive))
        {
            return;
        }

        if (previewResolved && preview.Kind != LauncherDropKind.ReorderTarget)
        {
            _singleIconDragPreview.ClearPending();
            return;
        }

        int resolvedIndex = previewResolved
            ? preview.ReorderIndex
            : _ctx.GetShortcutDropIndexFromPointer();
        int targetIndex = Math.Clamp(
            resolvedIndex,
            0,
            Math.Max(0, ShortcutTiles.Count - 1));
        if (!TryStabilizeSingleIconReorderTarget(targetIndex)) return;
        int currentIndex = ShortcutTiles.IndexOf(_draggedIcon);
        if (currentIndex >= 0 && currentIndex != targetIndex)
        {
            if (_singleIconDragPreview.TargetIndex != targetIndex)
            {
                _singleIconDragPreview.CommitTarget(targetIndex);
                PreviewSingleIconLiveYield(targetIndex);
            }
        }
        else if (currentIndex >= 0 && _singleIconDragPreview.TargetIndex != targetIndex)
        {
            _singleIconDragPreview.CommitTarget(targetIndex);
            StopAllLiveReorderAnimations();
        }
        _singleIconDragPreview.CommitTarget(targetIndex);
    }

    public bool TryStabilizeSingleIconReorderTarget(int targetIndex)
    {
        return _singleIconDragPreview.TryActivateTarget(
            targetIndex,
            Environment.TickCount64,
            LauncherDragPageContext.SingleIconReorderActivationDelayMs);
    }

    private void PreviewSingleIconLiveYield(int targetIndex)
    {
        if (!_isCustomDragActive || _draggedIcon == null || _draggedIcons.Count != 1
            || _draggedIconsSuppressedFromTiles || IconListView == null || AppData.AppConfig.PMModel)
            return;
        int ci = ShortcutTiles.IndexOf(_draggedIcon);
        int sti = Math.Clamp(targetIndex, 0, Math.Max(0, GetTileInsertUpperBound(ShortcutTiles)));
        if (ci < 0 || ci == sti) { StopAllLiveReorderAnimations(); return; }
        var vt = ShortcutTiles.Where(LauncherDragIconClassifier.IsDataShortcutIcon).ToList();
        int vci = vt.IndexOf(_draggedIcon);
        if (vci < 0) return;
        vt.RemoveAt(vci);
        vt.Insert(Math.Clamp(sti, 0, vt.Count), _draggedIcon);
        int cc = Math.Max(1, _ctx.GetShortcutColumnCount());
        double sw = _ctx.GetShortcutItemSlotWidth() > 0d ? _ctx.GetShortcutItemSlotWidth() : 88d;
        double sh = _ctx.GetShortcutItemSlotHeight() > 0d ? _ctx.GetShortcutItemSlotHeight() : 104d;
        var curItems = ShortcutTiles
            .Where(LauncherDragIconClassifier.IsDataShortcutIcon)
            .Select(ic => LauncherShortcutGridItemAdapter.Create(
                ic,
                cc,
                preserveManualPosition: !IsShowingAllGroups));
        var curLayout = LauncherGridLayoutEngine.Arrange(curItems, new LauncherGridMetrics(cc, sw, sh));
        if (curLayout.Count == 0) return;
        var virItems = vt.Select(ic => LauncherShortcutGridItemAdapter.Create(ic, cc, preserveManualPosition: false));
        var virLayout = LauncherGridLayoutEngine.Arrange(virItems, new LauncherGridMetrics(cc, sw, sh));
        foreach (var ic in vt) { if (IconListView.ContainerFromItem(ic) is ListViewItem ri) ApplyDraggedItemContainerState(ri, ic); }
        var affected = new HashSet<ListViewItem>();
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        int step = 0;
        foreach (var ic in vt)
        {
            if (ReferenceEquals(ic, _draggedIcon) || !curLayout.TryGetValue(ic, out var cRect) || !virLayout.TryGetValue(ic, out var vRect)
                || IconListView.ContainerFromItem(ic) is not ListViewItem itm || itm.ContentTemplateRoot is not FrameworkElement rt) continue;
            double ox = vRect.Bounds.X - cRect.Bounds.X; double oy = vRect.Bounds.Y - cRect.Bounds.Y;
            if (Math.Abs(ox) < 0.5d && Math.Abs(oy) < 0.5d) continue;
            BeginShortcutItemPreviewOffset(itm, rt, ox, oy, LauncherDragPageContext.LiveDragReorderDuration, TimeSpan.FromMilliseconds(Math.Min(step, 8) * LauncherDragPageContext.LiveDragReorderStaggerDelay.TotalMilliseconds), ease);
            affected.Add(itm);
            step++;
            if (step >= LauncherDragPageContext.MaxLiveReorderAnimatedItems) break;
        }
        foreach (var itm in _ctx.ItemAnimations.AnimatedItems.ToList())
        {
            if (!affected.Contains(itm))
            {
                if (ShouldAnimateInteraction() && itm.ContentTemplateRoot is FrameworkElement art
                    && _ctx.ItemAnimations.TryGetTargetOffset(itm, out var tOff) && (Math.Abs(tOff.X) >= 0.5d || Math.Abs(tOff.Y) >= 0.5d))
                { BeginShortcutItemPreviewOffset(itm, art, 0d, 0d, TimeSpan.FromMilliseconds(100), TimeSpan.Zero, ease); affected.Add(itm); }
                else if (!_ctx.ItemAnimations.TryGetTargetOffset(itm, out var cTgt) || Math.Abs(cTgt.X) >= 0.5d || Math.Abs(cTgt.Y) >= 0.5d)
                    StopItemReorderAnimation(itm, "singleLiveYieldUnaffected");
            }
        }
    }

    // ── Folder Drop Commit ──────────────────────────────────────────

    public bool TryResolveItemCollection(IconInfo item, out ObservableCollection<IconInfo> collection)
    {
        if (ShortcutFolderTreeService.TryFindContainer(AppData.MenuList, item, out var container))
        {
            collection = container.Collection;
            return true;
        }

        collection = null!;
        return false;
    }

    public bool TryExecuteAllGroupsFolderMutation(
        LauncherAllGroupsFolderMutationKind kind,
        IconInfo subject,
        IconInfo target,
        int insertIndex = -1)
    {
        if (!IsShowingAllGroups
            || string.IsNullOrWhiteSpace(subject.IconId)
            || string.IsNullOrWhiteSpace(target.IconId))
        {
            return false;
        }

        List<IconInfo> flat = _allGroupsOrder.HasCurrentOrder
            ? _allGroupsOrder.CopyCurrentOrder()
            : GetOrderedIconsFromTiles();
        if (flat.Count == 0)
        {
            flat = LauncherAllGroupsProjectionBuilder.BuildDefault(AppData);
        }

        var flatIds = flat
            .Select(icon => icon.IconId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        if (flatIds.Count != flat.Count)
        {
            return false;
        }

        LauncherCommandResult result = _launcherCommands.MutateAllGroupsFolder(
            new LauncherAllGroupsFolderMutationCommand(
                kind,
                subject.IconId,
                target.IconId,
                flatIds,
                insertIndex));
        if (!result.Success || !result.Changed)
        {
            if (!result.Success)
            {
                LogUtil.WriteLog(result.Error ?? "All Groups 文件夹更新失败。");
            }
            return false;
        }

        List<IconInfo> updated = LauncherAllGroupsProjectionBuilder.Build(AppData);
        _allGroupsOrder.SetCurrent(updated);
        return true;
    }

    public void RestoreSingleIconToDragSourceBeforeFolderCommit()
    {
        if (_draggedIcon == null
            || _dragSourceIndex < 0
            || _draggedIconsSuppressedFromTiles)
        {
            return;
        }

        int currentIndex = ShortcutTiles.IndexOf(_draggedIcon);
        if (currentIndex < 0 || currentIndex == _dragSourceIndex)
        {
            return;
        }

        int targetIndex = Math.Clamp(_dragSourceIndex, 0, Math.Max(0, ShortcutTiles.Count - 1));
        ShortcutTiles.Move(currentIndex, targetIndex);
        _singleIconDragPreview.CommitTarget(targetIndex);
    }

    public bool TryCommitFolderDrop(IconInfo? target)
    {
        if (_draggedIcons.Count != 1 || _draggedIcon == null)
        {
            return false;
        }

        if (target == null)
        {
            LogUtil.WriteLog(
                $"FolderDrop commit miss: dragged={_draggedIcon.Name}, " +
                $"lastValid={_folderDropIntent.LastValidTarget?.Name ?? "<null>"}, " +
                $"duringDrag={_folderDropIntent.ActiveTarget?.Name ?? "<null>"}");
            return false;
        }

        if (!IsValidFolderDropTarget(target))
        {
            return false;
        }

        int createFolderVisualInsertIndex = -1;
        int createFolderInsertIndex = -1;
        if (!target.IsFolder)
        {
            createFolderInsertIndex = _ctx.GetShortcutDropIndexFromPointer();
            createFolderVisualInsertIndex = createFolderInsertIndex;
            if (IsShowingAllGroups && ShortcutFolderTreeService.TryFindContainer(AppData.MenuList, target, out var tc))
            {
                int local = 0;
                for (int i = 0; i < Math.Clamp(createFolderInsertIndex, 0, ShortcutTiles.Count); i++)
                {
                    if (LauncherDragIconClassifier.IsDataShortcutIcon(ShortcutTiles[i])
                        && !ReferenceEquals(ShortcutTiles[i], _draggedIcon)
                        && ShortcutFolderTreeService.TryFindContainer(AppData.MenuList, ShortcutTiles[i], out var ic)
                        && ReferenceEquals(ic.Collection, tc.Collection))
                        local++;
                }
                createFolderInsertIndex = local;
            }
        }

        RestoreSingleIconToDragSourceBeforeFolderCommit();

        if (target.IsFolder)
        {
            bool moved = (IsShowingAllGroups && target.IsFolder
                    && TryExecuteAllGroupsFolderMutation(LauncherAllGroupsFolderMutationKind.MoveInto, _draggedIcon, target))
                || (TryResolveItemCollection(_draggedIcon, out _)
                    && ExecuteLauncherCommand(new LauncherMutationCommand(
                        LauncherMutationKind.MoveIntoFolder,
                        DraggedShortcutId: _draggedIcon.IconId,
                        TargetShortcutId: target.IconId)));
            if (moved)
            {
                SetFolderDropIntent(null);
                _ctx.RebuildShortcutTiles();
                CommitAllGroupsProjection();
            }

            return moved;
        }

        if (LauncherShortcutDropTraitsAdapter.CanCreateFolderFrom(_draggedIcon, target))
        {
            bool committed = IsShowingAllGroups
                && LauncherShortcutDropTraitsAdapter.CanCreateFolderFrom(_draggedIcon, target)
                && TryExecuteAllGroupsFolderMutation(
                    LauncherAllGroupsFolderMutationKind.Create,
                    _draggedIcon, target, createFolderVisualInsertIndex);
            if (!committed)
            {
                committed = TryResolveItemCollection(_draggedIcon, out _)
                    && ExecuteLauncherCommand(new LauncherMutationCommand(
                        LauncherMutationKind.CreateFolder,
                        DraggedShortcutId: _draggedIcon.IconId,
                        TargetShortcutId: target.IconId,
                        InsertIndex: createFolderInsertIndex));
            }

            if (committed)
            {
                SetFolderDropIntent(null);
                _ctx.RebuildShortcutTiles();
                CommitAllGroupsProjection();
            }

            LogUtil.WriteLog(
                $"FolderDrop create-folder: committed={committed}, " +
                $"dragged={_draggedIcon.Name}, target={target.Name}, allGroups={IsShowingAllGroups}");

            return committed;
        }

        return false;
    }
}
