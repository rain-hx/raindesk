using rainspace.Constant;
using rainspace.Features.Launcher.Presentation.WinUI;
using rainspace.Control.Other;
using rainspace.Control.Windows;
using rainspace.Plugins.EveryThing;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Presentation;
using rainspace.Features.Appearance.Contracts;
using rainspace.Features.Workspace.Contracts;
using rainspace.Features.Workspace.Presentation;
using rainspace.Features.Workspace.Presentation.WinUI;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace rainspace.Control.UserControls.PannelCard
{
    /// <summary>
    /// RightCardControl.xaml 的交互逻辑
    /// </summary>
    public partial class RightCardControl : UserControl, INotifyPropertyChanged
    {
        private readonly IAppDataProvider _dataProvider;
        private readonly IWorkspaceCommandHandler _workspaceCommandHandler;
        private readonly ILauncherCommandHandler _launcherCommandHandler;
        private readonly IAppearanceQueryHandler _appearanceQueryHandler;
        private readonly IAppearanceCommandHandler _appearanceCommandHandler;
        private readonly LauncherBatchModeState _batchMode;
        private readonly LauncherBatchModeController<IconInfo> _batchModeController;
        private readonly LauncherCrossGroupMoveCoordinator _crossGroupMoveCoordinator;
        private AppData? _subscribedAppData;
        private bool _isControlLoaded;
        private AppData appData => _dataProvider.Current;
        private readonly LauncherShortcutListState _shortcutList = new();
        private BulkObservableCollection<IconInfo> shortcutTiles => _shortcutList.Items;
        private readonly SystemItemWindow.SystemItemViewModel inlineSystemVm;
        private ObservableCollection<IconInfo> currentSelectedIcons;
        private readonly WorkspaceGroupIconCollectionSubscription _groupIconSubscriptions = new();
        private readonly LauncherInlineAddPanelState _inlinePanel = new();
        private readonly LauncherInlineSystemItemsCoordinator<IconInfo> _inlineSystemItems;
        private readonly LauncherExternalImportCoordinator _externalImport;
        private readonly WorkspaceGroupEditSessionState<MenuInfo> _groupEdit = new();
        private readonly WorkspaceGroupEditVisualController _groupEditVisual = new();
        private readonly WorkspaceGroupEditFocusScheduler _groupEditFocus = new();
        private readonly WorkspaceGroupTabDragCoordinator<MenuInfo> _groupTabDrag;
        private readonly WorkspaceGroupTabChromeController _groupTabChrome = new();
        private readonly WorkspaceGroupTabContainerController _groupTabContainers;
        private readonly WorkspaceGroupTabSelectionIndicatorController _groupTabSelectionIndicator = new();
        private readonly WorkspaceGroupTabDeleteAnimationController _groupTabDeleteAnimation = new();
        private readonly WorkspaceGroupCollectionSubscription _groupCollectionSubscription = new();
        private readonly PointerEventHandler _rootGridPointerPressedHandler;
        private static readonly TimeSpan GroupTabDeleteAnimationDuration = TimeSpan.FromMilliseconds(220);
        private static readonly TimeSpan GroupTabSelectionAnimationDuration = TimeSpan.FromMilliseconds(220);
        private readonly LauncherShortcutLayoutScheduler _shortcutLayoutScheduler;
        private readonly LauncherShortcutTransitionController _shortcutTransitions;
        private readonly LauncherShortcutLayoutApplyController _layoutApply;
        private const double ShortcutGridRightInset = 8d;
        private const double ShortcutGridRootHorizontalInset = 66d;
        private static readonly TimeSpan ShortcutWrapAnimationDuration = TimeSpan.FromMilliseconds(220);
        private static readonly TimeSpan ShortcutLayoutFlyAnimationDuration = TimeSpan.FromMilliseconds(280);
        private static readonly TimeSpan ShortcutReorderAnimationDuration = TimeSpan.FromMilliseconds(230);
        private static readonly TimeSpan LiveDragReorderDuration = TimeSpan.FromMilliseconds(110);
        private static readonly TimeSpan ShortcutReorderStaggerDelay = TimeSpan.FromMilliseconds(24);
        private static readonly TimeSpan LiveDragReorderStaggerDelay = TimeSpan.Zero;
        private const int MaxLiveReorderAnimatedItems = 12;
        private const double ShortcutReorderYieldRiseOffset = 12d;
        private const int FolderDropHoverActivationDelayMs = 300;
        private const int SingleIconReorderActivationDelayMs = 150;
        private readonly LauncherIconActivationCoordinator<IconInfo> _iconActivation;
        private readonly LauncherContextMenuCoordinator<IconInfo> _launcherContextMenu;
        private readonly LauncherContextMenuViewController<IconInfo> _launcherContextMenuView;
        public LauncherFolderOverlayViewModel FolderOverlayViewModel { get; } = new();
        private LauncherFolderOverlayViewModel folderOverlayState => FolderOverlayViewModel;
        private readonly FolderOverlayController _folderOverlay;
        private readonly FolderOverlayMotionController _folderOverlayMotion = new();
        private readonly GroupTabEditController _groupTabEdit;
        private readonly InlineAddPanelController _inlineAddPanel;
        private readonly LauncherContextMenuHandler _contextMenuHandler;
        private readonly LauncherDragBusinessController _dragBusiness;

        // 自定义拖拽 — 纯状态委托到 LauncherDragSessionState
        private readonly LauncherDragSessionState _dragState = new();
        private readonly LauncherDragPreviewController _dragPreviewController = new();
        // _contextMenuIcon and _contextMenuGroup moved to LauncherContextMenuHandler
        private ListViewItem? _draggedContainer;
        private readonly LauncherCrossGroupPreviewOverlayController _crossGroupPreviewOverlayController = new();
        private readonly LauncherCrossGroupPreviewPlacementController<IconInfo, MenuInfo>
            _crossGroupPreviewPlacement;
        private readonly LauncherGroupBarDragController<MenuInfo, IconInfo> _groupBarDrag;
        private readonly LauncherCrossGroupDropViewCoordinator<IconInfo> _crossGroupDropView;
        private ListView? CrossGroupPreviewOverlay => _crossGroupPreviewOverlayController.View;
        private BulkObservableCollection<IconInfo> CrossGroupPreviewTiles => _crossGroupPreviewOverlayController.Tiles;
        private List<IconInfo>? _pendingBatchDropReleaseIcons;
        private readonly LauncherDragOrchestrator _dragOrchestrator;
        private static BitmapImage? _emptyDragBitmapImage;
        private readonly LauncherItemAnimationCoordinator _itemAnimations = new();
        private readonly LauncherShortcutContainerStateController _shortcutContainers;

        // 兼容属性：委托到 _dragState，保持现有调用点不变
        private IconInfo? _draggedIcon { get => _dragState.DraggedIcon; set => _dragState.DraggedIcon = value; }
        private List<IconInfo> _draggedIcons => _dragState.DraggedIcons;
        private int _dragSourceIndex { get => _dragState.DragSourceIndex; set => _dragState.DragSourceIndex = value; }
        private bool _isCustomDragActive { get => _dragState.IsCustomDragActive; set => _dragState.IsCustomDragActive = value; }
        private bool _isCrossGroupDrag { get => _dragState.IsCrossGroupDrag; set => _dragState.IsCrossGroupDrag = value; }
        private bool _dragStartedFromFolderOverlay { get => _dragState.DragStartedFromFolderOverlay; set => _dragState.DragStartedFromFolderOverlay = value; }
        private bool _singleIconLiveMoveMutatedTiles { get => _dragState.SingleIconLiveMoveMutatedTiles; set => _dragState.SingleIconLiveMoveMutatedTiles = value; }
        private bool _syncingPlaceholderBlockMove { get => _dragState.SyncingPlaceholderBlockMove; set => _dragState.SyncingPlaceholderBlockMove = value; }
        private int _liveReorderSlideGeneration { get => _dragState.LiveReorderSlideGeneration; set => _dragState.LiveReorderSlideGeneration = value; }
        private bool _groupTabReorderSuppressedForIconDrag { get => _dragState.GroupTabReorderSuppressedForIconDrag; set => _dragState.GroupTabReorderSuppressedForIconDrag = value; }
        private IconInfo? highlightedFolderDropTarget { get => _dragState.HighlightedFolderDropTarget; set => _dragState.HighlightedFolderDropTarget = value; }
        private bool _folderDropCommittedByDropEvent { get => _dragState.FolderDropCommittedByDropEvent; set => _dragState.FolderDropCommittedByDropEvent = value; }

        // 兼容属性：子状态实例委托到 _dragState
        private LauncherSingleIconDragPreviewState _singleIconDragPreview => _dragState.SingleIconPreview;
        private LauncherCrossGroupPreviewCoordinator<MenuInfo, WorkspaceNavigationSnapshot, IconInfo>
            _crossGroupPreview => _dragState.CrossGroupPreview;
        private LauncherAllGroupsOrderState<IconInfo> _allGroupsOrder => _dragState.AllGroupsOrder;
        private LauncherFolderDropIntentState<IconInfo> _folderDropIntent => _dragState.FolderDropIntent;
        private LauncherPointerCacheState _dragPointerCache => _dragState.PointerCache;
        private LauncherBatchPlaceholderCoordinator _batchDragPlaceholders => _dragState.BatchPlaceholders;
        private LauncherBatchDragSessionState _batchDragState => _dragState.BatchDrag;
        private bool _draggedIconsSuppressedFromTiles
        {
            get => _dragState.IsDraggedIconsSuppressedFromTiles;
            set => _dragState.IsDraggedIconsSuppressedFromTiles = value;
        }
        private int _batchDragInsertIndex
        {
            get => _dragState.BatchDragInsertIndex;
            set => _dragState.BatchDragInsertIndex = value;
        }
        private int _batchDragSourceInsertIndex
        {
            get => _dragState.BatchDragSourceInsertIndex;
            set => _dragState.BatchDragSourceInsertIndex = value;
        }
        private List<IconInfo> _batchDragYieldPlaceholders => _dragState.BatchDragYieldPlaceholders;
        private bool IsMergedBatchDragActive() => _dragState.IsMergedBatchDragActive;
        private bool IsYieldPlaceholderDragActive() => _dragState.IsYieldPlaceholderDragActive;
        private readonly LauncherIconListReorderSuppressionController _iconListReorderSuppression = new();
        private const int BatchDragMoveThrottleMs = 48;
        private readonly LauncherIconSelectionController<IconInfo> _iconSelection;
        private readonly WorkspaceGroupTabState _workspaceGroupTabs = new();
        private readonly WorkspaceNavigationPresenter _workspaceNavigation = new();
        private bool showingAllGroups => _workspaceNavigation.IsAllGroups;
        public bool UseManualShortcutGridPositions_NoWrite => !showingAllGroups;
        private MenuInfo _allGroupsTabEntry => _workspaceGroupTabs.AllGroupsEntry;
        private ObservableCollection<MenuInfo> _groupTabItems => _workspaceGroupTabs.Items;
        // Expose AppConfig for x:Bind in XAML (replaces RelativeSource AncestorType=Window)
        public AppConfig AppConfig => appData.AppConfig;
        public ObservableCollection<IconInfo> ShortcutTiles_NoWrite => shortcutTiles;
        public ObservableCollection<MenuInfo> ShortcutGroups_NoWrite => appData.MenuList;
        public ObservableCollection<MenuInfo> GroupTabItems_NoWrite => _groupTabItems;
        public double ShortcutItemSlotWidth_NoWrite
        {
            get => (double)GetValue(ShortcutItemSlotWidth_NoWriteProperty);
            set => SetValue(ShortcutItemSlotWidth_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutItemSlotWidth_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutItemSlotWidth_NoWrite),
                typeof(double),
                typeof(RightCardControl),
                new PropertyMetadata(88d));

        public double ShortcutItemCardWidth_NoWrite
        {
            get => (double)GetValue(ShortcutItemCardWidth_NoWriteProperty);
            set => SetValue(ShortcutItemCardWidth_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutItemCardWidth_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutItemCardWidth_NoWrite),
                typeof(double),
                typeof(RightCardControl),
                new PropertyMetadata(88d));

        public int ShortcutColumnCount_NoWrite
        {
            get => (int)GetValue(ShortcutColumnCount_NoWriteProperty);
            set => SetValue(ShortcutColumnCount_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutColumnCount_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutColumnCount_NoWrite),
                typeof(int),
                typeof(RightCardControl),
                new PropertyMetadata(1));

        public double ShortcutItemSlotHeight_NoWrite
        {
            get => (double)GetValue(ShortcutItemSlotHeight_NoWriteProperty);
            set => SetValue(ShortcutItemSlotHeight_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutItemSlotHeight_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutItemSlotHeight_NoWrite),
                typeof(double),
                typeof(RightCardControl),
                new PropertyMetadata(76d));

        public Thickness ShortcutGridHostPadding_NoWrite
        {
            get => (Thickness)GetValue(ShortcutGridHostPadding_NoWriteProperty);
            set => SetValue(ShortcutGridHostPadding_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutGridHostPadding_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutGridHostPadding_NoWrite),
                typeof(Thickness),
                typeof(RightCardControl),
                new PropertyMetadata(new Thickness(0, 0, 0, 0)));

        public double ShortcutFolderPreviewIconSize_NoWrite
        {
            get => (double)GetValue(ShortcutFolderPreviewIconSize_NoWriteProperty);
            set => SetValue(ShortcutFolderPreviewIconSize_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutFolderPreviewIconSize_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutFolderPreviewIconSize_NoWrite),
                typeof(double),
                typeof(RightCardControl),
                new PropertyMetadata(20d));

        public double ShortcutFolderBodyHeight_NoWrite
        {
            get => (double)GetValue(ShortcutFolderBodyHeight_NoWriteProperty);
            set => SetValue(ShortcutFolderBodyHeight_NoWriteProperty, value);
        }

        public static readonly DependencyProperty ShortcutFolderBodyHeight_NoWriteProperty =
            DependencyProperty.Register(
                nameof(ShortcutFolderBodyHeight_NoWrite),
                typeof(double),
                typeof(RightCardControl),
                new PropertyMetadata(54d));

        public event PropertyChangedEventHandler? PropertyChanged;

        public RightCardControl()
        {
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _workspaceCommandHandler = AppServices.Get<IWorkspaceCommandHandler>();
            _launcherCommandHandler = AppServices.Get<ILauncherCommandHandler>();
            _appearanceQueryHandler = AppServices.Get<IAppearanceQueryHandler>();
            _appearanceCommandHandler = AppServices.Get<IAppearanceCommandHandler>();
            _batchMode = AppServices.Get<LauncherBatchModeState>();
            _externalImport = new LauncherExternalImportCoordinator(
                AppServices.Get<ILauncherExternalImportService>());
            _crossGroupMoveCoordinator = new LauncherCrossGroupMoveCoordinator(_launcherCommandHandler);
            _iconActivation = new LauncherIconActivationCoordinator<IconInfo>(
                AppServices.Get<ILauncherActivationService>(),
                static icon => icon.IconId,
                LauncherDragIconClassifier.IsAddShortcutPlaceholder,
                static icon => icon.IsFolder,
                ShowAddUrlPanel,
                OpenFolder);
            _launcherContextMenu = new LauncherContextMenuCoordinator<IconInfo>(
                static icon => icon.IconId,
                static icon => icon.IsFolder,
                LauncherDragIconClassifier.IsAddShortcutPlaceholder,
                static icon => !string.IsNullOrWhiteSpace(icon.Path_NoWrite)
                    || !string.IsNullOrWhiteSpace(icon.LnkPath_NoWrite));
            ILauncherSystemCandidateSource systemCandidateSource =
                AppServices.Get<ILauncherSystemCandidateSource>();
            _inlineSystemItems = new LauncherInlineSystemItemsCoordinator<IconInfo>(
                async token => (await systemCandidateSource
                        .GetBuiltInSystemItemsAsync(token)
                        .ConfigureAwait(false))
                    .Select(LauncherSystemCandidatePresentationMapper.ToIconInfo)
                    .ToList(),
                async token => (await systemCandidateSource
                        .GetStartMenuItemsAsync(token)
                        .ConfigureAwait(false))
                    .Select(LauncherSystemCandidatePresentationMapper.ToIconInfo)
                    .ToList(),
                static icon => icon.Name_NoWrite);
            _inlineSystemItems.Updated += update =>
                DispatcherQueue.TryEnqueue(() => ApplyInlineSystemItemsUpdate(update));
            inlineSystemVm = new SystemItemWindow.SystemItemViewModel(appData.AppConfig);
            _rootGridPointerPressedHandler = RootGrid_PointerPressed;
            _groupIconSubscriptions.IconListReplaced += WorkspaceGroupIconListReplaced;
            _groupIconSubscriptions.IconsChanged += WorkspaceGroupIconsChanged;
            _groupTabDrag = new WorkspaceGroupTabDragCoordinator<MenuInfo>(
                () => GroupListView,
                () => GroupScrollViewer,
                () => MainWindow.mainWindow?.DragOverlayCanvasHost,
                () => MainWindow.mainWindow?.Content as UIElement,
                () => _groupTabItems,
                () => !IsGroupEditing() && !_isCustomDragActive,
                ResolveMenuInfoFromGroupTabItem,
                static menu => menu.IsAllGroupsTab_NoWrite,
                SyncMenuListFromGroupTabs,
                GroupTabWidthMetrics);
            _groupTabContainers = new WorkspaceGroupTabContainerController(
                _groupTabChrome,
                _groupTabDrag.ApplyPlaceholderChrome,
                GroupTabWidthMetrics,
                _groupTabDrag.PointerPressedHandler,
                _groupTabDrag.PointerMovedHandler,
                _groupTabDrag.PointerReleasedHandler,
                _groupTabDrag.PointerCanceledHandler,
                () => _groupTabDrag.Menu,
                () => Resources["GroupContextMenu"] as MenuFlyout,
                IsGroupTabSelectionMenu,
                () => ScheduleGroupTabSelectionIndicatorUpdate(
                    _workspaceNavigation.GetActiveGroupIndex(appData),
                    animate: false));
            _crossGroupPreviewPlacement =
                new LauncherCrossGroupPreviewPlacementController<IconInfo, MenuInfo>(
                    () => _crossGroupPreviewOverlayController.IsVisible,
                    () => CrossGroupPreviewOverlay,
                    () => CrossGroupPreviewTiles,
                    () => IconListView,
                    () => shortcutTiles,
                    () => _batchDragYieldPlaceholders,
                    GetActiveDraggedIcons,
                    () => MainWindow.mainWindow?.DragOverlayCanvasHost,
                    () => _crossGroupPreview.Target,
                    menu => WorkspaceGroupTabLayoutAdapter.TryGetContainer(
                        GroupListView,
                        menu,
                        out ListViewItem container)
                            ? container
                            : null,
                    LauncherVisualTreeHelper.FindShortcutIconBorder);
            _groupBarDrag = new LauncherGroupBarDragController<MenuInfo, IconInfo>(
                () => GroupListView,
                () => GroupBar,
                () => GroupScrollViewer,
                () => _groupTabItems,
                static (menu, highlighted) => menu.IsDragDropTarget_NoWrite = highlighted,
                () => _isCustomDragActive,
                () => _crossGroupPreview.IsActive,
                () => _crossGroupPreview.Target,
                IsDragPointOverShortcutIconArea,
                GetActiveDraggedIcons,
                () => _draggedIcon,
                (icons, menu) => menu.IconList != null && CanDropIconsOnGroupTab(icons, menu),
                ApplyCrossGroupPreview,
                RestoreCrossGroupPreviewView,
                GetEmptyDragBitmapImage);
            _crossGroupDropView = new LauncherCrossGroupDropViewCoordinator<IconInfo>(
                () => _allGroupsOrder.CurrentOrder,
                () => _workspaceNavigation.SelectAll(appData),
                ApplyAllGroupsFlatOrderToTiles,
                RebuildShortcutTiles,
                RefreshIconListLayout,
                RefreshGroupSelectionState,
                CommitCurrentShortcutOrder);
            _iconSelection = new LauncherIconSelectionController<IconInfo>(
                () => IconListView,
                () => WrapUFG,
                () => IconMarqueeCanvas,
                () => IconMarqueeRect,
                () => shortcutTiles,
                () => appData.MenuList.SelectMany(menu =>
                    menu.IconList ?? Enumerable.Empty<IconInfo>()),
                LauncherDragIconClassifier.IsAddShortcutPlaceholder,
                () => _batchMode.IsEnabled,
                () => _isCustomDragActive,
                () => ShortcutItemCardWidth_NoWrite,
                static icon => icon.IsListSelected_NoWrite,
                static (icon, selected, updateChecked) =>
                {
                    icon.IsListSelected_NoWrite = selected;
                    if (updateChecked)
                    {
                        icon.IsChecked_NoWrite = selected;
                    }
                });
            this.InitializeComponent();
            _shortcutContainers = new LauncherShortcutContainerStateController(
                IconListView,
                _itemAnimations,
                () => _isCustomDragActive,
                IsDraggedIcon,
                ApplyDraggedItemContainerState,
                RefreshShortcutItemSpans);
            _batchModeController = new LauncherBatchModeController<IconInfo>(
                _batchMode,
                IconListView,
                () => shortcutTiles,
                LauncherDragIconClassifier.IsAddShortcutPlaceholder,
                static icon => icon.IsChecked_NoWrite = false,
                UpdateIconReorderEnabled);
            _shortcutTransitions = new LauncherShortcutTransitionController(
                IconListView,
                () => _isCustomDragActive,
                UsesManualLiveDragYieldAnimation,
                ShouldAnimateInteraction,
                ShouldAnimateInteraction,
                ShouldAnimateInteraction,
                () => !appData.AppConfig.PMModel,
                ShortcutReorderAnimationDuration,
                ShortcutWrapAnimationDuration);
            _shortcutLayoutScheduler = new LauncherShortcutLayoutScheduler(
                DispatcherQueue,
                () => !_isCustomDragActive && ShouldAnimateInteraction(),
                (generation, animate) => ApplyLayoutFromScheduler(generation, animate),
                () => !_isCustomDragActive,
                _shortcutTransitions.Restore,
                ex => LogUtil.WriteErrorLog(ex, "快捷方式布局更新失败"),
                ShortcutWrapAnimationDuration + TimeSpan.FromMilliseconds(80));
            _layoutApply = new LauncherShortcutLayoutApplyController(
                _shortcutLayoutScheduler,
                GetShortcutAvailableWidth,
                () => shortcutTiles.Count,
                () => _isCustomDragActive,
                IsYieldPlaceholderDragActive,
                _launcherCommandHandler,
                () => shortcutTiles.Where(LauncherDragIconClassifier.IsDataShortcutIcon),
                (metrics, animate) =>
                {
                    _shortcutTransitions.Apply(animate);
                    if (IconListView != null)
                    {
                        LauncherShortcutPanelMetricsAdapter.Apply(IconListView.ItemsPanelRoot, metrics);
                    }

                    if (_crossGroupPreviewOverlayController.IsVisible)
                    {
                        LauncherShortcutPanelMetricsAdapter.Apply(
                            CrossGroupPreviewOverlay?.ItemsPanelRoot,
                            metrics);
                    }
                },
                () =>
                {
                    LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView?.ItemsPanelRoot);
                    IconListView?.UpdateLayout();
                    if (_crossGroupPreviewOverlayController.IsVisible)
                    {
                        LauncherShortcutPanelMetricsAdapter.Invalidate(
                            CrossGroupPreviewOverlay?.ItemsPanelRoot);
                        CrossGroupPreviewOverlay?.UpdateLayout();
                    }
                },
                RefreshIconListLayout,
                RefreshShortcutItemSpans,
                RestoreVisibleShortcutContainerState,
                (metrics, availableWidth, tileCount) =>
                {
                    ShortcutColumnCount_NoWrite = metrics.ColumnCount;
                    ShortcutItemSlotWidth_NoWrite = metrics.SlotWidth;
                    ShortcutItemCardWidth_NoWrite = metrics.CardWidth;
                    ShortcutItemSlotHeight_NoWrite = metrics.SlotHeight;
                    ShortcutFolderPreviewIconSize_NoWrite =
                        LauncherFolderTileVisualAdapter.GetPreviewIconSize(ShortcutFolderSize.Medium);
                    ShortcutFolderBodyHeight_NoWrite =
                        LauncherFolderTileVisualAdapter.GetBodyHeight(metrics.SlotHeight, rowSpan: 1);
                    ShortcutGridHostPadding_NoWrite = new Thickness(0d, 0d, ShortcutGridRightInset, 0d);
                },
                () => IconListView != null,
                ex => LogUtil.WriteErrorLog(ex, "快捷方式布局更新失败"),
                canPersistGridLayout: () => !showingAllGroups);
            _launcherContextMenuView = new LauncherContextMenuViewController<IconInfo>(
                new Dictionary<LauncherContextMenuAction, MenuFlyoutItemBase>
                {
                    [LauncherContextMenuAction.OpenFolder] = OpenFolderContextMenuItem,
                    [LauncherContextMenuAction.ResizeFolder] = FolderSizeContextMenuItem,
                    [LauncherContextMenuAction.RunAsAdministrator] = AdminStartContextMenuItem,
                    [LauncherContextMenuAction.ShowInFileExplorer] = ShowInExplorerContextMenuItem,
                    [LauncherContextMenuAction.EditProperties] = PropertiesContextMenuItem,
                    [LauncherContextMenuAction.Rename] = RenameContextMenuItem,
                    [LauncherContextMenuAction.Remove] = RemoveContextMenuItem
                },
                PrimaryContextMenuSeparator,
                EditContextMenuSeparator);
            _dragOrchestrator = new LauncherDragOrchestrator(
                _dragState,
                DispatcherQueue,
                updateDragFromPointer: () =>
                {
                    if (_dragState.DragStartedFromFolderOverlay)
                    {
                        SetFolderDropIntent(null);
                        if (!IsPointerOverFolderOverlayPanelAtCurrentPointer())
                        {
                            TryUpdateSingleIconDropIndexFromPointer();
                        }
                    }
                    else
                    {
                        bool folderDropIntent = TryUpdateFolderDropIntentFromPointer();
                        if (!folderDropIntent)
                        {
                            if (_dragState.IsYieldPlaceholderDragActive)
                            {
                                TryUpdateBatchPlaceholderBlockFromPointer();
                            }
                            else
                            {
                                TryUpdateSingleIconDropIndexFromPointer();
                            }
                        }
                    }
                },
                updatePreviewFromCursor: () =>
                {
                    if (!_dragPreviewController.HasPreview) return;
                    try
                    {
                        var window = MainWindow.mainWindow;
                        var overlay = window?.DragOverlayCanvasHost;
                        if (window == null || overlay == null) return;

                        if (!NativeWindowMethods.GetCursorPos(out NativeWindowMethods.POINT screenPt)) return;

                        IntPtr hwnd = WindowNative.GetWindowHandle(window);
                        var clientPt = screenPt;
                        if (!ScreenToClient(hwnd, ref clientPt)) return;

                        double scale = overlay.XamlRoot?.RasterizationScale ?? 1.0;
                        UpdateDragPreviewPosition(clientPt.X / scale, clientPt.Y / scale);
                    }
                    catch { }
                },
                cleanupDrag: () => CompleteIconDragCleanup(restoreContainers: true));
            _folderOverlay = new FolderOverlayController(
                FolderOverlayViewModel,
                refreshOverlay: RefreshFolderOverlay,
                isAddShortcutPlaceholder: LauncherDragIconClassifier.IsAddShortcutPlaceholder,
                activateIcon: icon => _iconActivation.Activate(icon, LauncherActivationMode.Default),
                animateOpen: AnimateFolderOverlayOpen,
                animateClose: AnimateFolderOverlayClose);
            _groupTabEdit = new GroupTabEditController(
                _groupEdit,
                _groupEditVisual,
                _groupEditFocus,
                _groupTabDrag,
                _groupTabChrome,
                _groupTabSelectionIndicator,
                _groupTabDeleteAnimation,
                _workspaceCommandHandler,
                _workspaceNavigation,
                _iconSelection,
                _workspaceGroupTabs,
                _groupBarDrag,
                _rootGridPointerPressedHandler,
                () => _groupTabReorderSuppressedForIconDrag,
                v => _groupTabReorderSuppressedForIconDrag = v,
                () => appData,
                () => showingAllGroups,
                config => config.PMModel,
                SelectAllGroups,
                (info, index) => SelectGroup(index),
                SyncGroupTabItems);
            _inlineAddPanel = new InlineAddPanelController(
                _inlinePanel,
                _inlineSystemItems,
                inlineSystemVm,
                EnsureWritableGroupSelected,
                ShowShortcutPanel);
            _contextMenuHandler = new LauncherContextMenuHandler(
                _launcherContextMenu,
                _launcherContextMenuView,
                _iconActivation,
                _launcherCommandHandler,
                _appearanceQueryHandler,
                _appearanceCommandHandler,
                _batchMode,
                _layoutApply,
                _folderOverlay,
                _inlinePanel,
                () => appData,
                () => _batchModeController,
                () => shortcutTiles,
                RebuildShortcutTiles,
                SyncFolderOverlayAfterFolderMutation,
                RefreshShortcutItemSpans,
                ShowAddUrlPanel,
                ShowAddSystemPanel);
            var dragPageCtx = new LauncherDragPageContext
            {
                GetIconListView = () => IconListView,
                GetGroupListView = () => GroupListView,
                GetGroupBar = () => GroupBar,
                GetWrapUFG = () => WrapUFG,
                GetFolderItemsListView = () => FolderItemsListView,
                GetGroupScrollViewer = () => GroupScrollViewer,
                GetIconMarqueeCanvas = () => IconMarqueeCanvas,
                GetIconMarqueeRect = () => IconMarqueeRect,
                GetAppData = () => appData,
                GetShortcutTiles = () => shortcutTiles,
                IsShowingAllGroups = () => showingAllGroups,
                GetShortcutColumnCount = () => ShortcutColumnCount_NoWrite,
                GetShortcutAvailableWidth = GetShortcutAvailableWidth,
                GetShortcutItemSlotWidth = () => ShortcutItemSlotWidth_NoWrite,
                GetShortcutItemSlotHeight = () => ShortcutItemSlotHeight_NoWrite,
                GetShortcutItemCardWidth = () => ShortcutItemCardWidth_NoWrite,
                GetShortcutDropIndexFromPointer = () => GetShortcutDropIndexFromPointer(skipIcon: null),
                RebuildShortcutTiles = RebuildShortcutTiles,
                RefreshShortcutItemSpans = RefreshShortcutItemSpans,
                RefreshIconListLayout = RefreshIconListLayout,
                UpdateIconReorderEnabled = UpdateIconReorderEnabled,
                LogDragState = LogIconDragState,
                RefreshGroupSelectionState = RefreshGroupSelectionState,
                SuppressGroupTabReorderForIconDrag = SuppressGroupTabReorderForIconDrag,
                RestoreGroupTabReorderAfterIconDrag = RestoreGroupTabReorderAfterIconDrag,
                BeginDragPointerTracking = BeginDragPointerTracking,
                EndDragPointerTracking = EndDragPointerTracking,
                ScheduleDeferredShortcutContainerRestore = () =>
                {
                    ScheduleDeferredShortcutContainerRestore();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                GetSelectedIcons = () => currentSelectedIcons,
                ApplyShortcutTileSnapshot = tiles => _shortcutList.ApplySnapshot(tiles),
                TryGetDragPointerOverlayPosition = TryGetDragPointerOverlayPosition,
                GetDragOverlayCanvas = () => MainWindow.mainWindow?.DragOverlayCanvasHost,
                SetIconDragActive = active => MainWindow.mainWindow?.SetIconDragActive(active),
                GetWindowContent = () => MainWindow.mainWindow?.Content as UIElement,
                LayoutApply = _layoutApply,
                ItemAnimations = _itemAnimations,
                ShortcutContainers = _shortcutContainers,
                IconListReorderSuppression = _iconListReorderSuppression,
                IconSelection = _iconSelection,
                FolderOverlay = _folderOverlay,
                ExternalImport = _externalImport,
                ShortcutTransitions = _shortcutTransitions,
                ShortcutLayoutScheduler = _shortcutLayoutScheduler,
                CrossGroupMoveCoordinator = _crossGroupMoveCoordinator,
                WorkspaceNavigation = _workspaceNavigation,
                DispatcherQueue = DispatcherQueue
            };
            _dragBusiness = new LauncherDragBusinessController(
                dragPageCtx,
                _dragState,
                _dragPreviewController,
                _crossGroupPreviewOverlayController,
                _crossGroupPreviewPlacement,
                _groupBarDrag,
                _crossGroupDropView,
                _dragOrchestrator,
                _launcherCommandHandler,
                _dataProvider);
            IconListView.ItemsSource = shortcutTiles;
            IconListView.SelectionChanged += _iconSelection.HandleSelectionChanged;
            shortcutTiles.CollectionChanged += ShortcutTiles_CollectionChanged;
            this.Loaded += RightCardControl_Loaded;
            this.Unloaded += RightCardControl_Unloaded;
            RootGrid.SizeChanged += RootGrid_SizeChanged;
            _layoutApply.ApplyInitial(0d);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ShouldIgnoreShortcutSizeChange(e)) return;
            _layoutApply.Schedule(ShouldAnimateShortcutResizeForSizeChange(e));
        }

        private void RightCardControl_Loaded(object sender, RoutedEventArgs e)
        {
            // WinUI 3: 延迟加载
            _isControlLoaded = true;
            _dataProvider.StateReplaced -= AppDataProvider_StateReplaced;
            _dataProvider.StateReplaced += AppDataProvider_StateReplaced;
            AttachAppDataSubscriptions(appData);
            _batchModeController.Attach();
            IconDialog.Opening += IconContextFlyout_Opening;
            GroupContextMenu.Opening += GroupContextFlyout_Opening;
            RootGrid.AddHandler(UIElement.PointerPressedEvent, _rootGridPointerPressedHandler, true);
            if (GetShortcutAvailableWidth() > 0d)
            {
                _layoutApply.ApplyFromScheduler(_layoutApply.CurrentGeneration, animateLayout: false);
            }
            else
            {
                _layoutApply.Schedule();
            }

            DispatcherQueue.TryEnqueue(() => _layoutApply.Schedule());
            SyncGroupTabItems();
            if (GroupListView != null)
            {
                GroupListView.Padding = new Thickness(0d);
                GroupListView.ItemContainerTransitions = new TransitionCollection();
                GroupListView.ItemsSource = _groupTabItems;
            }

            SelectAllGroups();
            RefreshGlassBackdrop();
        }

        private void AppDataProvider_StateReplaced(AppData replacement)
        {
            if (!_isControlLoaded)
            {
                return;
            }

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() => AppDataProvider_StateReplaced(replacement));
                return;
            }

            replacement = _dataProvider.Current;
            _batchMode.SetEnabled(false);
            CancelGroupEditForStateReplacement();
            AttachSelectedIcons(null);
            AttachAppDataSubscriptions(replacement);
            inlineSystemVm.AppConfig = replacement.AppConfig;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppConfig)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShortcutGroups_NoWrite)));
            SyncGroupTabItems();
            SelectAllGroups();
            RefreshGlassBackdrop();
        }

        private void CancelGroupEditForStateReplacement()
            => _groupTabEdit.CancelGroupEditForStateReplacement(GroupListView, DispatcherQueue);

        private void AttachAppDataSubscriptions(AppData data)
        {
            if (_subscribedAppData != null)
            {
                _subscribedAppData.AppConfig.PropertyChanged -= AppConfig_PropertyChanged;
            }

            _groupIconSubscriptions.DetachAll();
            _subscribedAppData = data;
            data.AppConfig.PropertyChanged -= AppConfig_PropertyChanged;
            data.AppConfig.PropertyChanged += AppConfig_PropertyChanged;
            _groupCollectionSubscription.Attach(data.MenuList, MenuList_CollectionChanged);
            _groupIconSubscriptions.Synchronize(data.MenuList);
        }

        private void SyncGroupTabItems()
        {
            _workspaceGroupTabs.Reset(appData.MenuList);
        }

        private void SyncMenuListFromGroupTabs()
        {
            if (_groupTabItems.Count <= 1)
            {
                return;
            }

            IReadOnlyList<string> orderedIds = _workspaceGroupTabs.GetOrderedGroupIds();
            WorkspaceCommandResult result = _workspaceCommandHandler.Reorder(
                new ReorderWorkspaceGroupsCommand(orderedIds));
            if (!result.Success)
            {
                SyncGroupTabItems();
                GrowlService.Warning(result.Error ?? "分组排序失败，请重试。");
            }
        }

        private static readonly WorkspaceGroupTabWidthMetrics GroupTabWidthMetrics =
            WorkspaceGroupTabWidthMetrics.Default;

        private void GroupListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            _groupTabContainers.Handle(sender, args);
        }

        internal void RefreshGroupIcon(MenuInfo menu)
        {
            _groupTabContainers.RefreshIcon(GroupListView, menu);
        }

        private bool IsGroupTabSelectionMenu(MenuInfo menu)
        {
            return WorkspaceGroupSelectionProjection.IsSelectedTab(
                menu,
                _allGroupsTabEntry,
                _workspaceNavigation.GetActiveGroup(appData),
                showingAllGroups);
        }

        private static MenuInfo? ResolveMenuInfoFromGroupTabItem(ListViewItem item) =>
            item.Content as MenuInfo ?? item.DataContext as MenuInfo;


        private void RightCardControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 清理事件订阅，防止内存泄漏
            _isControlLoaded = false;
            _dataProvider.StateReplaced -= AppDataProvider_StateReplaced;
            _batchModeController.Detach();
            if (_subscribedAppData != null)
            {
                _subscribedAppData.AppConfig.PropertyChanged -= AppConfig_PropertyChanged;
                _subscribedAppData = null;
            }
            _groupCollectionSubscription.Detach();
            _groupIconSubscriptions.DetachAll();
            _groupTabDrag.Abort();
            _iconListReorderSuppression.Clear(IconListView);
            AttachSelectedIcons(null);
            IconDialog.Opening -= IconContextFlyout_Opening;
            GroupContextMenu.Opening -= GroupContextFlyout_Opening;
            RootGrid.RemoveHandler(UIElement.PointerPressedEvent, _rootGridPointerPressedHandler);

            // 清理定时器和 CancellationToken
            _inlineSystemItems.CancelPending();
            _externalImport.CancelPending();
            _shortcutLayoutScheduler.Cancel();
            _folderOverlayMotion.Stop();
        }

        private void IconContextFlyout_Opening(object? sender, object e)
            => _contextMenuHandler.HandleIconContextFlyoutOpening(sender, e, ResolveIconFromContextTarget);

        private void GroupContextFlyout_Opening(object? sender, object e)
            => _contextMenuHandler.HandleGroupContextFlyoutOpening(sender, e,
                LauncherVisualTreeHelper.ResolveGroupFromContextTarget,
                EditGroupIconMenuItem, EditGroupIconMenuSeparator, DeleteGroupMenuItem);

        private IconInfo? ResolveIconFromContextTarget(DependencyObject? target)
        {
            IconInfo? icon = LauncherVisualTreeHelper.ResolveIconFromFlyoutTarget(target);
            if (icon != null) return icon;

            if (target is ListViewItem item && IconListView != null)
            {
                int index = IconListView.IndexFromContainer(item);
                if (index >= 0 && index < shortcutTiles.Count)
                {
                    return shortcutTiles[index];
                }
            }

            return null;
        }


        private IconInfo? ResolveIconForContextMenu(object sender)
        {
            IconInfo? icon = _contextMenuHandler.ResolveIconForContextMenu(sender);
            return icon ?? ResolveIconFromContextTarget(IconDialog.Target);
        }

        private MenuInfo? ResolveGroupForContextMenu(object sender)
        {
            MenuInfo? menu = _contextMenuHandler.ResolveGroupForContextMenu(sender);
            return menu ?? LauncherVisualTreeHelper.ResolveGroupFromContextTarget(GroupContextMenu.Target);
        }

        private void IconListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ShouldIgnoreShortcutSizeChange(e)) return;
            _layoutApply.Schedule(ShouldAnimateShortcutResizeForSizeChange(e));
        }

        private void ShortcutHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ShouldIgnoreShortcutSizeChange(e)) return;
            _layoutApply.Schedule(ShouldAnimateShortcutResizeForSizeChange(e));
        }

        private static bool ShouldIgnoreShortcutSizeChange(SizeChangedEventArgs e)
        {
            return Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 0.5d
                && Math.Abs(e.NewSize.Height - e.PreviousSize.Height) < 0.5d;
        }

        private static bool ShouldAnimateShortcutResizeForSizeChange(SizeChangedEventArgs e)
        {
            bool widthRestored = e.PreviousSize.Width < 1d && e.NewSize.Width > 1d;
            bool heightRestored = e.PreviousSize.Height < 1d && e.NewSize.Height > 1d;
            return !(widthRestored || heightRestored);
        }

        internal void ScheduleShortcutLayoutUpdateFromWindow()
        {
            _layoutApply.ScheduleFromWindow();
        }

        /// <summary>动画开关：性能模式（PMModel）下全部关闭。</summary>
        private bool ShouldAnimateInteraction()
            => MotionPreferences.ShouldAnimate && !appData.AppConfig.PMModel;

        private void AnimateFolderOverlayOpen()
        {
            DispatcherQueue.TryEnqueue(() => _folderOverlayMotion.Enter(
                FolderOverlay,
                FolderOverlayPanel,
                ShouldAnimateInteraction()));
        }

        private void AnimateFolderOverlayClose(Action completed)
        {
            _folderOverlayMotion.Exit(
                FolderOverlay,
                FolderOverlayPanel,
                ShouldAnimateInteraction(),
                completed);
        }

        private void RefreshIconListLayout()
        {
            if (IconListView == null) return;

            IconListView.SetDesiredContainerUpdateDuration(TimeSpan.Zero);
            IconListView.UpdateLayout();

            LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView.ItemsPanelRoot);

            DispatcherQueue.TryEnqueue(() =>
            {
                IconListView.SetDesiredContainerUpdateDuration(TimeSpan.FromMilliseconds(200));
            });
        }

        private void RefreshShortcutItemSpans()
        {
            if (IconListView == null) return;

            IconListView.UpdateLayout();
            for (int i = 0; i < shortcutTiles.Count; i++)
            {
                IconInfo icon = shortcutTiles[i];
                if (IconListView.ContainerFromItem(icon) is not ListViewItem item)
                {
                    continue;
                }

                LauncherShortcutItemSpanAdapter.Apply(
                    IconListView,
                    item,
                    icon,
                    ShortcutColumnCount_NoWrite,
                    GetShortcutAvailableWidth());
                if (icon.IsFolder)
                {
                    LauncherFolderTileVisualAdapter.Apply(
                        IconListView,
                        item,
                        icon,
                        ShortcutColumnCount_NoWrite,
                        GetShortcutAvailableWidth());
                }
            }

            LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView.ItemsPanelRoot);

            IconListView.UpdateLayout();
        }

        private Point? GetIconContainerWindowOrigin(IconInfo icon) => _dragBusiness.GetIconContainerWindowOrigin(icon);

        private const double LauncherShortcutDragPreviewSize = 64d;


        private void StartLiveYieldFromPositions(
            IReadOnlyDictionary<IconInfo, Point> previousPositions,
            int fromIndex,
            int toIndex,
            int generation,
            bool isRetry = false)
            => _dragBusiness.StartLiveYieldFromPositions(previousPositions, fromIndex, toIndex, generation, isRetry);

        private void BeginShortcutItemSlide(
            ListViewItem item,
            FrameworkElement root,
            double dx,
            double dy,
            TimeSpan duration,
            TimeSpan beginTime,
            EasingFunctionBase easing)
            => _dragBusiness.BeginShortcutItemSlide(item, root, dx, dy, duration, beginTime, easing);

        private void BeginShortcutItemFlyPlacement(
            ListViewItem item,
            FrameworkElement root,
            double dx,
            double dy,
            TimeSpan beginTime,
            EasingFunctionBase easing)
            => _dragBusiness.BeginShortcutItemFlyPlacement(item, root, dx, dy, beginTime, easing);

        private void StopItemReorderAnimation(ListViewItem item, string reason = "unknown")
            => _dragBusiness.StopItemReorderAnimation(item, reason);

        private void StopAllLiveReorderAnimations()
            => _dragBusiness.StopAllLiveReorderAnimations();

        private void ShortcutTiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => _dragBusiness.HandleShortcutTilesCollectionChanged(sender, e);

        private void ApplyLayoutFromScheduler(int layoutGeneration, bool animateLayout)
        {
            _layoutApply.ApplyFromScheduler(layoutGeneration, animateLayout);
        }

        private void ApplyOverlayItemChrome()
            => _dragBusiness.ApplyOverlayItemChrome();

        private Point GetShortcutContainerPoint(ListViewItem item)
            => _dragBusiness.GetShortcutContainerPoint(item);

        private double GetShortcutAvailableWidth()
        {
            return LauncherShortcutViewportAdapter.GetAvailableWidth(
                IconListView,
                WrapUFG,
                RootGrid,
                ShortcutGridRootHorizontalInset);
        }

        private void ScheduleDeferredShortcutContainerRestore()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_isCustomDragActive) return;

                IconListView?.UpdateLayout();
                RestoreVisibleShortcutContainerState();

            });
        }

        private void RestoreVisibleShortcutContainerState()
        {
            _shortcutContainers.Restore(shortcutTiles);
        }

        public void RefreshGlassBackdrop()
        {
            GlassBackdropUtil.ApplyCardGlass(WrapCard);
        }

        private void AppConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppConfig.SelectedMenuIcons))
            {
                _iconSelection.ClearSelection();
                AttachSelectedIcons(appData.AppConfig.SelectedMenuIcons);
                RebuildShortcutTiles();
            }
            else if (e.PropertyName == nameof(AppConfig.BlurEffect) || e.PropertyName == nameof(AppConfig.BlurValue))
            {
                RefreshGlassBackdrop();
            }
        }

        private void AttachSelectedIcons(ObservableCollection<IconInfo>? icons)
        {
            if (currentSelectedIcons != null)
            {
                currentSelectedIcons.CollectionChanged -= SelectedIcons_CollectionChanged;
            }

            currentSelectedIcons = icons;

            if (currentSelectedIcons != null)
            {
                currentSelectedIcons.CollectionChanged += SelectedIcons_CollectionChanged;
            }
        }

        private void SelectedIcons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isCustomDragActive && e.Action == NotifyCollectionChangedAction.Move) return;
            RebuildShortcutTiles();
        }

        private void MenuList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _groupIconSubscriptions.Synchronize(appData.MenuList);

            _workspaceGroupTabs.ApplyCollectionChange(e, appData.MenuList);

            bool selectionChanged = _workspaceNavigation.Reconcile(appData);
            int selectedIndex = _workspaceNavigation.GetActiveGroupIndex(appData);
            if (selectionChanged)
            {
                AttachSelectedIcons(null);
            }

            if (showingAllGroups)
            {
                RebuildShortcutTiles();
                RefreshGroupSelectionState(selectedIndex);
            }
        }

        private void RebuildShortcutTiles()
            => _dragBusiness.RebuildShortcutTiles();

        private bool UsesManualLiveDragYieldAnimation()
            => _dragBusiness.UsesManualLiveDragYieldAnimation();

        private void EnsureBatchDragPlaceholderContainerVisible(int attempt = 0)
            => _dragBusiness.EnsureBatchDragPlaceholderContainerVisible(attempt);

        private bool MoveBatchDragPlaceholder(int targetIndex, string logReason)
            => _dragBusiness.MoveBatchDragPlaceholder(targetIndex, logReason);

        private int GetTileInsertUpperBound(IReadOnlyList<IconInfo> tiles)
            => _dragBusiness.GetTileInsertUpperBound(tiles);


        private void FinalizeBatchPlaceholderBlockPosition(string logReason)
            => _dragBusiness.FinalizeBatchPlaceholderBlockPosition(logReason);

        // 委托标记：完整拖拽重排序走 business controller
        private void CompleteDragReorder(
            bool committedFolderDrop,
            bool committedCrossGroupDrop,
            bool batchBlockDrag,
            bool animate,
            IconInfo? droppedIcon,
            IReadOnlyList<IconInfo> draggedIconsSnapshot,
            Action<IReadOnlyList<IconInfo>, bool, IconInfo?> restoreContainerVisibility)
            => _dragBusiness.CompleteDragReorder(
                committedFolderDrop, committedCrossGroupDrop, batchBlockDrag,
                animate, droppedIcon, draggedIconsSnapshot, restoreContainerVisibility);

        // 委托标记：All Groups 文件夹突变走 business controller
        private bool TryExecuteAllGroupsFolderMutation(
            LauncherAllGroupsFolderMutationKind kind,
            IconInfo subject,
            IconInfo target,
            int insertIndex = -1)
            => _dragBusiness.TryExecuteAllGroupsFolderMutation(kind, subject, target, insertIndex);

        // 委托标记：跨分组拖拽完成走 business controller
        private bool TryCommitCrossGroupDropAtDragEnd()
            => _dragBusiness.TryCommitCrossGroupDropAtDragEnd();

        // 委托标记：跨分组拖放走 business controller
        private bool TryCommitCrossGroupDrop(
            IReadOnlyList<IconInfo> icons,
            MenuInfo? targetMenu,
            string batchFinalizeReason,
            out bool wasAllGroups)
            => _dragBusiness.TryCommitCrossGroupDrop(icons, targetMenu, batchFinalizeReason, out wasAllGroups);

        private void TryUpdateBatchPlaceholderBlockFromPointer()
            => _dragBusiness.TryUpdateBatchPlaceholderBlockFromPointer();

        private bool IsDragPointOverShortcutIconArea(DragEventArgs e)
            => _dragBusiness.IsDragPointOverShortcutIconArea(e);

        private bool RestoreDraggedIconsAfterBatchReorder(bool cancelled = false)
        {
            LauncherBatchRestorePlan? restorePlan =
                _batchDragPlaceholders.PlanRestore(shortcutTiles, cancelled);
            if (restorePlan == null) return false;

            if (cancelled)
            {
                _batchDragPlaceholders.ApplyRestore(shortcutTiles, restorePlan);
                return false;
            }

            List<IconInfo> orderedIcons = restorePlan.Icons.ToList();
            int insertIndex = restorePlan.InsertIndex;

            var trailingIcons = new List<IconInfo>();
            var trailingPositions = new Dictionary<IconInfo, Point>();
            if (!appData.AppConfig.PMModel && IconListView != null)
            {
                IconListView.UpdateLayout();
                for (int i = insertIndex + 1; i < shortcutTiles.Count; i++)
                {
                    IconInfo icon = shortcutTiles[i];
                    if (LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon) || LauncherDragIconClassifier.IsDragYieldPlaceholder(icon)) continue;

                    trailingIcons.Add(icon);
                    if (IconListView.ContainerFromItem(icon) is ListViewItem item)
                    {
                        trailingPositions[icon] = GetShortcutContainerPoint(item);
                    }
                }
            }

            _batchDragPlaceholders.ApplyRestore(shortcutTiles, restorePlan);

            if (!appData.AppConfig.PMModel && orderedIcons.Count > 0 && IconListView != null)
            {
                var iconsSnapshot = orderedIcons.ToList();
                var trailingSnapshot = trailingIcons.ToList();
                var trailingPositionSnapshot = new Dictionary<IconInfo, Point>(trailingPositions);
                int releaseInsertIndex = insertIndex;
                DispatcherQueue.TryEnqueue(() =>
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
            // Inline AnimateBatchDragDropRelease
            int started = 0;
            if (icons.Count > 0)
            {
                var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
                int columns = Math.Max(1, ShortcutColumnCount_NoWrite);
                for (int i = 0; i < icons.Count; i++)
                {
                    if (IconListView.ContainerFromItem(icons[i]) is not ListViewItem item) continue;
                    if (item.ContentTemplateRoot is not FrameworkElement root) continue;
                    var off = LauncherBatchDropAnimationPlanner.GetReleaseOffset(i, columns);
                    BeginShortcutItemFlyPlacement(item, root, off.X, off.Y, LauncherBatchDropAnimationPlanner.GetStepDelay(i, ShortcutReorderStaggerDelay), easing);
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
                    BeginShortcutItemSlide(tItem, tRoot, dx, dy, ShortcutReorderAnimationDuration, LauncherBatchDropAnimationPlanner.GetStepDelay(icons.Count + i, ShortcutReorderStaggerDelay), easing);
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
                DispatcherQueue.TryEnqueue(() =>
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
                ShortcutLayoutFlyAnimationDuration,
                ShortcutReorderStaggerDelay,
                TimeSpan.FromMilliseconds(60));

            _pendingBatchDropReleaseIcons = icons.ToList();
            _dragOrchestrator.StartBatchDropReleaseCleanup(delay, _pendingBatchDropReleaseIcons, () =>
                FinishBatchDropReleaseCleanup(_pendingBatchDropReleaseIcons!));
        }

        private void FinishBatchDropReleaseCleanup(IReadOnlyList<IconInfo> icons)
        {

            StopAllLiveReorderAnimations();
            _shortcutTransitions.SetEnabled(false);

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
            RestoreVisibleShortcutContainerState();
            _shortcutTransitions.Restore();
        }


        private void HideDraggedShortcutContainers()
            => _dragBusiness.HideDraggedShortcutContainers();

        private void CompleteIconDragCleanup(bool restoreContainers)
            => _dragBusiness.CompleteIconDragCleanup(restoreContainers);

        private void LogIconDragState(string stage)
            => _dragBusiness.LogIconDragState(stage);

        private void ScheduleHideDraggedShortcutContainers()
            => _dragBusiness.ScheduleHideDraggedShortcutContainers();

        private int GetShortcutDropIndexFromPointer(IconInfo? skipIcon)
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
                tiles = shortcutTiles;
                tileLimit = GetTileInsertUpperBound(shortcutTiles);
            }
            else
            {
                return GetTileInsertUpperBound(shortcutTiles);
            }

            Point point = default;
            bool pointerResolved = TryGetDragPointerOverlayPosition(out Point overlayPos)
                && MainWindow.mainWindow?.DragOverlayCanvasHost is UIElement overlay
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

        private bool ShouldSkipDropHitTestLayoutSync()
        {
            return _isCustomDragActive
                && (IsMergedBatchDragActive() || UsesManualLiveDragYieldAnimation());
        }

        private bool TryUpdateFolderDropIntentFromPointer()
        {
            if (!_isCustomDragActive
                || _draggedIcons.Count != 1
                || _draggedIcon == null
                || _crossGroupPreview.IsActive
                || _dragStartedFromFolderOverlay)
            {
                SetFolderDropIntent(null);
                return false;
            }

            // 委托标记：文件夹意图更新走 business controller 的悬停延迟门控
            return _dragBusiness.UpdateFolderDropIntentActivation(
                _dragBusiness.ResolveFolderDropTargetFromPointer(allowCachedPointer: false));
        }

        private bool UpdateFolderDropIntentActivation(IconInfo? target)
        {
            bool isValid = target != null && IsValidFolderDropTarget(target);
            LauncherFolderDropIntentUpdate<IconInfo> update =
                _folderDropIntent.EvaluateCandidate(
                    target,
                    isValid,
                    Environment.TickCount64,
                    FolderDropHoverActivationDelayMs);
            if (update.ActiveTarget != null && IsValidFolderDropTarget(update.ActiveTarget))
                LogUtil.WriteLog($"FolderDrop intent: dragged={_draggedIcon?.Name_NoWrite}, target={update.ActiveTarget.Name_NoWrite}, targetIsFolder={update.ActiveTarget.IsFolder}, allGroups={showingAllGroups}");
            if (IsFolderDropReorderSuppressed())
            {
                StopAllLiveReorderAnimations();
                _batchDragState.ResetMoveThrottle();
                Point? fdiOrigin = null;
                if (update.ActiveTarget != null)
                {
                    Canvas? fdiOv = MainWindow.mainWindow?.DragOverlayCanvasHost;
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

        private IconInfo? ResolveFolderDropTargetFromPointer(
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
                        IconListView, shortcutTiles, fdp,
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

        private void TryUpdateSingleIconDropIndexFromPointer()
        {
            if (!_isCustomDragActive
                || _draggedIconsSuppressedFromTiles
                || _draggedIcon == null
                || IconListView == null)
            {
                return;
            }

            bool folderOverlayMoveOut = _dragStartedFromFolderOverlay
                && !IsPointerOverFolderOverlayPanelAtCurrentPointer();
            if (!folderOverlayMoveOut
                && !ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
                    _draggedIcons.Count,
                    showingAllGroups,
                    IsFolderDropReorderSuppressed(),
                    _crossGroupPreview.IsActive))
            {
                return;
            }

            bool hasPreview = TryResolveLauncherDragPreviewFromPointer(out LauncherDragPreview preview);
            if (hasPreview && preview.Kind != LauncherDropKind.ReorderTarget)
            {
                _singleIconDragPreview.ClearPending();
                return;
            }

            int resolvedIndex = hasPreview
                ? preview.ReorderIndex
                : GetShortcutDropIndexFromPointer(_draggedIcon);
            int targetIndex = Math.Clamp(
                resolvedIndex,
                0,
                Math.Max(0, shortcutTiles.Count - 1));
            // Inline UpdateSingleIconReorderPreviewTarget
            if (!TryStabilizeSingleIconReorderTarget(targetIndex)) return;
            int currentIndex = shortcutTiles.IndexOf(_draggedIcon);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                if (_singleIconDragPreview.TargetIndex != targetIndex)
                {
                    _singleIconDragPreview.CommitTarget(targetIndex);
                    // Inline PreviewSingleIconLiveYield
                    if (!_isCustomDragActive || _draggedIcon == null || _draggedIcons.Count != 1
                        || _draggedIconsSuppressedFromTiles || IconListView == null || appData.AppConfig.PMModel)
                        return;
                    int ci = shortcutTiles.IndexOf(_draggedIcon);
                    int sti = Math.Clamp(targetIndex, 0, Math.Max(0, GetTileInsertUpperBound(shortcutTiles)));
                    if (ci < 0 || ci == sti) { StopAllLiveReorderAnimations(); return; }
                    var vt = shortcutTiles.Where(LauncherDragIconClassifier.IsDataShortcutIcon).ToList();
                    int vci = vt.IndexOf(_draggedIcon);
                    if (vci < 0) return;
                    vt.RemoveAt(vci);
                    vt.Insert(Math.Clamp(sti, 0, vt.Count), _draggedIcon);
                    int cc = Math.Max(1, ShortcutColumnCount_NoWrite);
                    double sw = ShortcutItemSlotWidth_NoWrite > 0d ? ShortcutItemSlotWidth_NoWrite : 88d;
                    double sh = ShortcutItemSlotHeight_NoWrite > 0d ? ShortcutItemSlotHeight_NoWrite : 104d;
                    var curItems = shortcutTiles
                        .Where(LauncherDragIconClassifier.IsDataShortcutIcon)
                        .Select(ic => LauncherShortcutGridItemAdapter.Create(
                            ic,
                            cc,
                            preserveManualPosition: !showingAllGroups));
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
                        BeginShortcutItemPreviewOffset(itm, rt, ox, oy, LiveDragReorderDuration, TimeSpan.FromMilliseconds(Math.Min(step, 8) * LiveDragReorderStaggerDelay.TotalMilliseconds), ease);
                        affected.Add(itm);
                        step++;
                        if (step >= MaxLiveReorderAnimatedItems) break;
                    }
                    foreach (var itm in _itemAnimations.AnimatedItems.ToList())
                    {
                        if (!affected.Contains(itm))
                        {
                            if (ShouldAnimateInteraction() && itm.ContentTemplateRoot is FrameworkElement art
                                && _itemAnimations.TryGetTargetOffset(itm, out var tOff) && (Math.Abs(tOff.X) >= 0.5d || Math.Abs(tOff.Y) >= 0.5d))
                            { BeginShortcutItemPreviewOffset(itm, art, 0d, 0d, TimeSpan.FromMilliseconds(100), TimeSpan.Zero, ease); affected.Add(itm); }
                            else if (!_itemAnimations.TryGetTargetOffset(itm, out var cTgt) || Math.Abs(cTgt.X) >= 0.5d || Math.Abs(cTgt.Y) >= 0.5d)
                                StopItemReorderAnimation(itm, "singleLiveYieldUnaffected");
                        }
                    }
                }
            }
            else if (currentIndex >= 0 && _singleIconDragPreview.TargetIndex != targetIndex)
            {
                _singleIconDragPreview.CommitTarget(targetIndex);
                StopAllLiveReorderAnimations();
            }
            _singleIconDragPreview.CommitTarget(targetIndex);
        }

        private bool TryStabilizeSingleIconReorderTarget(int targetIndex)
            => _dragBusiness.TryStabilizeSingleIconReorderTarget(targetIndex);

        private void BeginShortcutItemPreviewOffset(
            ListViewItem item,
            FrameworkElement root,
            double offsetX,
            double offsetY,
            TimeSpan duration,
            TimeSpan beginTime,
            EasingFunctionBase easing)
            => _dragBusiness.BeginShortcutItemPreviewOffset(item, root, offsetX, offsetY, duration, beginTime, easing);

        private bool TryResolveLauncherDragPreviewFromPointer(
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

            Dictionary<IconInfo, LauncherGridRect> layout = BuildCurrentLauncherLayoutMap(IconListView, shortcutTiles);
            if (layout.Count == 0)
            {
                return false;
            }

            preview = LauncherDragSession.ResolvePointer(
                shortcutTiles,
                _draggedIcons,
                layout,
                new LauncherPoint(pointer.X, pointer.Y),
                GetTileInsertUpperBound(shortcutTiles));

            return true;
        }

        private IconInfo? ResolveFolderDropTargetAtPoint(Point pointer)
        {
            if (IconListView == null || _draggedIcons.Count != 1 || _draggedIcon == null)
            {
                return null;
            }

            Dictionary<IconInfo, LauncherGridRect> layout = BuildCurrentLauncherLayoutMap(
                IconListView,
                shortcutTiles);
            if (layout.Count == 0)
            {
                return null;
            }

            LauncherDragPreview preview = LauncherDragSession.ResolvePointer(
                shortcutTiles,
                _draggedIcons,
                layout,
                new LauncherPoint(pointer.X, pointer.Y),
                GetTileInsertUpperBound(shortcutTiles));
            return preview.Kind is LauncherDropKind.FolderTarget or LauncherDropKind.CreateFolderTarget
                && IsValidFolderDropTarget(preview.TargetIcon)
                    ? preview.TargetIcon
                    : null;
        }

        private Dictionary<IconInfo, LauncherGridRect> BuildCurrentLauncherLayoutMap(
            ListView listView,
            IReadOnlyList<IconInfo> tiles)
        {
            int columnCount = Math.Max(1, ShortcutColumnCount_NoWrite);
            double slotWidth = ShortcutItemSlotWidth_NoWrite > 0d ? ShortcutItemSlotWidth_NoWrite : 88d;
            double slotHeight = ShortcutItemSlotHeight_NoWrite > 0d ? ShortcutItemSlotHeight_NoWrite : 104d;
            return LauncherDropHitTestAdapter.BuildLogicalLayout(
                listView,
                tiles,
                new LauncherGridMetrics(columnCount, slotWidth, slotHeight),
                synchronizeLayout: !ShouldSkipDropHitTestLayoutSync(),
                include: LauncherDragIconClassifier.IsDataShortcutIcon);
        }

        /// <summary>单图标/文件夹拖拽落位；占位符拖拽由 RestoreDraggedIconsAfterBatchReorder 处理。</summary>
        private bool SetManualGridPosFromPointer(IconInfo icon)
        {
            if (showingAllGroups
                || IconListView == null
                || !TryGetDragPointerOverlayPosition(out Point overlayPos)
                || MainWindow.mainWindow?.DragOverlayCanvasHost is not UIElement overlay)
                return false;
            UIElement coordTarget = IconListView.ItemsPanelRoot as UIElement ?? IconListView;
            if (!LauncherDropHitTestAdapter.TryTranslatePoint(overlay, coordTarget, overlayPos, out Point point))
                return false;
            int cc = Math.Max(1, ShortcutColumnCount_NoWrite);
            double sw = ShortcutItemSlotWidth_NoWrite > 0d ? ShortcutItemSlotWidth_NoWrite : 88d;
            double sh = ShortcutItemSlotHeight_NoWrite > 0d ? ShortcutItemSlotHeight_NoWrite : 104d;
            int cs = ShortcutLayoutCalculator.ClampFolderColumnSpan(LauncherShortcutItemSpanAdapter.GetColumnSpan(icon), cc);
            int col = Math.Clamp((int)Math.Floor(point.X / sw), 0, Math.Max(0, cc - cs));
            int row = Math.Max(0, (int)Math.Floor(point.Y / sh));
            var result = _launcherCommandHandler.UpdateGridLayout(new UpdateLauncherGridLayoutCommand(new[] { new LauncherGridPlacement(icon.IconId, col, row) }));
            return result.Success && result.Changed;
        }

        private void RestoreSingleIconToDragSourceBeforeFolderCommit()
        {
            if (_draggedIcon == null
                || _dragSourceIndex < 0
                || _draggedIconsSuppressedFromTiles)
            {
                return;
            }

            int currentIndex = shortcutTiles.IndexOf(_draggedIcon);
            if (currentIndex < 0 || currentIndex == _dragSourceIndex)
            {
                return;
            }

            int targetIndex = Math.Clamp(_dragSourceIndex, 0, Math.Max(0, shortcutTiles.Count - 1));
            shortcutTiles.Move(currentIndex, targetIndex);
            _singleIconDragPreview.CommitTarget(targetIndex);
        }




        private void Icon_Tapped(object sender, TappedRoutedEventArgs e)
            => _contextMenuHandler.HandleIconTapped(sender, e, LauncherVisualTreeHelper.GetIconInfoFromSender);

        private void Icon_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
            => _contextMenuHandler.HandleIconDoubleTapped(sender, e, LauncherVisualTreeHelper.GetIconInfoFromSender);

        /// <summary>
        /// 管理员方式启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconAdminStart(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleIconAdminStart(sender, e, ResolveIconForContextMenu);

        private void ShowInExplore(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleShowInExplore(sender, e, ResolveIconForContextMenu);

        private void OpenFolderFromContext(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleOpenFolderFromContext(sender, e, ResolveIconForContextMenu);

        private void SetFolderSmall(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleSetFolderSmall(sender, e, ResolveIconForContextMenu);

        private void SetFolderMedium(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleSetFolderMedium(sender, e, ResolveIconForContextMenu);

        private void SetFolderLarge(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleSetFolderLarge(sender, e, ResolveIconForContextMenu);

        private void SetFolderDropTargetHighlight(IconInfo? folder)
            => _dragBusiness.SetFolderDropTargetHighlight(folder);

        private void ApplyFolderDropTargetVisualState(IconInfo? target, bool active)
            => _dragBusiness.ApplyFolderDropTargetVisualState(target, active);

        private void SetFolderDropIntent(IconInfo? target)
            => _dragBusiness.SetFolderDropIntent(target);

        private bool IsFolderDropReorderSuppressed() =>
            _dragBusiness.IsFolderDropReorderSuppressed();

        private void ClearFolderDropTargetHighlight()
            => _dragBusiness.ClearFolderDropTargetHighlight();

        private void OpenFolder(IconInfo folder) => _folderOverlay.OpenFolder(folder);
        private void CloseFolderOverlay() => _folderOverlay.Close();
        private void CloseFolderOverlayClick(object sender, RoutedEventArgs e) => _folderOverlay.HandleCloseClick(sender, e);
        private void FolderOverlay_BackdropTapped(object sender, TappedRoutedEventArgs e) => _folderOverlay.HandleBackdropTapped(sender, e);
        private void FolderOverlayPanel_Tapped(object sender, TappedRoutedEventArgs e) => _folderOverlay.HandlePanelTapped(sender, e);
        private void FolderItem_Click(object sender, ItemClickEventArgs e) => _folderOverlay.HandleItemClick(sender, e);
        private void RefreshFolderOverlay() => _folderOverlay.RefreshFolderOverlay();
        private void FolderBackButton_Click(object sender, RoutedEventArgs e) => _folderOverlay.HandleBackButton(sender, e);
        private void FolderItemsListView_DragOver(object sender, DragEventArgs e) => _folderOverlay.HandleDragOver(sender, e);

        private void FolderItemsListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            IconInfo? icon = e.Items.OfType<IconInfo>().FirstOrDefault(item => !LauncherDragIconClassifier.IsAddShortcutPlaceholder(item));
            if (icon == null)
            {
                e.Cancel = true;
                return;
            }

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
            e.Data.Properties["DragType"] = "IconDrag";
            e.Data.Properties["IconInfo"] = icon;
            e.Data.Properties["IconDragCount"] = 1;
            MainWindow.mainWindow?.SetIconDragActive(true);
            ImageUtil.EnsureIconBitmap(icon);
            ImageSource? iconSource = icon.BitmapImage_NoWrite ?? icon.BitmapImage;
            FrameworkElement? captureTarget = FolderItemsListView?.ContainerFromItem(icon) as FrameworkElement;
            var overlay = MainWindow.mainWindow?.DragOverlayCanvasHost;
            bool useDragOverlayPreview = overlay != null && (iconSource != null || captureTarget != null);
            BeginDragPointerTracking(registerSystemDragSuppressHandlers: true);

            if (useDragOverlayPreview)
            {
                _dragPreviewController.End(animate: false);
                var previewRequest = new LauncherDragPreviewRequest
                {
                    IconSource = iconSource,
                    CaptureTarget = captureTarget,
                    Overlay = overlay!,
                    CaptureWidth = LauncherShortcutDragPreviewSize,
                    CaptureHeight = LauncherShortcutDragPreviewSize,
                    AnimatePreview = !appData.AppConfig.PMModel,
                    DragCount = 1
                };
                _ = StartIconDragPreviewAsync(previewRequest);
            }
        }

        private void WorkspaceGroupIconListReplaced(MenuInfo menu)
        {
            if (showingAllGroups || ReferenceEquals(_workspaceNavigation.GetActiveGroup(appData), menu))
            {
                if (!showingAllGroups)
                {
                    AttachSelectedIcons(menu.IconList);
                    appData.AppConfig.SelectedMenuIcons = menu.IconList;
                }

                RebuildShortcutTiles();
            }
        }

        private void WorkspaceGroupIconsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (showingAllGroups && !_isCustomDragActive)
            {
                RebuildShortcutTiles();
            }
        }

        private void FolderItemsListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            bool committedMoveOut = false;
            if (_dragStartedFromFolderOverlay && _draggedIcon != null)
            {
                IconInfo? srcFolder = folderOverlayState.Folder;
                if (srcFolder?.IsFolder == true && srcFolder.FolderItems.Contains(_draggedIcon)
                    && !IsPointerOverFolderOverlayPanelAtCurrentPointer())
                {
                    if (TryMoveOutOfAllGroupsFolder(srcFolder, _draggedIcon, GetAllGroupsMoveOutInsertIndex(srcFolder)))
                    {
                        SetManualGridPosFromPointer(_draggedIcon);
                        SyncFolderOverlayAfterFolderMutation();
                        RebuildShortcutTiles();
                        MoveDraggedIconToResolvedFlatTarget(_draggedIcon);
                        if (showingAllGroups) CommitCurrentShortcutOrder();
                        committedMoveOut = true;
                    }
                    else
                    {
                        var mc = _launcherContextMenu.CreateMoveOutOfFolderCommand(srcFolder, _draggedIcon, insertIndex: -1);
                        if (mc != null && ExecuteLauncherCommand(mc))
                        {
                            SetManualGridPosFromPointer(_draggedIcon);
                            SyncFolderOverlayAfterFolderMutation();
                            RebuildShortcutTiles();
                            MoveDraggedIconToResolvedFlatTarget(_draggedIcon);
                            if (showingAllGroups) CommitCurrentShortcutOrder();
                            committedMoveOut = true;
                        }
                    }
                }
            }
            if (committedMoveOut)
            {
                SyncFolderOverlayAfterFolderMutation();
                RebuildShortcutTiles();
                CommitAllGroupsProjection();
            }
            else
            {
                bool committedCrossGroupDrop = TryCommitCrossGroupDropAtDragEnd();
                bool committedFolderDrop = !committedCrossGroupDrop
                    && !_isCrossGroupDrag
                    && TryCommitFolderDrop(ResolveActivatedFolderDropTargetAtDragEnd());
                if (committedFolderDrop || committedCrossGroupDrop)
                {
                    SyncFolderOverlayAfterFolderMutation();
                    RebuildShortcutTiles();
                    CommitAllGroupsProjection();
                }
            }

            CompleteFolderOverlayItemDrag();
        }

        private void CompleteFolderOverlayItemDrag()
        {
            EndDragPointerTracking();
            MainWindow.mainWindow?.SetIconDragActive(false);
            _dragPreviewController.End(animate: false);

            _isCustomDragActive = false;
            CompleteIconDragCleanup(restoreContainers: true);
        }

        private void FolderOverlay_DragOver(object sender, DragEventArgs e)
        {
            if (!_dragStartedFromFolderOverlay || _draggedIcon == null)
            {
                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                return;
            }

            SuppressSystemDragVisual(e);
            UpdateDragPreviewFromDragEvent(e);
            SetFolderDropIntent(null);

            if (!IsPointerOverFolderOverlayPanelAtCurrentPointer())
            {
                TryUpdateSingleIconDropIndexFromPointer();
            }

            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.Handled = true;
        }

        private bool IsPointerOverFolderOverlayPanelAtCurrentPointer()
        {
            if (FolderOverlay.Visibility != Visibility.Visible
                || FolderOverlayPanel == null
                || FolderOverlayPanel.ActualWidth <= 0d
                || FolderOverlayPanel.ActualHeight <= 0d)
            {
                return false;
            }

            if (!TryGetDragPointerOverlayPosition(out Point overlayPos)
                || MainWindow.mainWindow?.DragOverlayCanvasHost is not UIElement canvas)
            {
                return false;
            }

            Point inPanel = canvas.TransformToVisual(FolderOverlayPanel).TransformPoint(overlayPos);
            return inPanel.X >= 0d
                && inPanel.Y >= 0d
                && inPanel.X <= FolderOverlayPanel.ActualWidth
                && inPanel.Y <= FolderOverlayPanel.ActualHeight;
        }

        private int GetAllGroupsMoveOutInsertIndex(IconInfo sourceFolder)
        {
            List<IconInfo> flat = _allGroupsOrder.HasCurrentOrder
                ? _allGroupsOrder.CopyCurrentOrder()
                : GetOrderedIconsFromTiles();
            int folderIndex = flat.IndexOf(sourceFolder);
            return folderIndex >= 0 ? folderIndex + 1 : flat.Count;
        }

        private bool TryMoveOutOfAllGroupsFolder(IconInfo sourceFolder, IconInfo child, int insertIndex)
        {
            return TryExecuteAllGroupsFolderMutation(
                LauncherAllGroupsFolderMutationKind.MoveOut,
                child,
                sourceFolder,
                insertIndex);
        }

        private void MoveDraggedIconToResolvedFlatTarget(IconInfo draggedIcon)
        {
            int targetFlatIndex = _singleIconDragPreview.TargetIndex >= 0
                ? _singleIconDragPreview.TargetIndex
                : GetShortcutDropIndexFromPointer(draggedIcon);
            int currentIndex = shortcutTiles.IndexOf(draggedIcon);
            if (currentIndex < 0 || targetFlatIndex < 0)
            {
                return;
            }

            targetFlatIndex = Math.Clamp(targetFlatIndex, 0, Math.Max(0, shortcutTiles.Count - 1));
            if (currentIndex != targetFlatIndex)
            {
                shortcutTiles.Move(currentIndex, targetFlatIndex);
            }
        }

        private void FolderItemsListView_Drop(object sender, DragEventArgs e)
        {
            if (_draggedIcon == null || folderOverlayState.Folder?.IsFolder != true)
            {
                return;
            }

            IconInfo currentFolder = folderOverlayState.Folder;
            IconInfo? itemTarget = ResolveFolderOverlayItemTarget(e);
            bool belongsToCurrentFolder = currentFolder.FolderItems.Contains(_draggedIcon);
            int reorderIndex = belongsToCurrentFolder
                ? GetFolderOverlayDropIndexFromPointer(e, _draggedIcon)
                : 0;
            LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(
                new LauncherFolderDropCommandRequest(
                    _draggedIcon.IconId,
                    LauncherShortcutDropTraitsAdapter.From(_draggedIcon),
                    currentFolder.IconId,
                    itemTarget?.IconId,
                    itemTarget == null
                        ? null
                        : LauncherShortcutDropTraitsAdapter.From(itemTarget),
                    belongsToCurrentFolder,
                    reorderIndex));
            bool committed = decision.Command != null
                && ExecuteLauncherCommand(decision.Command);

            if (committed)
            {
                SyncFolderOverlayAfterFolderMutation();
                RebuildShortcutTiles();
                CommitAllGroupsProjection();
                e.Handled = true;
            }
        }

        private IconInfo? ResolveFolderOverlayItemTarget(DragEventArgs e)
        {
            IconInfo? currentFolder = folderOverlayState.Folder;
            if (currentFolder?.IsFolder != true || FolderItemsListView == null)
            {
                return null;
            }

            return LauncherDropHitTestAdapter.FindCenteredTarget(
                FolderItemsListView,
                currentFolder.FolderItems,
                e.GetPosition(FolderItemsListView),
                child => !ReferenceEquals(child, _draggedIcon));
        }

        private int GetFolderOverlayDropIndexFromPointer(DragEventArgs e, IconInfo? skipIcon)
        {
            IconInfo? currentFolder = folderOverlayState.Folder;
            if (currentFolder?.IsFolder != true || FolderItemsListView == null)
            {
                return 0;
            }

            return LauncherDropHitTestAdapter.ResolveRealizedListDropIndex(
                FolderItemsListView,
                currentFolder.FolderItems,
                e.GetPosition(FolderItemsListView),
                Math.Max(0, currentFolder.FolderItems.Count - 1),
                synchronizeLayout: true,
                exclude: candidate => ReferenceEquals(candidate, skipIcon));
        }

        private void MoveFolderChildOut(object sender, RoutedEventArgs e)
        {
            if (((MenuFlyoutItem)sender).Tag is not IconInfo child)
            {
                return;
            }

            IconInfo? folder = folderOverlayState.Folder;
            if (folder?.IsFolder != true)
            {
                return;
            }

            if (TryMoveOutOfAllGroupsFolder(folder, child, GetAllGroupsMoveOutInsertIndex(folder)))
            {
                ResetLauncherGridPosition(child);
                SyncFolderOverlayAfterFolderMutation();
                RebuildShortcutTiles();
                CommitAllGroupsProjection();
                return;
            }

            LauncherMutationCommand? moveCommand = _launcherContextMenu.CreateMoveOutOfFolderCommand(
                folder,
                child,
                insertIndex: -1);
            if (moveCommand == null || !ExecuteLauncherCommand(moveCommand))
            {
                return;
            }

            ResetLauncherGridPosition(child);
            SyncFolderOverlayAfterFolderMutation();
            RebuildShortcutTiles();
        }

        private void ResetLauncherGridPosition(IconInfo icon)
        {
            LauncherCommandResult result = _launcherCommandHandler.UpdateGridLayout(
                new UpdateLauncherGridLayoutCommand(new[]
                {
                    new LauncherGridPlacement(icon.IconId, -1, -1)
                }));
            if (!result.Success)
            {
                LogUtil.WriteLog(result.Error ?? "重置 Launcher 网格位置失败。");
            }
        }

        /// <summary>
        /// 拖动添加项目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Wrap_Drop(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Handled = true;

            if (!e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            string[] paths = items
                .OfType<global::Windows.Storage.StorageFile>()
                .Select(file => file.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            if (paths.Length == 0)
            {
                return;
            }

            EnsureWritableGroupSelected();
            MenuInfo? targetMenu = _workspaceNavigation.GetActiveGroup(appData)
                ?? appData.MenuList.FirstOrDefault();

            if (targetMenu == null || string.IsNullOrWhiteSpace(targetMenu.MenuId))
            {
                return;
            }

            LauncherExternalImportResult? result = await _externalImport.ImportAsync(
                targetMenu.MenuId,
                paths);
            if (result == null)
            {
                return;
            }

            if (result.AddedCount > 0)
            {
                RebuildShortcutTiles();
            }
            if (result.HasErrors)
            {
                string warning = result.Error
                    ?? $"已添加 {result.AddedCount} 项，另有 {result.Errors.Count} 项无法解析。";
                GrowlService.Warning(warning);
            }
        }

        private void SyncFolderOverlayAfterFolderMutation()
        {
            if (!folderOverlayState.IsOpen)
            {
                return;
            }

            while (folderOverlayState.Folder != null
                && !AllGroupsShortcutViewService.IsAllGroupsRootItem(
                    appData.AppConfig.AllGroupsIconList,
                    folderOverlayState.Folder)
                && !ShortcutFolderTreeService.TryFindContainer(appData.MenuList, folderOverlayState.Folder, out _))
            {
                if (folderOverlayState.CanGoBack)
                {
                    folderOverlayState.Back();
                }
                else
                {
                    CloseFolderOverlay();
                    return;
                }
            }

            if (folderOverlayState.IsOpen)
            {
                RefreshFolderOverlay();
            }
        }

        /// <summary>
        /// 从列表删除图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RemoveIcon(object sender, RoutedEventArgs e)
            => await _contextMenuHandler.HandleRemoveIcon(sender, e, ResolveIconForContextMenu);

        private void PropertyConfig(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandlePropertyConfig(sender, e, ResolveIconForContextMenu);

        private void RenameIcon(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleRenameIcon(sender, e, ResolveIconForContextMenu, DispatcherQueue);

        private void MenuIcon_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            RunTimeStatus.MOUSE_ENTER_ICON = true;
            IconInfo iconInfo = LauncherVisualTreeHelper.GetIconInfoFromSender(sender);
            if (iconInfo == null) return;
            if (iconInfo.IsAddShortcut_NoWrite) return;

            HoverAnimationHelper.ApplyHoverScale(sender, 1.08, 120, appData.AppConfig.PMModel);
        }

        private void MenuIcon_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            RunTimeStatus.MOUSE_ENTER_ICON = false;

            HoverAnimationHelper.ApplyHoverScale(sender, 1, 180, appData.AppConfig.PMModel);
        }

        /// <summary>
        /// 添加URL项目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddUrlIcon(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleAddUrlIcon(sender, e);

        /// <summary>
        /// 添加系统项目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddSystemIcon(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleAddSystemIcon(sender, e);

        public void ShowAddUrlPanel()
        {
            EnsureInlineUrlPanelLoaded();
            EnsureInlineSystemPanelLoaded();
            _inlineAddPanel.ShowAddUrlPanel(BuildInlineAddPanelViews());
        }

        public void ShowAddSystemPanel()
        {
            EnsureInlineSystemPanelLoaded();
            EnsureInlineUrlPanelLoaded();
            _inlineAddPanel.ShowAddSystemPanel(BuildInlineAddPanelViews());
        }

        private void BackInlinePanel(object sender, RoutedEventArgs e)
        {
            _inlineAddPanel.HandleBackInlinePanel(sender, e, BuildInlineAddPanelViews());
        }

        private void ShowShortcutPanel()
        {
            _inlineAddPanel.ShowShortcutPanel(BuildInlineAddPanelViews());
        }

        private void EnsureInlineUrlPanelLoaded()
        {
            if (InlineUrlPanel == null)
            {
                FindName(nameof(InlineUrlPanel));
            }

            if (InlineUrlHost == null)
            {
                FindName(nameof(InlineUrlHost));
            }
        }

        private void EnsureInlineSystemPanelLoaded()
        {
            if (InlineSystemPanel == null)
            {
                FindName(nameof(InlineSystemPanel));
            }

            if (InlineSystemTabs == null)
            {
                FindName(nameof(InlineSystemTabs));
            }

            if (InlineSystemItems == null)
            {
                FindName(nameof(InlineSystemItems));
            }
        }

        private void EnsureInlineSystemItemsDataContext()
        {
            if (InlineSystemItems != null && InlineSystemItems.DataContext != inlineSystemVm)
            {
                InlineSystemItems.DataContext = inlineSystemVm;
            }
        }

        private void InlineSystemTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => _inlineAddPanel.HandleInlineSystemTabsSelectionChanged(sender, e, BuildInlineAddPanelViews());

        private void LoadInlineSystemItems()
        {
            // Ensure elements are loaded before controller access
            if (InlineSystemPanel == null || InlineSystemPanel.Visibility != Visibility.Visible) return;
            var views = BuildInlineAddPanelViews();
            _inlineAddPanel.HandleInlineSystemTabsSelectionChanged(this, null!, views);
        }

        private void InlineSystemSearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _inlineAddPanel.HandleInlineSystemSearchBoxTextChanged(sender, e, BuildInlineAddPanelViews());

        private void ApplyInlineSystemItemsUpdate(
            LauncherInlineSystemItemsUpdate<IconInfo> update)
        {
            _inlineAddPanel.ApplySystemItemsUpdate(update, InlineSystemLoading);
        }

        private InlineAddPanelViews BuildInlineAddPanelViews() => new()
        {
            SectionTitle = SectionTitle,
            TitleRow = TitleRow,
            TitleBar = TitleBar,
            BackInlineButton = BackInlineButton,
            GroupBarHost = GroupBarHost,
            WrapUFG = EnsureWrapUFG(),
            InlineUrlPanel = InlineUrlPanel,
            InlineUrlHost = InlineUrlHost,
            InlineSystemPanel = InlineSystemPanel,
            InlineSystemTabs = InlineSystemTabs,
            InlineSystemSearchBox = InlineSystemSearchBox,
            InlineSystemSearchPlaceholder = InlineSystemSearchPlaceholder,
            InlineSystemLoading = InlineSystemLoading,
            InlineSystemItems = InlineSystemItems,
        };

        /// <summary>
        /// 搜索Card点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VerticalCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //隐藏搜索框
            if (RunTimeStatus.SEARCH_BOX_SHOW)
            {
                MainWindow.mainWindow.HidedSearchBox();
            }
        }

        /// <summary>
        /// 锁定/解锁主面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LockAppPanel(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleLockAppPanel(sender, e);

        private void WrapCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (RunTimeStatus.LOCK_APP_PANEL)
            {
                CardLockCM.Text = "解锁主面板";
            }
            else
            {
                CardLockCM.Text = "锁定主面板";
            }
        }



        /// <summary>
        /// 菜单结果icon 列表鼠标滚轮预处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void IconListView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (RunTimeStatus.ICONLIST_MOUSE_WHEEL)
            {
                RunTimeStatus.MOUSE_WHEEL_WAIT_MS = 500;
            }
            else
            {
                RunTimeStatus.ICONLIST_MOUSE_WHEEL = true;

                // 直接等待剩余时间，无需逐毫秒轮询
                await System.Threading.Tasks.Task.Delay(RunTimeStatus.MOUSE_WHEEL_WAIT_MS);
                RunTimeStatus.MOUSE_WHEEL_WAIT_MS = 100;
                RunTimeStatus.ICONLIST_MOUSE_WHEEL = false;
            }

        }

        /// <summary>
        /// 控制图标标题显示及隐藏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTitle_Click(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleShowTitleClick(sender, e);

        private void BatchHandle(object sender, RoutedEventArgs e)
            => _contextMenuHandler.HandleBatchHandle(sender, e);

        #region 框选与多选

        private bool IsDraggedIcon(IconInfo? icon) => _dragBusiness.IsDraggedIcon(icon);

        private void Icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            IconInfo? icon = LauncherVisualTreeHelper.GetIconInfoFromSender(sender);
            if (icon != null)
            {
                _iconSelection.HandleItemPointerPressed(
                    icon,
                    e,
                    RunTimeStatus.SEARCH_BOX_HIDED_300);
            }
        }

        private void WrapUFG_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CommitGroupEditOnOutsidePointer(e.OriginalSource as DependencyObject);
            _iconSelection.HandleHostPointerPressed(e);
        }

        private void WrapUFG_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _iconSelection.HandleHostPointerMoved(e);
        }

        private void WrapUFG_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _iconSelection.HandleHostPointerReleased(e);
        }

        private void WrapUFG_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _iconSelection.HandleHostPointerCanceled(e);
        }

        private bool TryMoveIconsBetweenGroups(IEnumerable<IconInfo> icons, MenuInfo targetMenu)
        {
            bool moved = false;
            foreach (IconInfo icon in icons)
            {
                if (LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon)) continue;
                if (targetMenu.IconList != null && !targetMenu.IsEncrypt)
                {
                    MenuInfo? src = FindGroupContainingIcon(icon);
                    if (src != null && !ReferenceEquals(src, targetMenu))
                    {
                        int ins = Math.Max(0, targetMenu.IconList.Count - (targetMenu.IconList.Contains(icon) ? 1 : 0));
                        moved |= ExecuteLauncherCommand(new LauncherMutationCommand(
                            LauncherMutationKind.MoveToGroup, ShortcutIds: new[] { icon.IconId },
                            TargetGroupId: targetMenu.MenuId, InsertIndex: ins));
                    }
                }
            }

            return moved;
        }

        private void ClearDraggedIconsState()
            => _dragBusiness.ClearDraggedIconsState();

        #endregion

        private void IconListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            LogIconDragState("starting");
            var selectedItems = new List<object>();
            foreach (object item in e.Items)
            {
                selectedItems.Add(item);
            }

            ShortcutDragStartItems<object> resolvedDragStart = ShortcutDragPolicy.ResolveDragStartItems(
                selectedItems,
                item => item is IconInfo icon && icon.IsAddShortcut_NoWrite,
                item => item is IconInfo icon && !LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon));
            if (resolvedDragStart.Cancel)
            {
                e.Cancel = true;
                return;
            }

            ApplyResolvedDragStartingItems(e, resolvedDragStart.Items);


            if (!TryBeginIconDragStartingCore(
                    e,
                    resolvedDragStart.Items,
                    out LauncherDragPreviewRequest? previewRequest))
            {
                return;
            }

            if (previewRequest != null)
            {
                _ = StartIconDragPreviewAsync(previewRequest);
            }
        }

        private void ApplyResolvedDragStartingItems(
            DragItemsStartingEventArgs e,
            IReadOnlyList<object> dragItems)
        {
            bool sameItems = e.Items.Count == dragItems.Count;
            if (sameItems)
            {
                for (int i = 0; i < dragItems.Count; i++)
                {
                    if (ReferenceEquals(e.Items[i], dragItems[i])) continue;

                    sameItems = false;
                    break;
                }
            }

            if (sameItems)
            {
                return;
            }

            _iconSelection.ReplaceSelectionObjects(dragItems);

            e.Items.Clear();
            foreach (object item in dragItems)
            {
                e.Items.Add(item);
            }
        }

        private bool TryBeginIconDragStartingCore(
            DragItemsStartingEventArgs e,
            IReadOnlyList<object> selectedItems,
            out LauncherDragPreviewRequest? previewRequest)
        {
            previewRequest = null;

            foreach (object item in selectedItems)
            {
                if (item is IconInfo icon && icon.IsAddShortcut_NoWrite)
                {
                    e.Cancel = true;
                    return false;
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
                e.Cancel = true;
                return false;
            }

            _draggedIcon = _draggedIcons[0];
            _iconSelection.EnsurePrimaryDragSelection(_draggedIcon, _draggedIcons.Count);
            _dragSourceIndex = shortcutTiles.IndexOf(_draggedIcon);
            _singleIconDragPreview.Begin(_dragSourceIndex);
            _batchDragState.ResetMoveThrottle();
            _liveReorderSlideGeneration++;
            _folderDropCommittedByDropEvent = false;
            _isCustomDragActive = true;
            _isCrossGroupDrag = false;
            MainWindow.mainWindow?.SetIconDragActive(true);

            if (showingAllGroups)
            {
                _allGroupsOrder.CaptureDragStart(GetOrderedIconsFromTiles());
            }

            SuppressGroupTabReorderForIconDrag();
            _shortcutTransitions.Apply();
            StopAllLiveReorderAnimations();

            e.Data.Properties.Add("DragType", "IconDrag");
            e.Data.Properties.Add("IconInfo", _draggedIcon);
            e.Data.Properties.Add("IconDragCount", _draggedIcons.Count);

            _iconSelection.PrepareDragVisuals(_draggedIcons);

            ImageUtil.EnsureIconBitmap(_draggedIcon);
            double captureWidth = LauncherShortcutDragPreviewSize;
            double captureHeight = LauncherShortcutDragPreviewSize;
            bool animatePreview = !appData.AppConfig.PMModel;
            var overlay = MainWindow.mainWindow?.DragOverlayCanvasHost;
            ImageSource? iconSource = _draggedIcon.BitmapImage_NoWrite ?? _draggedIcon.BitmapImage;

            ListViewItem? primaryContainer = IconListView.ContainerFromItem(_draggedIcon) as ListViewItem;
            FrameworkElement? captureTarget = primaryContainer != null
                ? LauncherVisualTreeHelper.FindShortcutDragCaptureTarget(primaryContainer)
                : null;

            var dragPolicy = ShortcutDragPolicy.ForDragCount(_draggedIcons.Count);
            bool useBatchPlaceholderDrag = dragPolicy.UseBatchPlaceholderDrag;
            bool useCustomOverlayPreview = useBatchPlaceholderDrag;
            bool useDragOverlayPreview = overlay != null
                && (_draggedIcons.Count == 1 || iconSource != null || captureTarget != null);

            if (useBatchPlaceholderDrag)
            {
                _iconListReorderSuppression.Suppress(
                    IconListView,
                    LauncherIconListReorderSuppressionReason.BatchDrag);
                _dragOrchestrator.RegisterSystemDragVisualSuppression(
                    IconListView,
                    MainWindow.mainWindow?.Content as UIElement);
                IconListView?.UpdateLayout();
                foreach (IconInfo icon in _draggedIcons)
                {
                    if (IconListView?.ContainerFromItem(icon) is ListViewItem container)
                        ApplyDraggedItemContainerState(container, icon);
                }
                _batchDragPlaceholders.TryBegin(shortcutTiles, _draggedIcons);
                if (dragPolicy.RestrictDragItemsToPlaceholder)
                {
                    _batchDragPlaceholders.RestrictDragItemsToPrimaryPlaceholder(e.Items);
                }
                _shortcutTransitions.Apply();
                IconListView?.UpdateLayout();
                RestoreVisibleShortcutContainerState();
                EnsureBatchDragPlaceholderContainerVisible();
            }

            if (useDragOverlayPreview || useBatchPlaceholderDrag)
            {
                ScheduleHideDraggedShortcutContainers();
            }

            BeginDragPointerTracking(registerSystemDragSuppressHandlers: useDragOverlayPreview || useBatchPlaceholderDrag);

            if (useDragOverlayPreview)
            {
                _dragPreviewController.End(animate: false);
                captureTarget?.UpdateLayout();
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

            return true;
        }

        private System.Threading.Tasks.Task StartIconDragPreviewAsync(
            LauncherDragPreviewRequest request)
        {
            return _dragPreviewController.BeginAsync(request);
        }

        private void BeginDragPointerTracking(bool registerSystemDragSuppressHandlers = false)
        {
            var windowRoot = MainWindow.mainWindow?.Content as UIElement;
            _dragOrchestrator.BeginPointerTracking(
                IconListView,
                windowRoot,
                (s, e) => OnDragPointerMoved(s, e),
                (s, e) =>
                {
                    if (!e.DataView.Properties.ContainsKey("IconInfo")) return;
                    SuppressSystemDragVisual(e);
                });
        }

        private void EndDragPointerTracking()
        {
            var windowRoot = MainWindow.mainWindow?.Content as UIElement;
            _dragOrchestrator.EndPointerTracking(IconListView, windowRoot);
        }

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref NativeWindowMethods.POINT lpPoint);

        private void OnDragPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // 性能优化：PointerMoved 只保存最新坐标到缓存。
            // 实际换位计算由 16ms 定时器每帧执行一次，
            // 避免每秒 120+ 次 PointerMoved 事件都触发换位计算。
            if (!_isCustomDragActive) return;

            // 保存坐标到指针缓存，供定时器下一帧读取
            var overlay = MainWindow.mainWindow?.DragOverlayCanvasHost;
            if (overlay != null)
            {
                Point pos = e.GetCurrentPoint(overlay).Position;
                _dragPointerCache.Store(new rainspace.Features.Launcher.Application.LauncherPoint(pos.X, pos.Y));
            }
        }

        private void UpdateDragPreviewPosition(double x, double y)
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

        private void SuppressSystemDragVisual(DragEventArgs e)
        {
            if (!_isCustomDragActive) return;
            if (_draggedIcons.Count <= 1 && !_dragPreviewController.HasPreview) return;

            e.DragUIOverride.Caption = string.Empty;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsGlyphVisible = false;

            if (_draggedIcons.Count > 1)
            {
                try
                {
                    e.DragUIOverride.SetContentFromBitmapImage(GetEmptyDragBitmapImage());
                    e.DragUIOverride.IsContentVisible = true;
                }
                catch
                {
                    e.DragUIOverride.IsContentVisible = false;
                }
            }
            else
            {
                e.DragUIOverride.IsContentVisible = false;
            }

        }

        private static BitmapImage GetEmptyDragBitmapImage()
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

        private void UpdateDragPreviewFromDragEvent(DragEventArgs e)
        {
            if (!_isCustomDragActive || !_dragPreviewController.HasPreview) return;

            var overlay = MainWindow.mainWindow?.DragOverlayCanvasHost;
            if (overlay == null) return;

            Point pos = e.GetPosition(overlay);
            UpdateDragPreviewPosition(pos.X, pos.Y);
        }

        private void IconListView_DragOver(object sender, DragEventArgs e)
        {
            // 性能优化：DragOver 只负责隐藏系统拖拽 UI 和设置允许操作。
            // 所有换位计算统一由 16ms 定时器 → _updateDragFromPointer() 处理，
            // 避免 PointerMoved 和 DragOver 重复计算。
            SuppressSystemDragVisual(e);
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.Handled = true;
        }

        private void IconListView_Drop(object sender, DragEventArgs e)
        {
            if (!_isCustomDragActive
                || _isCrossGroupDrag
                || _draggedIcons.Count != 1
                || _draggedIcon == null
                || !e.DataView.Properties.ContainsKey("IconInfo"))
            {
                return;
            }

            Point pointer = e.GetPosition(IconListView);
            _dragPointerCache.Store(new LauncherPoint(pointer.X, pointer.Y));

            // 委托标记：folder drop commit 走 business controller
            _dragBusiness.HandleIconListViewDrop(pointer);

            IconInfo? target = ResolveFolderDropTargetAtPoint(pointer);
            if (!ReferenceEquals(target, _folderDropIntent.ActiveTarget))
            {
                return;
            }

            _folderDropCommittedByDropEvent = TryCommitFolderDrop(target);
            if (_folderDropCommittedByDropEvent)
            {
                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                e.Handled = true;
            }
        }

        private void WrapCard_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Properties.ContainsKey("IconInfo")) return;

            SuppressSystemDragVisual(e);
            if (_groupBarDrag.TryUpdateHover(e, "IconInfo"))
            {
                if (_crossGroupPreview.IsActive && _draggedIconsSuppressedFromTiles)
                {
                    TryUpdateBatchPlaceholderBlockFromPointer();
                }

                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                return;
            }

            if (_crossGroupPreview.IsActive && _draggedIconsSuppressedFromTiles && IsDragPointOverShortcutIconArea(e))
            {
                UpdateDragPreviewFromDragEvent(e);
                TryUpdateBatchPlaceholderBlockFromPointer();
                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                return;
            }

            if (_crossGroupPreview.IsActive)
            {
                _groupBarDrag.ClearHighlight();
            }

            UpdateDragPreviewFromDragEvent(e);
            bool folderDropIntent = TryUpdateFolderDropIntentFromPointer();
            if (!folderDropIntent && _crossGroupPreview.IsActive)
            {
                _groupBarDrag.ClearHighlight();
            }
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }

        private void IconListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            LogIconDragState("completed-enter");
            if (!_isCrossGroupDrag)
            {
                TryUpdateFolderDropIntentFromPointer();
            }

            bool batchBlockDrag = IsYieldPlaceholderDragActive();
            bool committedCrossGroupDrop = TryCommitCrossGroupDropAtDragEnd();
            bool committedFolderDrop = _folderDropCommittedByDropEvent
                || (!committedCrossGroupDrop
                    && !_isCrossGroupDrag
                    && TryCommitFolderDrop(ResolveActivatedFolderDropTargetAtDragEnd()));
            LogIconDragState(committedFolderDrop ? "completed-folder-committed" : "completed-folder-missed");
            _groupBarDrag.ClearHighlight();

            if (batchBlockDrag)
            {
                FinalizeBatchPlaceholderBlockPosition("block finalized on drag completed");
            }

            EndDragPointerTracking();
            MainWindow.mainWindow?.SetIconDragActive(false);
            IconInfo? droppedIcon = _draggedIcon;
            ListViewItem? draggedContainer = _draggedContainer;
            _draggedContainer = null;

            Point? dropTarget = null;
            if (droppedIcon != null && !_crossGroupPreview.IsActive)
            {
                dropTarget = GetIconContainerWindowOrigin(droppedIcon);
            }

            bool animate = _isCustomDragActive && !appData.AppConfig.PMModel;

            var draggedIconsSnapshot = _draggedIcons.ToList();
            void CompleteReorder()
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

                    RestoreGroupTabReorderAfterIconDrag();
                    _iconListReorderSuppression.Restore(
                        IconListView,
                        LauncherIconListReorderSuppressionReason.BatchDrag);

                    // G-落位动画：单图标放手时，捕获旧位置 → 集合 Move → 延迟布局 → 滑动动画 → 清理。
                    // 不关闭过渡、不强制 UpdateLayout、不立即压缩，让落位有 130ms 平滑动画。
                    if (!batchBlockDrag && !_singleIconLiveMoveMutatedTiles
                        && droppedIcon != null && !_draggedIconsSuppressedFromTiles
                        && animate && !_isCrossGroupDrag)
                    {
                        int curIdx = shortcutTiles.IndexOf(droppedIcon);
                        if (curIdx >= 0)
                        {
                            int tgtIdx = _singleIconDragPreview.TargetIndex >= 0
                                ? _singleIconDragPreview.TargetIndex
                                : GetShortcutDropIndexFromPointer(droppedIcon);
                            tgtIdx = Math.Clamp(tgtIdx, 0, Math.Max(0, shortcutTiles.Count - 1));

                            if (tgtIdx != curIdx)
                            {
                                // 1. 捕获受影响范围内所有容器的旧位置
                                var oldPositions = new Dictionary<IconInfo, Point>();
                                int rangeMin = Math.Min(curIdx, tgtIdx);
                                int rangeMax = Math.Max(curIdx, tgtIdx);
                                for (int ri = rangeMin; ri <= rangeMax; ri++)
                                {
                                    if (ri >= shortcutTiles.Count) continue;
                                    IconInfo riIcon = shortcutTiles[ri];
                                    if (IconListView.ContainerFromItem(riIcon) is ListViewItem riItem)
                                        oldPositions[riIcon] = GetShortcutContainerPoint(riItem);
                                }

                                // 2. 让被拖拽图标容器可见
                                if (IconListView.ContainerFromItem(droppedIcon) is ListViewItem dropCont)
                                {
                                    dropCont.Opacity = 1;
                                    dropCont.Visibility = Visibility.Visible;
                                    dropCont.IsHitTestVisible = true;
                                    if (dropCont.ContentTemplateRoot is UIElement dropRoot)
                                    {
                                        dropRoot.Opacity = 1;
                                        dropRoot.Visibility = Visibility.Visible;
                                        dropRoot.IsHitTestVisible = true;
                                    }
                                }

                                // 3. 执行集合 Move（不关闭动画、不强制布局）
                                shortcutTiles.Move(curIdx, tgtIdx);

                                // 4. 延迟一帧：布局完成后计算新位置，执行滑动动画
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                                    double riseBoost = Math.Min(
                                        LauncherDragPageContext.ShortcutReorderYieldRiseOffset,
                                        ShortcutItemSlotHeight_NoWrite * 0.14d);

                                    foreach (var kvp in oldPositions)
                                    {
                                        if (IconListView.ContainerFromItem(kvp.Key) is not ListViewItem aItem
                                            || aItem.ContentTemplateRoot is not FrameworkElement aRoot)
                                            continue;
                                        Point newPt = GetShortcutContainerPoint(aItem);
                                        double dx = kvp.Value.X - newPt.X;
                                        double dy = kvp.Value.Y - newPt.Y;
                                        if (Math.Abs(dx) < 0.5d && Math.Abs(dy) < 0.5d) continue;
                                        if (dy > 0d) dy += riseBoost;
                                        BeginShortcutItemSlide(aItem, aRoot, dx, dy,
                                            TimeSpan.FromMilliseconds(130), TimeSpan.Zero, easing);
                                    }

                                    // 5. 动画结束后再延迟一帧：压缩、持久化、清理
                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        ClearDraggedIconsState();
                                        _dragSourceIndex = -1;
                                        CommitCurrentShortcutOrder();
                                        _layoutApply.Compact(shortcutTiles, preserveExistingVisualOrder: false);
                                        _iconSelection.RestoreDragVisuals();
                                        RestoreVisibleShortcutContainerState();
                                        _shortcutTransitions.Restore();
                                        ClearAllGroupsDragOrderSnapshots();
                                        ScheduleDeferredShortcutContainerRestore();
                                        LogIconDragState("completed-clean");
                                    });
                                });

                                return;
                            }
                        }
                    }

                    if (batchReleaseAnimationScheduled)
                    {
                    }
                    else
                    {
                        StopAllLiveReorderAnimations();
                        _shortcutTransitions.SetEnabled(false);

                        foreach (IconInfo icon in draggedIconsSnapshot)
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

                            if (animate && ReferenceEquals(icon, droppedIcon))
                            {
                                LauncherVisualTreeHelper.AnimateContainerFadeIn(container);
                            }
                        }
                    }

                    if (_isCrossGroupDrag)
                    {
                        bool wasAllGroups = showingAllGroups || _crossGroupPreview.SavedSelection.IsAllGroups;
                        _isCrossGroupDrag = false;
                        ClearDraggedIconsState();
                        _dragSourceIndex = -1;
                        _allGroupsOrder.ClearBeforePreview();

                        _crossGroupDropView.Apply(
                            wasAllGroups,
                            _workspaceNavigation.GetActiveGroupIndex(appData),
                            () =>
                            {
                                _iconSelection.RestoreDragVisuals();
                                RestoreVisibleShortcutContainerState();
            _shortcutTransitions.Restore();
                                ClearAllGroupsDragOrderSnapshots();
                            });
                        return;
                    }

                    if (!batchBlockDrag && !_singleIconLiveMoveMutatedTiles
                        && droppedIcon != null && !_draggedIconsSuppressedFromTiles)
                    {
                        int curIdx = shortcutTiles.IndexOf(droppedIcon);
                        if (curIdx >= 0)
                        {
                            int tgtIdx = _singleIconDragPreview.TargetIndex >= 0
                                ? _singleIconDragPreview.TargetIndex
                                : GetShortcutDropIndexFromPointer(droppedIcon);
                            tgtIdx = Math.Clamp(tgtIdx, 0, Math.Max(0, shortcutTiles.Count - 1));
                            if (tgtIdx != curIdx) shortcutTiles.Move(curIdx, tgtIdx);
                        }
                    }

                    ClearDraggedIconsState();
                    _dragSourceIndex = -1;

                    CommitCurrentShortcutOrder();

                    _layoutApply.Compact(shortcutTiles, preserveExistingVisualOrder: false);

                    _iconSelection.RestoreDragVisuals();
                    RefreshIconListLayout();

                    if (!batchReleaseAnimationScheduled)
                    {
                        RestoreVisibleShortcutContainerState();
                        ScheduleDeferredShortcutContainerRestore();
                        _shortcutTransitions.Restore();
                    }

                    ClearAllGroupsDragOrderSnapshots();
                }
                else
                {
                    CompleteIconDragCleanup(restoreContainers: true);
                }

                LogIconDragState("completed-clean");
            }

            if (_isCustomDragActive && _dragPreviewController.HasPreview)
            {
                if (batchBlockDrag || draggedIconsSnapshot.Count <= 1 || _isCrossGroupDrag)
                {
                    _dragPreviewController.End(animate: false);
                    _isCustomDragActive = false;
                    CompleteReorder();
                    return;
                }

                _dragPreviewController.End(
                    animate,
                    dropTarget?.X,
                    dropTarget?.Y,
                    CompleteReorder);
                _isCustomDragActive = false;
                return;
            }

            _isCustomDragActive = false;
            CompleteReorder();
        }

        private void ApplyDraggedItemContainerState(ListViewItem item, IconInfo? icon)
            => _dragBusiness.ApplyDraggedItemContainerState(item, icon);

        private void IconListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                if (args.ItemContainer is ListViewItem recycledItem)
                {
                    recycledItem.ContextFlyout = null;
                    if (!IsYieldPlaceholderDragActive())
                    {
                        StopItemReorderAnimation(recycledItem, "recycleQueue");
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
                }
                return;
            }

            void ApplyContextFlyout()
            {
                if (args.ItemContainer is not ListViewItem item || args.Item is not IconInfo contextIcon)
                {
                    return;
                }

                item.ContextFlyout = IconDialog;
            }

            if (args.Phase == 0)
            {
                ApplyContextFlyout();

                if (args.ItemContainer is ListViewItem phase0Item && args.Item is IconInfo phase0Icon)
                {
                    ApplyDraggedItemContainerState(phase0Item, phase0Icon);
                }

                args.RegisterUpdateCallback(IconListView_ContainerContentChanging);
                args.Handled = true;
                return;
            }

            if (args.Phase == 1
                && args.Item is IconInfo phaseIcon
                && LauncherDragIconClassifier.IsDataShortcutIcon(phaseIcon))
            {
                ImageUtil.EnsureIconBitmap(phaseIcon);
                if (phaseIcon.IsFolder)
                {
                    foreach (IconInfo child in phaseIcon.FolderItems.Take(6))
                    {
                        ImageUtil.EnsureIconBitmap(child);
                    }
                }
                args.RegisterUpdateCallback(IconListView_ContainerContentChanging);
                args.Handled = true;
                return;
            }

            ApplyContextFlyout();

            if (args.ItemContainer is ListViewItem item)
            {
                if (args.Item is IconInfo boundIcon)
                {
                    ApplyDraggedItemContainerState(item, boundIcon);
                    LauncherShortcutItemSpanAdapter.Apply(
                        IconListView,
                        item,
                        boundIcon,
                        ShortcutColumnCount_NoWrite,
                        GetShortcutAvailableWidth());
                    if (boundIcon.IsFolder)
                    {
                        DispatcherQueue.TryEnqueue(() => LauncherFolderTileVisualAdapter.Apply(
                            IconListView,
                            item,
                            boundIcon,
                            ShortcutColumnCount_NoWrite,
                            GetShortcutAvailableWidth()));
                    }
                }

                if (args.ItemIndex >= 0)
                {
                    LauncherShortcutItemSpanAdapter.ApplyMargin(
                        item,
                        args.ItemIndex,
                        shortcutTiles.Count);
                }
            }
        }

        private void CommitCurrentShortcutOrder()
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
            if (showingAllGroups)
            {
                _allGroupsOrder.SetCurrent(orderedIcons);
                command = new UpdateLauncherShortcutOrderCommand(
                    LauncherShortcutOrderScope.AllGroups,
                    ids);
            }
            else
            {
                MenuInfo? group = _workspaceNavigation.GetActiveGroup(appData);
                if (group == null || string.IsNullOrWhiteSpace(group.MenuId))
                {
                    return;
                }
                command = new UpdateLauncherShortcutOrderCommand(
                    LauncherShortcutOrderScope.Group,
                    ids,
                    group.MenuId);
            }

            LauncherCommandResult result = _launcherCommandHandler.UpdateOrder(command);
            if (!result.Success)
            {
                LogUtil.WriteLog(result.Error ?? "Launcher 顺序更新失败。");
            }
        }

        private List<IconInfo> GetOrderedIconsFromTiles()
        {
            return shortcutTiles
                .Where(icon => LauncherDragIconClassifier.IsDataShortcutIcon(icon))
                .ToList();
        }

        private List<IconInfo> BuildDefaultAllGroupsFlatOrder()
        {
            return LauncherAllGroupsProjectionBuilder.BuildDefault(appData);
        }

        private List<IconInfo> BuildAllGroupsFlatOrderTiles()
        {
            IReadOnlyList<IconInfo>? preferredOrder = _allGroupsOrder.HasCurrentOrder
                ? _allGroupsOrder.CopyCurrentOrder()
                : null;
            List<IconInfo> tiles = LauncherAllGroupsProjectionBuilder.Build(
                appData,
                preferredOrder);

            _allGroupsOrder.SetCurrent(tiles);
            return tiles;
        }

        private void CommitAllGroupsProjection()
        {
            if (!showingAllGroups)
            {
                return;
            }

            CommitCurrentShortcutOrder();
        }

        private void ApplyAllGroupsFlatOrderToTiles(IReadOnlyList<IconInfo> flat)
        {
            shortcutTiles.Clear();
            foreach (var icon in flat)
            {
                if (icon == null || LauncherDragIconClassifier.IsAddShortcutPlaceholder(icon)) continue;
                icon.IsAddShortcut_NoWrite = false;
                shortcutTiles.Add(icon);
            }

            _layoutApply.Compact(shortcutTiles, preserveExistingVisualOrder: false);
            _allGroupsOrder.SetCurrent(flat);
        }

        private void UpdateIconReorderEnabled()
        {
            if (IconListView == null) return;

            IconListView.CanDragItems = true;
            IconListView.CanReorderItems = false;
            IconListView.ReorderMode = ListViewReorderMode.Disabled;
        }

        private void GroupListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_groupTabDrag.ConsumeSuppressedClick())
            {
                return;
            }

            _groupEdit.AllowLostFocus();
            TryCommitActiveGroupEdit();

            if (e.ClickedItem is not MenuInfo selectedMenu)
            {
                return;
            }

            if (selectedMenu.IsAllGroupsTab_NoWrite)
            {
                SelectAllGroups();
                return;
            }

            int menuIndex = appData.MenuList.IndexOf(selectedMenu);
            if (menuIndex < 0)
            {
                return;
            }

            SelectGroup(menuIndex);
        }

        #region 跨分组拖拽

        private const double CrossGroupPreviewIconOpacity = 0.82d;

        private IReadOnlyList<IconInfo> GetActiveDraggedIcons()
            => _dragBusiness.GetActiveDraggedIcons();

        private void ApplyCrossGroupPreview(MenuInfo targetMenu)
            => _dragBusiness.ApplyCrossGroupPreview(targetMenu);

        private void RestoreCrossGroupPreviewView()
            => _dragBusiness.RestoreCrossGroupPreviewView();

        private bool TryGetDragPointerOverlayPosition(out Point position)
        {
            position = default;
            var window = MainWindow.mainWindow;
            var overlay = window?.DragOverlayCanvasHost;
            if (window == null || overlay == null) return false;

            if (!NativeWindowMethods.GetCursorPos(out NativeWindowMethods.POINT screenPt)) return false;

            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            var clientPt = screenPt;
            if (!ScreenToClient(hwnd, ref clientPt)) return false;

            double scale = overlay.XamlRoot?.RasterizationScale ?? 1.0;
            position = new Point(clientPt.X / scale, clientPt.Y / scale);
            return true;
        }

        private void ResumeDragPreviewFollowIfNeeded()
            => _dragBusiness.ResumeDragPreviewFollowIfNeeded();

        private void RestoreDraggedIconHiddenState()
            => _dragBusiness.RestoreDraggedIconHiddenState();

        private void EnsureDraggedIconsHiddenDuringActiveDrag()
            => _dragBusiness.EnsureDraggedIconsHiddenDuringActiveDrag();

        private void CommitCrossGroupPreviewState()
            => _dragBusiness.CommitCrossGroupPreviewState();

        private bool ExecuteLauncherCommand(LauncherMutationCommand command)
            => _dragBusiness.ExecuteLauncherCommand(command);

        private bool IsAllGroupsShortcutView()
            => _dragBusiness.IsAllGroupsShortcutView();

        private List<IconInfo> GetValidDropIcons(IReadOnlyList<IconInfo> icons)
            => _dragBusiness.GetValidDropIcons(icons);

        private bool CanDropIconsOnGroupTab(IReadOnlyList<IconInfo> icons, MenuInfo targetMenu)
            => _dragBusiness.CanDropIconsOnGroupTab(icons, targetMenu, FindGroupContainingIcon);

        private void ClearAllGroupsDragOrderSnapshots()
            => _dragBusiness.ClearAllGroupsDragOrderSnapshots();

        private bool IsValidFolderDropTarget(IconInfo? target)
        {
            if (_draggedIcon == null || target == null)
            {
                return false;
            }

            if (ReferenceEquals(target, _draggedIcon))
            {
                return false;
            }

            if (target.IsFolder)
            {
                return LauncherShortcutDropTraitsAdapter.CanMoveIntoFolder(_draggedIcon, target);
            }

            return LauncherShortcutDropTraitsAdapter.CanCreateFolderFrom(_draggedIcon, target);
        }

        private bool TryGetFolderDropPointerInIconListView(out Point point, bool allowCachedPointer = true)
        {
            point = default;
            if (IconListView != null
                && TryGetDragPointerOverlayPosition(out Point overlayPos)
                && MainWindow.mainWindow?.DragOverlayCanvasHost is UIElement overlay
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

        private bool TryResolveItemCollection(IconInfo item, out ObservableCollection<IconInfo> collection)
        {
            if (ShortcutFolderTreeService.TryFindContainer(appData.MenuList, item, out var container))
            {
                collection = container.Collection;
                return true;
            }

            collection = null!;
            return false;
        }

        private bool TryCommitFolderDrop(IconInfo? target)
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
                createFolderInsertIndex = GetShortcutDropIndexFromPointer(_draggedIcon);
                createFolderVisualInsertIndex = createFolderInsertIndex;
                if (showingAllGroups && ShortcutFolderTreeService.TryFindContainer(appData.MenuList, target, out var tc))
                {
                    int local = 0;
                    for (int i = 0; i < Math.Clamp(createFolderInsertIndex, 0, shortcutTiles.Count); i++)
                    {
                        if (LauncherDragIconClassifier.IsDataShortcutIcon(shortcutTiles[i])
                            && !ReferenceEquals(shortcutTiles[i], _draggedIcon)
                            && ShortcutFolderTreeService.TryFindContainer(appData.MenuList, shortcutTiles[i], out var ic)
                            && ReferenceEquals(ic.Collection, tc.Collection))
                            local++;
                    }
                    createFolderInsertIndex = local;
                }
            }

            RestoreSingleIconToDragSourceBeforeFolderCommit();

            if (target.IsFolder)
            {
                bool moved = (showingAllGroups && target.IsFolder
                        && TryExecuteAllGroupsFolderMutation(LauncherAllGroupsFolderMutationKind.MoveInto, _draggedIcon, target))
                    || (TryResolveItemCollection(_draggedIcon, out _)
                        && ExecuteLauncherCommand(new LauncherMutationCommand(
                            LauncherMutationKind.MoveIntoFolder,
                            DraggedShortcutId: _draggedIcon.IconId,
                            TargetShortcutId: target.IconId)));
                if (moved)
                {
                    SetFolderDropIntent(null);
                    RebuildShortcutTiles();
                    CommitAllGroupsProjection();
                }

                return moved;
            }

            if (LauncherShortcutDropTraitsAdapter.CanCreateFolderFrom(_draggedIcon, target))
            {
                bool committed = showingAllGroups
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
                    RebuildShortcutTiles();
                    CommitAllGroupsProjection();
                }

                LogUtil.WriteLog(
                    $"FolderDrop create-folder: committed={committed}, " +
                    $"dragged={_draggedIcon.Name}, target={target.Name}, allGroups={showingAllGroups}");

                return committed;
            }

            return false;
        }

        private IconInfo? ResolveActivatedFolderDropTargetAtDragEnd()
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

        private void GroupBar_IconDragOver(object sender, DragEventArgs e)
        {
            _groupBarDrag.HandleDragOver(e, "IconInfo");
        }

        private void GroupListView_IconDragOver(object sender, DragEventArgs e)
        {
            _groupBarDrag.HandleDragOver(e, "IconInfo");
        }

        private void GroupListView_IconDragLeave(object sender, DragEventArgs e)
        {
            // 不在 DragLeave 中恢复：分组标签间移动时会误触发，改由 DragOver 命中检测决定。
        }

        private void GroupListView_IconDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (_isCrossGroupDrag)
            {
                return;
            }

            if (!e.DataView.Properties.TryGetValue("IconInfo", out object? obj)
                || obj is not IconInfo primaryIcon)
            {
                _groupBarDrag.ClearHighlight();
                return;
            }

            IReadOnlyList<IconInfo> iconsToMove = _draggedIcons.Count > 0
                ? _draggedIcons
                : new[] { primaryIcon };

            LauncherCrossGroupDropTarget<MenuInfo> resolution =
                LauncherCrossGroupDropPlanner.ResolveDropEvent(
                    _groupBarDrag.ResolveTarget(e),
                    _groupBarDrag.HoverTarget,
                    _crossGroupPreview.Target);
            if (!TryCommitCrossGroupDrop(
                    iconsToMove,
                    resolution.Target,
                    "cross-group drop event",
                    out bool wasAllGroups))
            {
                _groupBarDrag.ClearHighlight();
                return;
            }

            _groupBarDrag.ClearIndicators();
            _crossGroupDropView.Apply(
                wasAllGroups,
                _workspaceNavigation.GetActiveGroupIndex(appData));
        }

        private MenuInfo? FindGroupContainingIcon(IconInfo icon)
        {
            return ShortcutFolderTreeService.FindGroupContainingIcon(appData.MenuList, icon);
        }

        #endregion

        private void GroupScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer ?? GroupScrollViewer;
            if (scrollViewer == null) return;

            int delta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
            if (delta == 0) return;

            double targetOffset = scrollViewer.HorizontalOffset - delta;
            if (targetOffset < 0) targetOffset = 0;
            if (targetOffset > scrollViewer.ScrollableWidth) targetOffset = scrollViewer.ScrollableWidth;

            scrollViewer.ChangeView(targetOffset, null, null, true);
            e.Handled = true;
        }

        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CommitGroupEditOnOutsidePointer(e.OriginalSource as DependencyObject);
        }

        private void CommitGroupEditOnOutsidePointer(DependencyObject? source)
            => _groupTabEdit.CommitGroupEditOnOutsidePointer(source, GroupListView, DispatcherQueue);

        private bool TryCommitActiveGroupEdit()
            => _groupTabEdit.TryCommitActiveGroupEdit(GroupListView, DispatcherQueue);

        private void RefreshGroupTabNaturalWidths()
            => _groupTabEdit.RefreshGroupTabNaturalWidths(GroupListView);

        private void CreateGroupClick(object sender, RoutedEventArgs e)
            => _groupTabEdit.HandleCreateGroupClick(sender, e, GroupListView, GroupScrollViewer, GroupTabSelectionIndicator, DispatcherQueue);

        private void EditGroupGeometryClick(object sender, RoutedEventArgs e)
            => _groupTabEdit.HandleEditGroupGeometryClick(sender, e);

        private void RenameGroupClick(object sender, RoutedEventArgs e)
            => _groupTabEdit.HandleRenameGroupClick(sender, e, GroupListView, DispatcherQueue);

        private void BeginGroupInlineEdit(MenuInfo menuInfo)
            => _groupTabEdit.BeginGroupInlineEdit(menuInfo, GroupListView, DispatcherQueue);

        private void FocusInlineGroupEditor(MenuInfo menuInfo)
            => _groupTabEdit.FocusInlineGroupEditor(menuInfo, GroupListView, DispatcherQueue);

        private void AllowGroupEditLostFocus(MenuInfo menuInfo)
            => _groupTabEdit.AllowGroupEditLostFocus(menuInfo);

        private void GroupEditKeyDown(object sender, KeyRoutedEventArgs e)
            => _groupTabEdit.HandleGroupEditKeyDown(sender, e, GroupListView, DispatcherQueue);

        private void GroupEditLostFocusOrEnterDown(object sender, RoutedEventArgs e)
            => _groupTabEdit.HandleGroupEditLostFocus(sender, e, GroupListView, DispatcherQueue);

        private void CommitGroupRename(TextBox box)
            => _groupTabEdit.CommitGroupRename(box, GroupListView, DispatcherQueue);

        private void CancelGroupRename(TextBox box)
            => _groupTabEdit.CancelGroupRename(box, GroupListView, DispatcherQueue);

        private void FinishGroupEdit()
            => _groupTabEdit.FinishGroupEdit(GroupListView, DispatcherQueue);

        private bool IsGroupEditing()
            => _groupTabEdit.IsGroupEditing();

        private void SetGroupReorderEnabled(bool enabled)
            => _groupTabEdit.SetGroupReorderEnabled(enabled, GroupListView);

        private void SuppressGroupTabReorderForIconDrag()
            => _groupTabEdit.SuppressGroupTabReorderForIconDrag();

        private void RestoreGroupTabReorderAfterIconDrag()
            => _groupTabEdit.RestoreGroupTabReorderAfterIconDrag();

        private async void DeleteGroupClick(object sender, RoutedEventArgs e)
            => await _groupTabEdit.HandleDeleteGroupClick(sender, e, GroupListView, DispatcherQueue);

        public PasswordDialog EnsurePasswordDialog()
        {
            if (PDDialog == null)
            {
                FindName(nameof(PDDialog));
            }

            return PDDialog;
        }

        public Grid EnsureWrapUFG()
        {
            if (WrapUFG == null)
            {
                FindName(nameof(WrapUFG));
            }

            return WrapUFG;
        }

        private void SelectAllGroups()
        {
            _groupTabEdit.TryCommitActiveGroupEdit(GroupListView, DispatcherQueue);

            _iconSelection.ClearSelection();
            _workspaceNavigation.SelectAll(appData);
            NotifyShortcutLayoutScopeChanged();
            AttachSelectedIcons(null);
            appData.AppConfig.SelectedMenuIcons = null;
            var passwordDialog = EnsurePasswordDialog();
            if (passwordDialog != null)
            {
                passwordDialog.Visibility = Visibility.Collapsed;
            }
            RefreshGroupSelectionState(-1);
            RebuildShortcutTiles();
            UpdateIconReorderEnabled();
        }

        private void EnsureWritableGroupSelected()
        {
            if (_workspaceNavigation.GetActiveGroup(appData) != null)
            {
                return;
            }

            if (appData.MenuList.Count == 0)
            {
                WorkspaceCommandResult result = _workspaceCommandHandler.Create(
                    new CreateWorkspaceGroupCommand("自定义分组"));
                if (!result.Success)
                {
                    GrowlService.Warning(result.Error ?? "创建默认分组失败，请重试。");
                    return;
                }
            }

            if (appData.MenuList.Count > 0)
            {
                SelectGroup(0);
            }
            else
            {
                SelectAllGroups();
            }
        }

        private void SelectGroup(int index)
        {
            if (index < 0 || index >= appData.MenuList.Count) return;

            _groupTabEdit.TryCommitActiveGroupEdit(GroupListView, DispatcherQueue);

            _iconSelection.ClearSelection();
            if (!_workspaceNavigation.TrySelect(appData, index)) return;
            NotifyShortcutLayoutScopeChanged();
            RefreshGroupSelectionState(index);

            MenuInfo menu = appData.MenuList[index];
            if (menu.IconList == null)
            {
                menu.IconList = new ObservableCollection<IconInfo>();
            }

            var passwordDialog = EnsurePasswordDialog();
            if (menu.IsEncrypt)
            {
                AttachSelectedIcons(null);
                appData.AppConfig.SelectedMenuIcons = null;
                RunTimeStatus.SHOW_MENU_PASSWORDBOX = true;
                if (passwordDialog != null)
                {
                    passwordDialog.Title.Text = "输入密码";
                    passwordDialog.type = PasswordType.INPUT;
                    passwordDialog.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (passwordDialog != null)
                {
                    passwordDialog.Visibility = Visibility.Collapsed;
                }

                appData.AppConfig.SelectedMenuIcons = menu.IconList;
                AttachSelectedIcons(menu.IconList);
                RebuildShortcutTiles();
            }

            UpdateIconReorderEnabled();
        }

        private void NotifyShortcutLayoutScopeChanged()
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(UseManualShortcutGridPositions_NoWrite)));
            LauncherShortcutPanelMetricsAdapter.Invalidate(IconListView?.ItemsPanelRoot);
        }

        private void RefreshGroupSelectionState(int selectedIndex)
            => _groupTabEdit.RefreshGroupSelectionState(selectedIndex, GroupListView, GroupScrollViewer, GroupTabSelectionIndicator, DispatcherQueue);

        private void ScheduleGroupTabSelectionIndicatorUpdate(int selectedIndex, bool animate)
            => _groupTabEdit.ScheduleSelectionIndicatorUpdate(selectedIndex, animate, GroupListView, GroupScrollViewer, GroupTabSelectionIndicator, DispatcherQueue);

    }
}
