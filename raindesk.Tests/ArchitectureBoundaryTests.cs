namespace raindesk.Tests;

[TestClass]
public class ArchitectureBoundaryTests
{
    [TestMethod]
    public void NonUiLayers_DoNotDependOnMainWindowAppData()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectRoot = Path.Combine(repositoryRoot, "raindesk");
        string[] protectedDirectories =
        {
            "Services",
            "Task",
            "MyThread",
            "Util",
            "Converts",
            "Controls",
            "Features"
        };

        var violations = protectedDirectories
            .Select(directory => Path.Combine(projectRoot, directory))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains("MainWindow.appData", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "非 UI 层重新依赖了窗口静态状态:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void Persistence_IsNotTriggeredByLegacyStaticModelBridge()
    {
        Type? saveTrigger = typeof(raindesk.ViewModel.AppData).Assembly
            .GetType("raindesk.Services.SaveTrigger", throwOnError: false);

        Assert.IsNull(saveTrigger, "SaveTrigger 不应重新进入模型层；请在命令服务或 AppDataChangeTracker 中保存。");
    }

    [TestMethod]
    public void RightCard_UsesStateAndFeaturePortsInsteadOfWindowStateOrLegacyDropCommit()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        Assert.IsFalse(
            source.Contains("MainWindow.appData", StringComparison.Ordinal),
            "RightCardControl 不应重新读取窗口静态 AppData。");
        Assert.IsFalse(
            source.Contains("LauncherDropCommitService.Commit", StringComparison.Ordinal),
            "RightCardControl 应通过 ILauncherCommandHandler 提交 Launcher 业务变更。");
        Assert.IsTrue(
            source.Contains("IWorkspaceCommandHandler", StringComparison.Ordinal)
            && source.Contains("ILauncherCommandHandler", StringComparison.Ordinal),
            "RightCardControl 应依赖 Workspace / Launcher 模块契约。");
    }

    [TestMethod]
    public void RightCard_WorkspaceSelectionUsesStableNavigationState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string[] selectedIndexUsages = source
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(line => line.Contains("appData.AppConfig.SelectedMenuIndex", StringComparison.Ordinal))
            .ToArray();

        Assert.IsTrue(
            source.Contains("WorkspaceNavigationPresenter", StringComparison.Ordinal)
            && source.Contains("_workspaceNavigation.GetActiveGroup", StringComparison.Ordinal),
            "RightCard 应通过 WorkspaceNavigationPresenter 消费稳定 GroupId 选择。");
        Assert.AreEqual(
            0,
            selectedIndexUsages.Length,
            "SelectedMenuIndex 的兼容投影必须留在 Workspace Presentation，RightCard 不应直接读写。");
    }

    [TestMethod]
    public void WorkspaceNavigationState_RemainsUiAndPersistenceAgnostic()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceNavigationState.cs"));
        string[] forbiddenDependencies =
        {
            "Microsoft.UI",
            "raindesk.ViewModel",
            "raindesk.Services",
            "AppData",
            "MenuInfo"
        };

        var violations = forbiddenDependencies
            .Where(source.Contains)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Workspace 导航状态必须保持为纯稳定 ID 状态:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void RightCard_DelegatesWorkspaceGroupTabDragSessionAndPreview()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // Delegation patterns now live in GroupTabEditController
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "GroupTabEditController.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupTabDragSessionState.cs"));
        string previewSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabDragPreviewController.cs"));
        string pointerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabPointerTrackingController.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabDragCoordinator.cs"));
        string insertPlannerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupTabInsertPlanner.cs"));
        string layoutSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabLayoutAdapter.cs"));
        string visualSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabDragVisualController.cs"));
        string indicatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabSelectionIndicatorController.cs"));
        string deleteAnimationSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabDeleteAnimationController.cs"));
        string widthSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupTabWidthCalculator.cs"));
        string scrollSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupTabScrollPlanner.cs"));
        string sizingSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabSizingAdapter.cs"));

        // These patterns should exist in the page (container and pointer handler wiring)
        string[] requiredPageDelegations =
        {
            "WorkspaceGroupTabDragCoordinator<MenuInfo>",
            "_groupTabDrag.PointerPressedHandler",
            "_groupTabDrag.PointerMovedHandler",
            "_groupTabDrag.PointerReleasedHandler",
            "_groupTabDrag.PointerCanceledHandler",
            "_groupTabDrag.ApplyPlaceholderChrome",
            "_groupTabDrag.ConsumeSuppressedClick",
            "WorkspaceGroupTabLayoutAdapter.TryGetContainer",
        };
        var missingPageDelegations = requiredPageDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingPageDelegations.Length,
            "RightCard 缺少 Workspace 分组标签容器/指针委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingPageDelegations));

        // These patterns should exist in the controller (reorder, delete animation, selection)
        string[] requiredControllerDelegations =
        {
            "_groupTabDrag.SetReorderEnabled",
            "_groupTabDrag.SuspendReorder",
            "_groupTabDrag.RestoreReorder",
            "_groupTabDrag.EnableDeleteTransitions",
            "_groupTabDrag.RestoreTransitions",
            "_selectionIndicator.ScheduleUpdate",
            "_deleteAnimation.AnimateOutAsync",
            "_deleteAnimation.ScheduleTransitionRestore",
            "WorkspaceGroupTabSizingAdapter.ApplyEditWidth",
            "WorkspaceGroupTabSizingAdapter.ApplyNaturalWidth",
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "GroupTabEditController 缺少 Workspace 分组标签拖拽/动画/选择委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] requiredCoordinatorOrchestration =
        {
            "WorkspaceGroupTabDragSessionState<TMenu>",
            "WorkspaceGroupTabDragPreviewController",
            "WorkspaceGroupTabPointerTrackingController",
            "WorkspaceGroupTabDragVisualController",
            "_state.Begin",
            "_state.TryActivate",
            "_state.TrySetPendingTarget",
            "_state.CommitLiveMove",
            "_state.Complete",
            "_preview.BeginAsync",
            "_preview.End",
            "_pointerTracking.Begin",
            "_pointerTracking.End",
            "WorkspaceGroupTabInsertPlanner.Resolve",
            "WorkspaceGroupTabLayoutAdapter.BuildSlots",
            "_visual.ApplyPlaceholderChrome",
            "_visual.EnableRepositionTransitions",
            "_visual.RestoreTransitions",
            "RestoreVerticalScroll",
            "RestoreDragContainer"
        };
        var missingCoordinatorOrchestration = requiredCoordinatorOrchestration
            .Where(value => !coordinatorSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingCoordinatorOrchestration.Length,
            "Workspace 分组标签拖拽协调器缺少会话、预览、指针或收尾编排:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingCoordinatorOrchestration));

        string[] requiredSelectionOrchestration =
        {
            "WorkspaceGroupSelectionProjection.ResolveTab",
            "WorkspaceGroupSelectionProjection.ToTabIndex",
            "WorkspaceGroupTabLayoutAdapter.TryGetBounds",
            "WorkspaceGroupTabScrollPlanner.Plan",
            "WorkspaceGroupTabSizingAdapter.ApplyEditWidth"
        };
        var missingSelectionOrchestration = requiredSelectionOrchestration
            .Where(value => !indicatorSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingSelectionOrchestration.Length,
            "Workspace 选择指示器控制器缺少标签解析、布局或滚动编排:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingSelectionOrchestration));

        string[] forbiddenRawState =
        {
            "_groupTabManualReorderEnabled",
            "_manualGroupTabDragActive",
            "_manualGroupTabDragMoved",
            "_groupTabDragPreviewStarted",
            "_suppressNextGroupTabClick",
            "_manualGroupTabDragMenu",
            "_manualGroupTabDragSourceIndex",
            "_manualGroupTabDragOriginalIndex",
            "_manualGroupTabPendingTargetIndex",
            "_manualGroupTabDragStart",
            "_savedGroupTabManualReorderEnabled",
            "_groupTabWindowPointerMovedHandler",
            "_groupTabWindowPointerReleasedHandler",
            "_groupTabDragPlaceholderWidth",
            "_groupTabSavedItemTransitions",
            "_manualGroupTabDragContainer",
            "_savedGroupScrollHorizontalMode",
            "GroupTabItem_PointerPressed",
            "GroupTabItem_PointerMoved",
            "CompleteManualGroupTabDrag",
            "CanStartManualGroupTabDrag",
            "BeginGroupTabWindowPointerTracking",
            "SuppressGroupScrollDuringTabDrag",
            "ResolveGroupTabInsertIndexFromPoint",
            "HideManualGroupTabDragContainer",
            "RestoreManualGroupTabDragContainer",
            "MoveGroupTabItem",
            "ApplyLiveGroupTabReorder",
            "BeginGroupTabDragPreviewAsync",
            "UpdateGroupTabDragPreviewPosition",
            "EndGroupTabDragPreview",
            "ResolveGroupTabInsertIndexFromPointCore",
            "ResolveGroupTabInsertIndexDuringDrag",
            "FindAdjacentGroupTabIndex",
            "TryGetGroupTabContainerBounds",
            "ApplyGroupTabDragPlaceholderChrome",
            "RestoreGroupTabContainerAfterInteraction",
            "_groupTabSelectionInitialized",
            "_groupTabSelectionStoryboard",
            "_groupTabIndicatorX",
            "_groupTabIndicatorWidth",
            "_pendingGroupTabSelectionIndex",
            "_pendingGroupTabSelectionAnimate",
            "_groupTabSelectionUpdateQueued",
            "ScheduleGroupTabSelectionIndicatorUpdateCore",
            "GetGroupTabSelectionIndicatorX",
            "ApplyGroupTabSelectionIndicatorLayout",
            "AnimateGroupTabSelectionIndicator",
            "ApplyGroupTabSelectionIndicatorUpdate",
            "TryGetGroupTabSelectionBounds",
            "ScrollGroupTabIntoView",
            "UpdateGroupTabSelectionIndicator",
            "GetGroupTabIndicatorHost",
            "GetGroupTabMenuForSelection",
            "EnableGroupTabDeleteTransitions",
            "ScheduleGroupTabDeleteTransitionRestore",
            "AnimateGroupTabDeleteOutAsync",
            "ApplyGroupTabEditWidth",
            "ApplyGroupTabNaturalWidth",
            "FindFirstTextBlock",
            "TryGetGroupTabContainer(",
            "GroupTabHorizontalChrome",
            "GroupTabIconSlotWidth",
            "GroupTabEditContentWidth",
            "GroupTabColumnSpacing",
            "new DragPreviewHost"
        };
        var rawStateViolations = forbiddenRawState
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawStateViolations.Length,
            "分组标签拖拽状态和预览宿主不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawStateViolations));

        string[] forbiddenStateDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "MenuInfo" };
        var stateViolations = forbiddenStateDependencies
            .Where(value => stateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            stateViolations.Length,
            "Workspace 分组标签拖拽会话必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, stateViolations));

        string combinedPlannerSource = string.Join(
            Environment.NewLine,
            insertPlannerSource,
            widthSource,
            scrollSource);
        var plannerViolations = forbiddenStateDependencies
            .Where(value => combinedPlannerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            plannerViolations.Length,
            "Workspace 分组标签插入规划必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, plannerViolations));

        Assert.IsTrue(
            previewSource.Contains("generation != _generation", StringComparison.Ordinal)
            && previewSource.Contains("!ReferenceEquals(_host, host)", StringComparison.Ordinal),
            "Workspace 分组标签预览必须拒绝拖拽结束后的晚到截图。");
        string[] forbiddenPreviewDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var previewViolations = forbiddenPreviewDependencies
            .Where(value => string.Join(
                Environment.NewLine,
                previewSource,
                pointerSource,
                coordinatorSource,
                layoutSource,
                visualSource,
                indicatorSource,
                deleteAnimationSource,
                sizingSource).Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            previewViolations.Length,
            "Workspace 分组标签预览不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, previewViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesWorkspaceGroupEditSessionFocusAndVisuals()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // Delegation patterns now live in GroupTabEditController
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "GroupTabEditController.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupEditSessionState.cs"));
        string visualSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupEditVisualController.cs"));
        string focusSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupEditFocusScheduler.cs"));

        string[] requiredDelegations =
        {
            "WorkspaceGroupEditSessionState<MenuInfo>",
            "_groupEdit.Begin",
            "_groupEdit.ShouldCommitOutside",
            "_groupEdit.ShouldCommitLostFocus",
            "_groupEdit.TryEstablishFocus",
            "_groupEdit.Complete",
            "WorkspaceGroupEditVisualController",
            "_groupEditVisual.IsInsideEditor",
            "_groupEditVisual.TryGetEditor",
            "_groupEditVisual.PrepareEditor",
            "_groupEditVisual.DetachEditor",
            "WorkspaceGroupEditFocusScheduler",
            "_groupEditFocus.Schedule"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "GroupTabEditController 缺少 Workspace 分组编辑状态/焦点/视觉委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string[] forbiddenRawState =
        {
            "groupEditTarget",
            "groupEditOriginalName",
            "_groupEditSuppressLostFocus",
            "_groupEditFocusEstablished",
            "IsInsideActiveGroupEditBox",
            "TryFocusInlineGroupEditor(MenuInfo menuInfo, int attempt)",
            "FindFirstChild<"
        };
        var rawStateViolations = forbiddenRawState
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawStateViolations.Length,
            "分组编辑状态、焦点重试和视觉树查找不应重新进入 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawStateViolations));

        string[] forbiddenStateDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "MenuInfo" };
        var stateViolations = forbiddenStateDependencies
            .Where(value => stateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            stateViolations.Length,
            "Workspace 分组编辑会话必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, stateViolations));

        string combinedVisualSource = visualSource + Environment.NewLine + focusSource;
        string[] forbiddenVisualDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "IWorkspaceCommandHandler" };
        var visualViolations = forbiddenVisualDependencies
            .Where(value => combinedVisualSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            visualViolations.Length,
            "Workspace 分组编辑视觉/焦点控制器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, visualViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesWorkspaceGroupProjectionSubscriptionAndChrome()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // Selection projection, chrome, and tab width delegation now live in GroupTabEditController
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "GroupTabEditController.cs"));
        string selectionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupSelectionProjection.cs"));
        string subscriptionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupCollectionSubscription.cs"));
        string chromeSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabChromeController.cs"));
        string selectionIndicatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabSelectionIndicatorController.cs"));
        string containerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WinUI",
            "WorkspaceGroupTabContainerController.cs"));

        // These patterns should exist in the page (subscription and container lifecycle)
        string[] requiredPageDelegations =
        {
            "WorkspaceGroupCollectionSubscription",
            "_groupCollectionSubscription.Attach",
            "_groupCollectionSubscription.Detach",
            "WorkspaceGroupTabContainerController",
            "_groupTabContainers.Handle",
            "_groupTabContainers.RefreshIcon"
        };
        var missingPageDelegations = requiredPageDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingPageDelegations.Length,
            "RightCard 缺少 Workspace 集合订阅/容器委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingPageDelegations));

        // These patterns should exist in the controller (selection projection and chrome)
        string[] requiredControllerDelegations =
        {
            "_workspaceGroupTabs.SuppressSourceSynchronization",
            "WorkspaceGroupSelectionProjection.Apply",
            "WorkspaceGroupTabChromeController",
            "_groupTabChrome.ApplyAll",
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "GroupTabEditController 缺少 Workspace 选择投影/Chrome 委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        Assert.IsTrue(
            selectionIndicatorSource.Contains("WorkspaceGroupSelectionProjection.ResolveTab", StringComparison.Ordinal)
            && selectionIndicatorSource.Contains("WorkspaceGroupSelectionProjection.ToTabIndex", StringComparison.Ordinal),
            "Workspace 选择标签解析应由选择指示器控制器完成。" );

        string[] forbiddenRawImplementation =
        {
            "_suppressGroupTabItemsSync",
            "GroupTabTransparentBrush",
            "ApplyGroupTabItemChrome",
            "ApplyAllGroupTabItemChrome",
            "GetGroupTabIndexForSelection",
            "AttachGroupTabPointerHandlers",
            "DetachGroupTabPointerHandlers",
            "RegisterUpdateCallback(GroupListView_ContainerContentChanging)",
            "GroupTabIconPath",
            "MenuList.CollectionChanged += MenuList_CollectionChanged",
            "MenuList.CollectionChanged -= MenuList_CollectionChanged"
        };
        var rawViolations = forbiddenRawImplementation
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawViolations.Length,
            "Workspace 集合订阅、选择投影和标签 Chrome 不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawViolations));

        string combinedPureSource = selectionSource + Environment.NewLine + subscriptionSource;
        string[] forbiddenPureDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "MenuInfo" };
        var pureViolations = forbiddenPureDependencies
            .Where(value => combinedPureSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            pureViolations.Length,
            "Workspace 选择投影和集合订阅必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, pureViolations));

        string[] forbiddenChromeDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "IWorkspaceCommandHandler" };
        string combinedWinUiSource = chromeSource + Environment.NewLine + containerSource;
        var chromeViolations = forbiddenChromeDependencies
            .Where(value => combinedWinUiSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            chromeViolations.Length,
            "Workspace 标签 Chrome 控制器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, chromeViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesWorkspaceGroupIconCollectionSubscriptionLifetime()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string subscriptionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Workspace",
            "Presentation",
            "WorkspaceGroupIconCollectionSubscription.cs"));

        string[] requiredDelegations =
        {
            "WorkspaceGroupIconCollectionSubscription",
            "_groupIconSubscriptions.IconListReplaced",
            "_groupIconSubscriptions.IconsChanged",
            "_groupIconSubscriptions.Synchronize",
            "_groupIconSubscriptions.DetachAll"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少 Workspace 分组图标集合订阅委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string[] forbiddenRawImplementation =
        {
            "_trackedGroupIconLists",
            "SynchronizeGroupIconListSubscriptions",
            "AttachGroupIconListSubscription",
            "DetachGroupIconListSubscription",
            "DetachAllGroupIconListSubscriptions",
            "TrackedMenu_PropertyChanged",
            "GroupIcons_CollectionChanged"
        };
        var rawViolations = forbiddenRawImplementation
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawViolations.Length,
            "分组 IconList 替换和集合订阅生命周期不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawViolations));

        Assert.IsTrue(
            subscriptionSource.Contains("previous.CollectionChanged -= OnIconsChanged", StringComparison.Ordinal)
            && subscriptionSource.Contains("group.PropertyChanged -= OnGroupPropertyChanged", StringComparison.Ordinal)
            && subscriptionSource.Contains("public void DetachAll()", StringComparison.Ordinal),
            "Workspace 分组图标集合订阅必须释放旧集合、分组属性和全部活动订阅。" );
        string[] forbiddenDependencies =
            { "Microsoft.UI", "MainWindow", "AppData", "CommonCode", "raindesk.Services" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => subscriptionSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "Workspace 分组图标集合订阅不应依赖窗口、应用状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesVisibleShortcutReconciliationToLauncherState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string listStateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherShortcutListState.cs"));

        Assert.IsTrue(
            rightCardSource.Contains("LauncherShortcutListState", StringComparison.Ordinal)
            && rightCardSource.Contains("_shortcutList.ApplySnapshot", StringComparison.Ordinal),
            "RightCard 应把可见快捷方式快照交给 Launcher Presentation 状态同步。");
        // ApplyShortcutTileSnapshot 在 LauncherDragPageContext 初始化中作为回调传递是可接受的
        // 实际快照逻辑由 _shortcutList.ApplySnapshot 在 Presentation 层处理

        string[] forbiddenDependencies = { "Microsoft.UI", "MainWindow", "CommonCode", "AppData" };
        var violations = forbiddenDependencies
            .Where(dependency => listStateSource.Contains(dependency, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            violations.Length,
            "Launcher 列表状态不应依赖窗口或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void LauncherFolderNavigationState_LivesInFeatureAndRemainsUiAgnostic()
    {
        string repositoryRoot = FindRepositoryRoot();
        string oldPath = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "FolderOverlayState.cs");
        string newPath = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherFolderNavigationState.cs");
        string source = File.ReadAllText(newPath);

        Assert.IsFalse(File.Exists(oldPath), "文件夹导航状态不应继续留在 UI 控件目录。");
        Assert.IsTrue(File.Exists(newPath), "文件夹导航状态应归属 Launcher Presentation。");

        string[] forbiddenDependencies =
            { "Microsoft.UI", "MainWindow", "AppData", "raindesk.Services" };
        var violations = forbiddenDependencies
            .Where(dependency => source.Contains(dependency, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            violations.Length,
            "Launcher 文件夹导航状态不应依赖窗口或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void FolderOverlay_UsesLauncherViewModelBindingsInsteadOfManualControlProjection()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard");
        string codeBehind = File.ReadAllText(Path.Combine(rightCardRoot, "RightCardControl.xaml.cs"));
        string xaml = File.ReadAllText(Path.Combine(rightCardRoot, "RightCardControl.xaml"));
        string viewModel = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherFolderOverlayViewModel.cs"));

        string[] requiredBindings =
        {
            "FolderOverlayViewModel.IsOpen",
            "FolderOverlayViewModel.CanGoBack",
            "FolderOverlayViewModel.Title",
            "FolderOverlayViewModel.Items"
        };
        var missingBindings = requiredBindings
            .Where(binding => !xaml.Contains(binding, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingBindings.Length,
            "文件夹浮层缺少 ViewModel 绑定:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingBindings));

        string[] forbiddenManualProjection =
        {
            "FolderOverlay.Visibility =",
            "FolderBackButton.Visibility =",
            "FolderOverlayTitle.Text =",
            "FolderItemsListView.ItemsSource ="
        };
        var projectionViolations = forbiddenManualProjection
            .Where(code => codeBehind.Contains(code, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            projectionViolations.Length,
            "RightCard 不应手工同步文件夹浮层控件:" + Environment.NewLine
                + string.Join(Environment.NewLine, projectionViolations));

        string[] forbiddenViewModelDependencies =
            { "Microsoft.UI", "MainWindow", "AppData", "raindesk.Services" };
        var dependencyViolations = forbiddenViewModelDependencies
            .Where(dependency => viewModel.Contains(dependency, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "Launcher 文件夹浮层 ViewModel 不应依赖窗口或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesFolderDropRulesToLauncherApplication()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        Assert.IsTrue(
            source.Contains("LauncherFolderDropCommandResolver.Resolve", StringComparison.Ordinal),
            "文件夹浮层 Drop 应由 Launcher Application 解析为命令。");
        Assert.IsFalse(
            source.Contains("ShortcutFolderService.CanMoveIntoFolder", StringComparison.Ordinal)
            || source.Contains("ShortcutFolderService.CanCreateFolderFrom", StringComparison.Ordinal),
            "RightCard 不应直接调用旧 Service 判断文件夹拖放资格。");
    }

    [TestMethod]
    public void RightCard_DelegatesBatchDragSessionStateToLauncherPresentation()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string sessionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherBatchDragSessionState.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherBatchPlaceholderCoordinator.cs"));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));

        // RightCard must delegate to high-level _dragBusiness methods
        string[] requiredPageDelegations =
        {
            "LauncherBatchPlaceholderCoordinator",
            "_dragBusiness.TryUpdateBatchPlaceholderBlockFromPointer",
            "_dragBusiness.CompleteDragReorder"
        };
        var missingPageDelegations = requiredPageDelegations
            .Where(delegation => !rightCardSource.Contains(delegation, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingPageDelegations.Length,
            "RightCard 缺少批量拖拽高层委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingPageDelegations));

        // Business controller must contain the low-level batch placeholder orchestration
        string[] requiredControllerDelegations =
        {
            "_batchDragPlaceholders.TryBegin",
            "_batchDragPlaceholders.PlanRestore",
            "_batchDragPlaceholders.ApplyRestore",
            "_batchDragPlaceholders.BuildTargetPreview",
            "MoveBatchDragPlaceholder",
            "ClearDraggedIconsState"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(delegation => !controllerSource.Contains(delegation, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少批量占位编排:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] forbiddenInlineOrchestration =
        {
            "private void ReplaceDraggedIconsWithYieldPlaceholders",
            "private void RemoveMergedBatchDragPlaceholder",
            "private static IconInfo CreateDragYieldPlaceholder"
        };
        var orchestrationViolations = forbiddenInlineOrchestration
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            orchestrationViolations.Length,
            "占位块集合编排不应重新进入 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, orchestrationViolations));

        string[] forbiddenDependencies =
            { "Microsoft.UI", "MainWindow", "AppData", "CommonCode", "raindesk.Services" };
        var dependencyViolations = forbiddenDependencies
            .Where(dependency => sessionSource.Contains(dependency, StringComparison.Ordinal)
                || coordinatorSource.Contains(dependency, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "批量拖拽会话状态不应依赖 UI、窗口或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesItemAnimationLifecycleToLauncherWinUiAdapter()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherItemAnimationCoordinator.cs"));
        string plannerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherBatchDropAnimationPlanner.cs"));

        string[] requiredDelegations =
        {
            "LauncherItemAnimationCoordinator",
            "_dragBusiness.BeginShortcutItemPreviewOffset",
            "_dragBusiness.StopAllLiveReorderAnimations"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少 Launcher 动画适配器委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        // Business controller must contain the low-level animation lifecycle methods
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "BeginShortcutItemSlide",
            "BeginShortcutItemFlyPlacement",
            "LauncherBatchDropAnimationPlanner.GetCleanupDelay",
            "LauncherBatchDropAnimationPlanner.GetReleaseOffset"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少动画生命周期委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        Assert.IsFalse(
            rightCardSource.Contains("_liveReorderStoryboards", StringComparison.Ordinal)
            || rightCardSource.Contains("_liveReorderTargetOffsets", StringComparison.Ordinal),
            "Storyboard 注册表和目标偏移状态不应重新进入 RightCard。");

        string[] forbiddenDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => coordinatorSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "WinUI 动画适配器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));

        string[] forbiddenPlannerDependencies =
            { "using Microsoft.UI", "using Windows.Foundation", "new Storyboard", "AppData", "raindesk.Services" };
        var plannerViolations = forbiddenPlannerDependencies
            .Where(value => plannerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            plannerViolations.Length,
            "批量落位动画时间线必须保持为纯规划器:" + Environment.NewLine
                + string.Join(Environment.NewLine, plannerViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesLauncherHitTestingToWinUiAdapter()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string hitTestSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDropHitTestAdapter.cs"));
        string gridItemSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherShortcutGridItemAdapter.cs"));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));

        // RightCard must delegate hit-testing to the adapter (directly or via business controller)
        string[] requiredPageDelegations =
        {
            "LauncherDropHitTestAdapter.TryTranslatePoint",
            "LauncherDropHitTestAdapter.FindCenteredTarget",
            "LauncherDropHitTestAdapter.IsPointInside",
            "LauncherDropHitTestAdapter.ResolveRealizedListDropIndex",
            "LauncherShortcutGridItemAdapter.Create"
        };
        var missingPageDelegations = requiredPageDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingPageDelegations.Length,
            "RightCard 缺少 Launcher 命中适配器委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingPageDelegations));

        // Business controller must contain the menu-specific hit-test resolution
        string[] requiredControllerDelegations =
        {
            "LauncherDropHitTestAdapter.FindRealizedItem<MenuInfo>"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少分组命中测试委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] forbiddenInlineHitTesting =
        {
            "private static Rect GetFolderDropHotZone",
            "private List<ShortcutSlotHitTarget> BuildWrapGridSlotHitTargets",
            "private static LauncherGridItem BuildLauncherGridItem"
        };
        var inlineViolations = forbiddenInlineHitTesting
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            inlineViolations.Length,
            "命中坐标和网格映射不应重新进入 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, inlineViolations));

        string[] forbiddenAdapterDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var adapterViolations = forbiddenAdapterDependencies
            .Where(value => hitTestSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            adapterViolations.Length,
            "WinUI 命中适配器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, adapterViolations));

        string[] forbiddenGridAdapterDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services" };
        var gridAdapterViolations = forbiddenGridAdapterDependencies
            .Where(value => gridItemSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            gridAdapterViolations.Length,
            "IconInfo 到 LauncherGridItem 的映射必须保持纯净:" + Environment.NewLine
                + string.Join(Environment.NewLine, gridAdapterViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesSingleIconTargetDebounceToLauncherState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherSingleIconDragPreviewState.cs"));

        string[] requiredDelegations =
        {
            "LauncherSingleIconDragPreviewState",
            "_singleIconDragPreview.CommitTarget",
            "_dragBusiness.TryStabilizeSingleIconReorderTarget"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少单图标拖放状态委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "_singleIconDragPreview.TryActivateTarget",
            "_singleIconDragPreview.Begin",
            "_singleIconDragPreview.Reset"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少单图标拖放内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        Assert.IsFalse(
            rightCardSource.Contains("_pendingSingleIconReorderTarget", StringComparison.Ordinal)
            || rightCardSource.Contains("_pendingSingleIconReorderTargetSinceTick", StringComparison.Ordinal)
            || rightCardSource.Contains("_liveSingleDragTargetIndex", StringComparison.Ordinal),
            "单图标目标与去抖状态不应重新散落到 RightCard 字段。");

        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => stateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "单图标拖放预览状态必须保持为纯索引/时间状态:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesFolderIntentAndPointerCacheToLauncherState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string intentSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherFolderDropIntentState.cs"));
        string pointerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherPointerCacheState.cs"));
        string visualSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDropTargetVisualAdapter.cs"));

        string[] requiredDelegations =
        {
            "LauncherFolderDropIntentState<IconInfo>",
            "_dragBusiness.SetFolderDropIntent",
            "_dragBusiness.CompleteDragReorder",
            "_dragBusiness.UpdateFolderDropIntentActivation",
            "LauncherPointerCacheState",
            "_dragPointerCache.Store",
            "_dragPointerCache.TryGet"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少文件夹意图/指针缓存状态委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "_folderDropIntent.EvaluateCandidate",
            "ClearFolderDropTargetHighlight",
            "ApplyFolderDropTargetVisualState",
            "ClearDraggedIconsState"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少文件夹意图内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] forbiddenRawState =
        {
            "_folderDropTargetDuringDrag",
            "_lastValidFolderDropTarget",
            "_pendingFolderDropTarget",
            "_pendingFolderDropTargetSinceTick",
            "_hasCachedDragPointerInIconListView",
            "_cachedDragPointerInIconListView"
        };
        var rawStateViolations = forbiddenRawState
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawStateViolations.Length,
            "文件夹意图和指针缓存不应重新散落到 RightCard 字段:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawStateViolations));

        Assert.IsFalse(
            rightCardSource.Contains("private void AnimateDropTargetScale", StringComparison.Ordinal),
            "文件夹目标的模板查找和 Storyboard 不应重新进入 RightCard。");

        string combinedStateSource = intentSource + Environment.NewLine + pointerSource;
        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => combinedStateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "文件夹意图和指针缓存状态必须保持纯净:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));

        string[] forbiddenVisualDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var visualViolations = forbiddenVisualDependencies
            .Where(value => visualSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            visualViolations.Length,
            "文件夹目标视觉适配器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, visualViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesDragPreviewOwnershipToLauncherController()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragPreviewController.cs"));

        string[] requiredDelegations =
        {
            "LauncherDragPreviewController",
            "_dragPreviewController.BeginAsync",
            "_dragPreviewController.End",
            "_dragPreviewController.HasPreview"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少拖拽预览生命周期委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        Assert.IsTrue(
            controllerSource.Contains("IsPlacementPreviewActive", StringComparison.Ordinal)
            && controllerSource.Contains("AnimateToPlacement", StringComparison.Ordinal),
            "拖拽预览控制器必须提供 IsPlacementPreviewActive 和 AnimateToPlacement 能力。");
        Assert.IsFalse(
            rightCardSource.Contains("private DragPreviewHost? _dragPreview;", StringComparison.Ordinal)
            || rightCardSource.Contains("_dragPreview = null", StringComparison.Ordinal),
            "Launcher 图标预览宿主的可空所有权不应重新进入 RightCard。");
        Assert.IsTrue(
            controllerSource.Contains("generation != _generation", StringComparison.Ordinal)
            && controllerSource.Contains("!ReferenceEquals(_host, host)", StringComparison.Ordinal),
            "异步截图完成后必须验证 generation，避免拖拽结束后出现晚到预览。");

        string[] forbiddenDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "拖拽预览控制器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesCrossGroupPreviewSessionToLauncherState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupPreviewState.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupPreviewCoordinator.cs"));
        string overlaySource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherCrossGroupPreviewOverlayController.cs"));
        string tilePlannerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupPreviewTilePlanner.cs"));
        string suppressionStateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherReorderSuppressionState.cs"));
        string reorderControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherIconListReorderSuppressionController.cs"));
        string anchorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupPreviewAnchorSelector.cs"));
        string placementSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherCrossGroupPreviewPlacementController.cs"));
        string dropPlannerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupDropPlanner.cs"));
        string dropViewSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherCrossGroupDropViewCoordinator.cs"));

        string[] requiredDelegations =
        {
            "LauncherCrossGroupPreviewCoordinator<MenuInfo, WorkspaceNavigationSnapshot, IconInfo>",
            "_dragBusiness.ApplyCrossGroupPreview",
            "_dragBusiness.RestoreCrossGroupPreviewView",
            "LauncherCrossGroupPreviewOverlayController",
            "LauncherIconListReorderSuppressionController",
            "_iconListReorderSuppression.Clear",
            "LauncherCrossGroupPreviewPlacementController<IconInfo, MenuInfo>",
            "LauncherCrossGroupDropPlanner.ResolveDropEvent",
            "LauncherCrossGroupDropViewCoordinator<IconInfo>",
            "_crossGroupDropView.Apply"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少跨分组预览会话状态委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string dragBusinessControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "_crossGroupPreview.Retarget",
            "_crossGroupPreview.IsCurrent",
            "_crossGroupPreview.Cancel",
            "CommitCrossGroupPreviewState",
            "IconListReorderSuppression.Suppress",
            "_ctx.IconListReorderSuppression.Restore",
            "_crossGroupPreviewOverlayController.ShowPreview",
            "_crossGroupPreviewOverlayController.HidePreview",
            "_crossGroupPreviewPlacement.TryAnimatePlacement",
            "LauncherCrossGroupDropPlanner.ResolveAtDragEnd",
            "LauncherCrossGroupDropPlanner.PrepareCommit",
            "LauncherCrossGroupDropPlanner.ResolvePreviewInsertIndex",
            "LauncherCrossGroupDropPlanner.ResolveOnComplete"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !dragBusinessControllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少跨分组预览内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        Assert.IsTrue(
            coordinatorSource.Contains("LauncherCrossGroupPreviewState<TTarget, TSnapshot>", StringComparison.Ordinal)
            && coordinatorSource.Contains("LauncherCrossGroupPreviewTilePlanner.Plan", StringComparison.Ordinal)
            && coordinatorSource.Contains("SessionStarted", StringComparison.Ordinal)
            && coordinatorSource.Contains("TargetChanged", StringComparison.Ordinal)
            && coordinatorSource.Contains("LauncherCrossGroupPreviewCompletionPolicy.PlanCancel", StringComparison.Ordinal),
            "跨分组预览协调器必须组合会话版本与目标列表规划，并显式报告 UI 副作用决策。" );

        string[] requiredOverlayLifecycle =
        {
            "ContainerContentChanging += HandleContainerContentChanging",
            "ContainerContentChanging -= HandleContainerContentChanging",
            "HandleContainerContentChanging",
            "PrepareBitmaps",
            "ApplyTransparency",
            "dispatcherQueue.TryEnqueue",
            "isCurrent(token)",
            "source.IsHitTestVisible = false",
            "source.IsHitTestVisible = true"
        };
        var missingOverlayLifecycle = requiredOverlayLifecycle
            .Where(value => !overlaySource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingOverlayLifecycle.Length,
            "跨分组覆盖层控制器缺少虚拟化、透明度、token 或显隐生命周期:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingOverlayLifecycle));

        string[] forbiddenRawState =
        {
            "_crossGroupPreviewActive",
            "_crossGroupPreviewTarget",
            "_crossGroupPreviewSavedSelection",
            "_crossGroupPreviewToken",
            "private ListView? _crossGroupPreviewOverlay",
            "_crossGroupPreviewDisplayTiles",
            "EnsureCrossGroupPreviewOverlay",
            "CrossGroupPreviewOverlay_ContainerContentChanging",
            "PrepareCrossGroupPreviewTileBitmaps",
            "EnsureCrossGroupPreviewVisualTransparency",
            "ApplyCrossGroupPreviewIconVisual",
            "BuildCrossGroupPreviewTiles",
            "GetBatchTargetGroupInsertIndex",
            "_iconListReorderSuppressedForCrossGroupPreview",
            "_iconListReorderSuppressedForBatchDrag",
            "_savedIconListCanReorderItems",
            "_savedIconListReorderMode",
            "SuppressIconListReorderForCrossGroupPreview",
            "RestoreIconListReorderAfterCrossGroupPreview",
            "SuppressIconListReorderForBatchDrag",
            "RestoreIconListReorderAfterBatchDrag",
            "GetGroupTabOverlayOrigin",
            "GetCrossGroupInsertSlotOverlayOrigin",
            "GetPreviewSlotOverlayOriginFromListView",
            "GetShortcutIconOverlayOriginFromListView",
            "AnimateCrossGroupPreviewScaleIn",
            "ResolveGroupFromDropPoint(e) ?? _groupTabHover.Target ?? _crossGroupPreview.Target"
        };
        var rawStateViolations = forbiddenRawState
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawStateViolations.Length,
            "跨分组预览会话状态不应重新散落到 RightCard 字段:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawStateViolations));

        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo", "MenuInfo" };
        string combinedPureSource = stateSource + Environment.NewLine + coordinatorSource;
        var dependencyViolations = forbiddenDependencies
            .Where(value => combinedPureSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "跨分组预览状态必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));

        string[] forbiddenPlannerDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "IconInfo", "MenuInfo", "raindesk.Services" };
        var plannerViolations = forbiddenPlannerDependencies
            .Where(value => tilePlannerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            plannerViolations.Length,
            "跨分组预览列表规划必须保持 UI 和业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, plannerViolations));

        string[] requiredDropViewBehavior =
        {
            "_selectAllGroups()",
            "_applyAllGroupsOrder(currentOrder)",
            "_rebuildTiles()",
            "_refreshLayout()",
            "_refreshGroupSelection(selectedGroupIndex)",
            "beforePersist?.Invoke()",
            "_persist()"
        };
        var missingDropViewBehavior = requiredDropViewBehavior
            .Where(value => !dropViewSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDropViewBehavior.Length,
            "跨分组提交视图协调器缺少导航、投影、布局、清理或持久化步骤:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDropViewBehavior));

        string[] forbiddenDropViewDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "CommonCode", "IconInfo", "MenuInfo", "raindesk.Services" };
        var dropViewDependencyViolations = forbiddenDropViewDependencies
            .Where(value => dropViewSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dropViewDependencyViolations.Length,
            "跨分组提交视图协调器必须通过组合根回调工作，不应依赖 UI、业务模型或持久化实现:" + Environment.NewLine
                + string.Join(Environment.NewLine, dropViewDependencyViolations));

        string combinedSuppressionSource = suppressionStateSource + Environment.NewLine + reorderControllerSource;
        string[] forbiddenSuppressionDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "IconInfo", "MenuInfo" };
        var suppressionViolations = forbiddenSuppressionDependencies
            .Where(value => combinedSuppressionSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            suppressionViolations.Length,
            "Launcher 重排抑制状态不应依赖窗口、业务模型或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, suppressionViolations));

        string[] forbiddenAnchorDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "IconInfo", "MenuInfo", "raindesk.Services" };
        var anchorViolations = forbiddenAnchorDependencies
            .Where(value => anchorSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            anchorViolations.Length,
            "跨分组插入槽锚点选择必须保持 UI 和业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, anchorViolations));

        string[] requiredPlacementLifecycle =
        {
            "LauncherCrossGroupPreviewAnchorSelector.Select",
            "TryGetItemOrigin",
            "ResolveFallbackTarget",
            "HideDraggedSourceContainers",
            "isCurrent(token)",
            "animate(slot.Value.X, slot.Value.Y, 1d)"
        };
        var missingPlacementLifecycle = requiredPlacementLifecycle
            .Where(value => !placementSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingPlacementLifecycle.Length,
            "跨分组预览落位控制器缺少锚点、标签回退、显隐或动画门禁:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingPlacementLifecycle));
        string[] forbiddenPlacementDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "IconInfo", "MenuInfo" };
        var placementViolations = forbiddenPlacementDependencies
            .Where(value => placementSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            placementViolations.Length,
            "跨分组预览落位控制器不应依赖窗口、业务模型或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, placementViolations));

        string[] forbiddenDropPlannerDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "IconInfo", "MenuInfo", "raindesk.Services" };
        var dropPlannerViolations = forbiddenDropPlannerDependencies
            .Where(value => dropPlannerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dropPlannerViolations.Length,
            "跨分组放置目标和提交准备规则必须保持 UI 与业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, dropPlannerViolations));

        string[] forbiddenOverlayDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "ILauncherCommandHandler" };
        var overlayViolations = forbiddenOverlayDependencies
            .Where(value => overlaySource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            overlayViolations.Length,
            "跨分组预览层控制器不应依赖窗口、业务状态或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, overlayViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesCrossGroupMoveCommitToApplicationCoordinator()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application",
            "LauncherCrossGroupMoveCoordinator.cs"));

        string[] requiredDelegations =
        {
            "LauncherCrossGroupMoveCoordinator",
            "_dragBusiness.TryCommitCrossGroupDropAtDragEnd"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少跨分组移动应用层协调器委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "CrossGroupMoveCoordinator.Execute",
            "LauncherCrossGroupMoveRequest",
            "LauncherCrossGroupMoveViewAction.RestoreCapturedAllGroupsOrder"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少跨分组移动内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        Assert.IsFalse(
            rightCardSource.Contains("ResolveCrossGroupTargetInsertIndex", StringComparison.Ordinal)
            || rightCardSource.Contains("RefreshAllGroupsFlatOrderAfterCrossGroupMove", StringComparison.Ordinal),
            "跨组插入命令规划和失败兜底不应重新进入 RightCard。");

        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo", "MenuInfo" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => coordinatorSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "跨组移动协调器必须只依赖 Launcher 契约和稳定 ID:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesAllGroupsOrderSnapshotsToLauncherState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherAllGroupsOrderState.cs"));

        string[] requiredDelegations =
        {
            "LauncherAllGroupsOrderState<IconInfo>",
            "_dragBusiness.CompleteDragReorder"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少全部分组排序状态委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));

        Assert.IsTrue(
            controllerSource.Contains("_allGroupsOrder.SetCurrent", StringComparison.Ordinal),
            "LauncherDragBusinessController 缺少 _allGroupsOrder.SetCurrent 调用。");

        string[] requiredControllerDelegations =
        {
            "_allGroupsOrder.CaptureDragStart",
            "_allGroupsOrder.CaptureBeforePreview",
            "_allGroupsOrder.RestoreCurrentFromBestSnapshot",
            "ClearAllGroupsDragOrderSnapshots"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少全部分组排序内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] forbiddenRawState =
        {
            "_allGroupsFlatOrder",
            "_allGroupsFlatOrderBeforePreview",
            "_allGroupsFlatOrderAtDragStart",
            "GetAllGroupsOrderSnapshotForCrossGroupPreview",
            "GetAllGroupsFlatOrderForCancelledCrossGroupDrag"
        };
        var rawStateViolations = forbiddenRawState
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            rawStateViolations.Length,
            "全部分组排序和拖拽快照不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, rawStateViolations));

        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo", "MenuInfo" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => stateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "全部分组排序状态必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesGroupTabHoverAndAutoScrollToLauncherPresentation()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string hoverSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherGroupTabHoverState.cs"));
        string scrollSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherHorizontalAutoScrollPlanner.cs"));
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherGroupBarDragController.cs"));

        string[] requiredDelegations =
        {
            "LauncherGroupBarDragController<MenuInfo, IconInfo>",
            "_groupBarDrag.HandleDragOver",
            "_groupBarDrag.TryUpdateHover",
            "_groupBarDrag.ResolveTarget",
            "_groupBarDrag.HoverTarget",
            "_groupBarDrag.ClearIndicators",
            "_groupBarDrag.ClearHighlight"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少分组悬停/自动滚动 Presentation 委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string[] forbiddenPageDetails =
        {
            "_highlightedGroupTab",
            "LauncherGroupTabHoverState<MenuInfo>",
            "_groupTabHover.SetTarget",
            "_groupTabHover.Clear",
            "LauncherHorizontalAutoScrollPlanner.Plan",
            "HandleCrossGroupIconDragOver",
            "TryUpdateCrossGroupDragHover",
            "ResolveGroupFromDropPoint",
            "HitTestGroupTabFromPoint",
            "TryAutoScrollGroupTabsDuringDrag",
            "IsDragPointOverGroupBar",
            "HighlightGroupTabFromDrag",
            "ClearAllGroupTabDragDropIndicators",
            "ClearGroupTabArrowOnly",
            "ClearGroupTabHighlight",
            "LogGroupTabDragDropState"
        };
        var pageDetailViolations = forbiddenPageDetails
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            pageDetailViolations.Length,
            "分组栏拖拽命中、滚动和悬停细节不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, pageDetailViolations));

        string[] requiredControllerBehavior =
        {
            "LauncherGroupTabHoverState<TMenu>",
            "LauncherDropHitTestAdapter.FindRealizedItem<TMenu>",
            "LauncherHorizontalAutoScrollPlanner.Plan",
            "UpdateHover",
            "ClearIndicators",
            "_restorePreview()"
        };
        var missingControllerBehavior = requiredControllerBehavior
            .Where(value => !controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerBehavior.Length,
            "分组栏拖拽控制器缺少命中、滚动、悬停或预览恢复协调:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerBehavior));

        string combinedSource = hoverSource + Environment.NewLine + scrollSource;
        string[] forbiddenDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "raindesk.Services", "IconInfo", "MenuInfo" };
        var dependencyViolations = forbiddenDependencies
            .Where(value => combinedSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            dependencyViolations.Length,
            "分组悬停和自动滚动规划必须保持 UI/业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, dependencyViolations));

        string[] forbiddenControllerDependencies =
            { "MainWindow", "AppData", "CommonCode", "raindesk.Services", "IconInfo", "MenuInfo" };
        var controllerDependencyViolations = forbiddenControllerDependencies
            .Where(value => controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            controllerDependencyViolations.Length,
            "分组栏拖拽控制器不应依赖窗口、业务模型或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, controllerDependencyViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesIconSelectionAndMarqueeToLauncherPresentation()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherIconSelectionState.cs"));
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherIconSelectionController.cs"));

        string[] requiredDelegations =
        {
            "LauncherIconSelectionController<IconInfo>",
            "_iconSelection.HandleItemPointerPressed",
            "_iconSelection.HandleHostPointerPressed",
            "_iconSelection.HandleHostPointerMoved",
            "_iconSelection.HandleHostPointerReleased",
            "_iconSelection.HandleHostPointerCanceled",
            "_iconSelection.ClearSelection"
        };
        var missingDelegations = requiredDelegations
            .Where(value => !rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingDelegations.Length,
            "RightCard 缺少 Launcher 图标选择/框选委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingDelegations));

        string dragBusinessSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        string[] requiredControllerDelegations =
        {
            "PrepareDragVisuals",
            "ReplaceSelectionObjects",
            "_ctx.IconSelection.RestoreDragVisuals"
        };
        var missingControllerDelegations = requiredControllerDelegations
            .Where(value => !dragBusinessSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingControllerDelegations.Length,
            "LauncherDragBusinessController 缺少图标选择内部委托:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingControllerDelegations));

        string[] forbiddenPageDetails =
        {
            "_selectionAnchorIndex",
            "_iconMarqueeActive",
            "_iconMarqueeAdditive",
            "_iconMarqueeStart",
            "_iconMarqueePointerId",
            "_dragSavedListSelections",
            "ToggleIconSelection",
            "SelectIconRangeTo",
            "SyncIconSelectionVisuals",
            "ApplyIconMarqueeSelection",
            "UpdateIconMarqueeVisual",
            "NormalizeRect(Point start, Point end)",
            "GetIconHitRect"
        };
        var pageDetailViolations = forbiddenPageDetails
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            pageDetailViolations.Length,
            "选择锚点、框选状态、命中几何和视觉同步不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, pageDetailViolations));

        string[] forbiddenStateDependencies =
            { "Microsoft.UI", "Windows.Foundation", "MainWindow", "AppData", "IconInfo", "MenuInfo", "raindesk.Services" };
        var stateViolations = forbiddenStateDependencies
            .Where(value => stateSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            stateViolations.Length,
            "Launcher 选择状态必须保持 UI 和业务模型无关:" + Environment.NewLine
                + string.Join(Environment.NewLine, stateViolations));

        Assert.IsTrue(
            controllerSource.Contains("LauncherIconSelectionState<TItem>", StringComparison.Ordinal)
            && controllerSource.Contains("InputKeyboardSource.GetKeyStateForCurrentThread", StringComparison.Ordinal)
            && controllerSource.Contains("ContainerFromIndex", StringComparison.Ordinal)
            && controllerSource.Contains("CapturePointer", StringComparison.Ordinal)
            && controllerSource.Contains("ReleasePointerCaptures", StringComparison.Ordinal)
            && controllerSource.Contains("_state.CaptureDragVisuals", StringComparison.Ordinal),
            "Launcher WinUI 选择控制器缺少键盘、命中、指针捕获或拖拽视觉生命周期。" );

        string[] forbiddenControllerDependencies =
            { "MainWindow", "AppData", "CommonCode", "IconInfo", "MenuInfo", "raindesk.Services" };
        var controllerViolations = forbiddenControllerDependencies
            .Where(value => controllerSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            controllerViolations.Length,
            "Launcher WinUI 选择控制器不应依赖窗口、业务模型或持久化:" + Environment.NewLine
                + string.Join(Environment.NewLine, controllerViolations));
    }

    [TestMethod]
    public void RightCard_DelegatesLauncherActivationToUseCaseAndWindowsAdapter()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // Activation delegation now lives in LauncherContextMenuHandler
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherContextMenuHandler.cs"));
        string serviceSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application",
            "LauncherActivationService.cs"));
        string adapterSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "WindowsLauncherActivationPort.cs"));
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));

        Assert.IsTrue(
            handlerSource.Contains("LauncherIconActivationCoordinator<IconInfo>", StringComparison.Ordinal)
            && handlerSource.Contains("_iconActivation.HandleTap", StringComparison.Ordinal)
            && handlerSource.Contains("_iconActivation.HandleDoubleTap", StringComparison.Ordinal)
            && handlerSource.Contains("LauncherActivationMode.Administrator", StringComparison.Ordinal)
            && handlerSource.Contains("LauncherActivationMode.ShowInFileExplorer", StringComparison.Ordinal),
            "LauncherContextMenuHandler 必须只向 Launcher 激活协调器分发单击、双击和上下文菜单意图。" );
        Assert.IsFalse(
            rightCardSource.Contains("ProcessUtil.StartIconApp", StringComparison.Ordinal)
            || rightCardSource.Contains("lastLaunchedIcon", StringComparison.Ordinal)
            || rightCardSource.Contains("lastLaunchTick", StringComparison.Ordinal),
            "进程启动和 800ms 防重状态不应重新进入 RightCard。" );
        Assert.IsTrue(
            serviceSource.Contains("ILauncherActivationPort", StringComparison.Ordinal)
            && serviceSource.Contains("DefaultDebounceMilliseconds = 800", StringComparison.Ordinal)
            && serviceSource.Contains("LauncherActivationThrottleState", StringComparison.Ordinal),
            "Launcher 激活用例必须通过 Port 调用基础设施并集中处理稳定 ID 防重。" );
        Assert.IsTrue(
            adapterSource.Contains("ShortcutFolderTreeService", StringComparison.Ordinal)
            && adapterSource.Contains(".EnumerateIcons", StringComparison.Ordinal)
            && adapterSource.Contains("ProcessUtil.StartIconApp", StringComparison.Ordinal)
            && adapterSource.Contains("LauncherActivationMode.ShowInFileExplorer", StringComparison.Ordinal),
            "Windows Launcher 激活适配器必须负责 ID 查找和 Shell 模式映射。" );
        Assert.IsTrue(
            appServicesSource.Contains("ILauncherActivationPort, WindowsLauncherActivationPort", StringComparison.Ordinal)
            && appServicesSource.Contains("ILauncherActivationService, LauncherActivationService", StringComparison.Ordinal),
            "Launcher 激活 Port 与用例必须在 composition root 注册。" );
    }

    [TestMethod]
    public void RightCard_DelegatesLauncherContextCapabilitiesAndMutationsToModule()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // Context menu delegation now lives in LauncherContextMenuHandler
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherContextMenuHandler.cs"));
        string rightCardXaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml"));
        string policySource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application",
            "LauncherContextMenuPolicy.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherContextMenuCoordinator.cs"));
        string viewControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherContextMenuViewController.cs"));
        string commandHandlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));

        Assert.IsTrue(
            handlerSource.Contains("LauncherContextMenuCoordinator<IconInfo>", StringComparison.Ordinal)
            && handlerSource.Contains("_contextMenu.CanUse", StringComparison.Ordinal)
            && handlerSource.Contains("_contextMenu.CreateRemoveCommand", StringComparison.Ordinal)
            && handlerSource.Contains("_contextMenu.CreateResizeFolderCommand", StringComparison.Ordinal)
            && handlerSource.Contains("LauncherContextMenuViewController<IconInfo>", StringComparison.Ordinal)
            && handlerSource.Contains("_contextMenuView.Apply(plan, icon)", StringComparison.Ordinal),
            "LauncherContextMenuHandler 必须把菜单能力与稳定 ID 写命令委托给 Launcher Presentation。" );
        Assert.IsFalse(
            rightCardSource.Contains("ShortcutFolderTreeService.RemoveFromTree", StringComparison.Ordinal)
            || rightCardSource.Contains("ShortcutFolderService.ChangeFolderSize", StringComparison.Ordinal)
            || rightCardSource.Contains("ShortcutFolderService.MoveIconOutOfFolder", StringComparison.Ordinal),
            "删除、文件夹尺寸和移出文件夹写操作不应重新进入 RightCard。" );
        Assert.IsTrue(
            policySource.Contains("LauncherContextMenuRequest", StringComparison.Ordinal)
            && policySource.Contains("request.IsPlaceholder", StringComparison.Ordinal)
            && policySource.Contains("request.IsFolder", StringComparison.Ordinal)
            && policySource.Contains("request.HasFileLocation", StringComparison.Ordinal),
            "Launcher Application 必须集中决定占位项、文件夹和普通快捷方式的菜单能力。" );
        Assert.IsTrue(
            coordinatorSource.Contains("SubjectShortcutId: _getId(item)", StringComparison.Ordinal)
            && coordinatorSource.Contains("LauncherMutationKind.RemoveShortcut", StringComparison.Ordinal)
            && coordinatorSource.Contains("LauncherMutationKind.ResizeFolder", StringComparison.Ordinal)
            && coordinatorSource.Contains("LauncherMutationKind.MoveOutOfFolder", StringComparison.Ordinal),
            "Launcher Presentation 必须使用稳定 ID 构造上下文写命令。" );
        Assert.IsTrue(
            viewControllerSource.Contains("IReadOnlyDictionary<LauncherContextMenuAction, MenuFlyoutItemBase>", StringComparison.Ordinal)
            && viewControllerSource.Contains("item.Visibility = ToVisibility", StringComparison.Ordinal)
            && viewControllerSource.Contains("AssignTarget(child, target)", StringComparison.Ordinal),
            "Launcher WinUI 控制器必须集中投影菜单显隐、分隔线和子菜单目标。" );
        Assert.IsTrue(
            commandHandlerSource.Contains("ShortcutFolderTreeService.RemoveFromTree", StringComparison.Ordinal)
            && commandHandlerSource.Contains("ShortcutFolderService.ChangeFolderSize", StringComparison.Ordinal)
            && commandHandlerSource.Contains("ShortcutFolderService.MoveIconOutOfFolder", StringComparison.Ordinal)
            && commandHandlerSource.Contains("_saveService.SaveDebounced", StringComparison.Ordinal),
            "Launcher Infrastructure 必须负责树变更和持久化请求。" );

        string[] actionItemNames =
        {
            "AdminStartContextMenuItem",
            "ShowInExplorerContextMenuItem",
            "PropertiesContextMenuItem",
            "RenameContextMenuItem",
            "RemoveContextMenuItem"
        };
        Assert.AreEqual(
            0,
            actionItemNames.Count(name => !rightCardXaml.Contains($"x:Name=\"{name}\"", StringComparison.Ordinal)),
            "Launcher 菜单项必须可由统一能力计划控制可见性。" );
    }

    [TestMethod]
    public void LauncherEditDialogs_StageDraftsAndCommitThroughTypedCommands()
    {
        string repositoryRoot = FindRepositoryRoot();
        string dialogRoot = Path.Combine(repositoryRoot, "raindesk", "Control", "Other");
        string renameSource = File.ReadAllText(Path.Combine(dialogRoot, "IconRenameDialog.xaml.cs"));
        string propertiesSource = File.ReadAllText(Path.Combine(dialogRoot, "IconInfoDialog.xaml.cs"));
        string urlSource = File.ReadAllText(Path.Combine(dialogRoot, "IconInfoUrlDialog.xaml.cs"));
        string draftSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherShortcutEditDraft.cs"));
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));
        Assert.IsTrue(
            renameSource.Contains("RenameLauncherShortcutCommand", StringComparison.Ordinal)
            && renameSource.Contains("_commandHandler.Rename", StringComparison.Ordinal),
            "重命名对话框必须通过稳定 ID 类型化命令提交。" );
        Assert.IsTrue(
            propertiesSource.Contains("LauncherShortcutEditDraft", StringComparison.Ordinal)
            && propertiesSource.Contains("_commandHandler.Update", StringComparison.Ordinal)
            && propertiesSource.Contains("_draft.ReplaceImage", StringComparison.Ordinal)
            && propertiesSource.Contains("RestoreDefaultImage()", StringComparison.Ordinal),
            "普通属性对话框必须在草稿中编辑文本和图标，再一次性提交。" );
        Assert.IsTrue(
            urlSource.Contains("LauncherShortcutEditDraft", StringComparison.Ordinal)
            && urlSource.Contains("_commandHandler.Update", StringComparison.Ordinal)
            && urlSource.Contains("_commandHandler.Add", StringComparison.Ordinal)
            && urlSource.Contains("LauncherShortcutKind.Url", StringComparison.Ordinal),
            "URL 对话框必须通过 Launcher Update/Add 命令提交，不得直接写 Store。" );

        string combinedDialogs = renameSource + propertiesSource + urlSource;
        string[] forbiddenDialogWrites =
        {
            "_saveService",
            "_dataProvider.Write",
            "iconInfo.Name =",
            "info.Name =",
            "info.Path =",
            "info.AdminStartUp =",
            "info.StartArg =",
            "info.ImageByteArr =",
            "info.BitmapImage ="
        };
        var violations = forbiddenDialogWrites
            .Where(value => combinedDialogs.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            violations.Length,
            "快捷方式编辑对话框取消前不得污染模型或自行保存:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));

        Assert.IsTrue(
            draftSource.Contains("ImageChanged", StringComparison.Ordinal)
            && draftSource.Contains("CreateUpdateCommand", StringComparison.Ordinal)
            && draftSource.Contains("?.ToArray()", StringComparison.Ordinal),
            "编辑草稿必须记录显式图标变更，并防御性复制字节数组。" );
        Assert.IsTrue(
            handlerSource.Contains("public LauncherCommandResult Rename", StringComparison.Ordinal)
            && handlerSource.Contains("public LauncherCommandResult Update", StringComparison.Ordinal)
            && handlerSource.Contains("UpdateShortcut", StringComparison.Ordinal)
            && handlerSource.Contains("_saveService.SaveDebounced", StringComparison.Ordinal),
            "Launcher Infrastructure 必须集中查找稳定 ID、应用更新并请求持久化。" );
    }

    [TestMethod]
    public void RightCard_DelegatesExternalFileResolutionAndBatchAddToLauncherModule()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        int dropStart = rightCardSource.IndexOf("private async void Wrap_Drop", StringComparison.Ordinal);
        int dropEnd = rightCardSource.IndexOf(
            "private void SyncFolderOverlayAfterFolderMutation",
            dropStart,
            StringComparison.Ordinal);
        Assert.IsTrue(dropStart >= 0 && dropEnd > dropStart, "无法定位外部文件 Drop 处理器。");
        string dropBody = rightCardSource[dropStart..dropEnd];

        string applicationSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application",
            "LauncherExternalImportService.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherExternalImportCoordinator.cs"));
        string adapterSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "WindowsLauncherShortcutDraftSource.cs"));
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));

        Assert.IsTrue(
            dropBody.Contains("OfType<global::Windows.Storage.StorageFile>()", StringComparison.Ordinal)
            && dropBody.Contains("_externalImport.ImportAsync", StringComparison.Ordinal)
            && dropBody.Contains("result.AddedCount > 0", StringComparison.Ordinal),
            "RightCard Drop 事件只能收集 StorageFile 路径、提交导入并刷新一次视图。" );
        string[] forbiddenDropWrites =
        {
            "CommonCode.GetIconInfoByPath",
            "CommonCode.SortIconList",
            "IconList.Add",
            "SaveAppDataDebounced"
        };
        Assert.AreEqual(
            0,
            forbiddenDropWrites.Count(value => dropBody.Contains(value, StringComparison.Ordinal)),
            "路径解析、模型添加、排序和保存不得重新进入 RightCard Drop 处理器。" );
        Assert.IsTrue(
            applicationSource.Contains("_draftSource.ResolveAsync", StringComparison.Ordinal)
            && applicationSource.Contains("_commandHandler.AddMany", StringComparison.Ordinal)
            && applicationSource.Contains("ApplyConfiguredSort: true", StringComparison.Ordinal),
            "Launcher Application 必须汇总逐路径解析结果并只提交一次批量命令。" );
        Assert.IsTrue(
            coordinatorSource.Contains("generation == _generation", StringComparison.Ordinal)
            && coordinatorSource.Contains("CancelPending", StringComparison.Ordinal)
            && coordinatorSource.Contains("CancellationTokenSource", StringComparison.Ordinal),
            "Launcher Presentation 必须取消旧导入批次并拦截晚到结果。" );
        Assert.IsTrue(
            adapterSource.Contains("FileUtil.GetTargetPathByLnk", StringComparison.Ordinal)
            && adapterSource.Contains("ImageUtil.GetImageBytesByPath", StringComparison.Ordinal)
            && adapterSource.Contains("File.Exists", StringComparison.Ordinal)
            && adapterSource.Contains("LauncherShortcutDraft", StringComparison.Ordinal),
            "Windows 路径与图标解析只能存在于 Launcher Infrastructure 适配器。" );
        Assert.IsFalse(
            adapterSource.Contains("IconInfo", StringComparison.Ordinal)
            || adapterSource.Contains("BitmapImage", StringComparison.Ordinal),
            "外部路径适配器只应返回 Contracts 草稿，不应构造 UI 模型或位图对象。" );
        Assert.IsTrue(
            handlerSource.Contains("public LauncherCommandResult AddMany", StringComparison.Ordinal)
            && handlerSource.Contains("SortAllShortcutLists", StringComparison.Ordinal)
            && handlerSource.Contains("_saveService.SaveDebounced", StringComparison.Ordinal),
            "批量添加必须在单一 Store 写边界中保留配置排序并请求一次保存。" );
        Assert.IsTrue(
            appServicesSource.Contains(
                "ILauncherShortcutDraftSource, WindowsLauncherShortcutDraftSource",
                StringComparison.Ordinal)
            && appServicesSource.Contains(
                "ILauncherExternalImportService, LauncherExternalImportService",
                StringComparison.Ordinal),
            "外部导入 Port 与 Application 服务必须在 composition root 注册。" );
    }

    [TestMethod]
    public void RightCard_DelegatesLauncherGridWritesToStableIdCommand()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutLayoutApplyController.cs"));
        string organizerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherGridOrganizer.cs"));
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));

        Assert.IsTrue(
            (controllerSource.Contains("LauncherGridOrganizer.Plan", StringComparison.Ordinal)
                || rightCardSource.Contains("LauncherGridOrganizer.Plan", StringComparison.Ordinal))
            && rightCardSource.Contains("_launcherCommandHandler.UpdateGridLayout", StringComparison.Ordinal)
            && rightCardSource.Contains("UpdateLauncherGridLayoutCommand", StringComparison.Ordinal)
            && rightCardSource.Contains("ResetLauncherGridPosition", StringComparison.Ordinal),
            "RightCard 和布局控制器必须把自动压缩、手动落位和位置重置提交为稳定 ID 网格命令。" );
        Assert.IsFalse(
            rightCardSource.Contains("icon.LauncherGridColumn =", StringComparison.Ordinal)
            || rightCardSource.Contains("icon.LauncherGridRow =", StringComparison.Ordinal)
            || rightCardSource.Contains("child.LauncherGridColumn =", StringComparison.Ordinal)
            || rightCardSource.Contains("child.LauncherGridRow =", StringComparison.Ordinal),
            "Launcher 网格坐标写入不得重新进入 RightCard。" );
        int directSaveCount = rightCardSource
            .Split("CommonCode.SaveAppDataDebounced", StringSplitOptions.None)
            .Length - 1;
        Assert.AreEqual(
            0,
            directSaveCount,
            "Launcher 网格与顺序命令接管后，RightCard 不应再直接请求保存。" );
        Assert.IsTrue(
            organizerSource.Contains("IReadOnlyList<LauncherGridPlacement> Plan", StringComparison.Ordinal)
            && organizerSource.Contains("placements.Add(new LauncherGridPlacement", StringComparison.Ordinal),
            "Launcher Presentation 网格组织器必须先生成不修改模型的布局计划。" );
        Assert.IsTrue(
            handlerSource.Contains("public LauncherCommandResult UpdateGridLayout", StringComparison.Ordinal)
            && handlerSource.Contains("FindLayoutIcon", StringComparison.Ordinal)
            && handlerSource.Contains("data.AppConfig.AllGroupsIconList", StringComparison.Ordinal)
            && handlerSource.Contains("_saveService.SaveDebounced", StringComparison.Ordinal),
            "Launcher Infrastructure 必须原子解析菜单树/All Groups 稳定 ID，并集中写坐标与保存。" );
    }

    [TestMethod]
    public void RightCard_DelegatesGroupAndAllGroupsOrderPersistenceToLauncherCommand()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));
        string projectionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherAllGroupsProjectionBuilder.cs"));
        string businessControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));

        Assert.IsTrue(
            rightCardSource.Contains("CommitCurrentShortcutOrder", StringComparison.Ordinal),
            "RightCard 必须通过委托提交顺序命令。" );
        Assert.IsTrue(
            (businessControllerSource.Contains("_launcherCommandHandler.UpdateOrder", StringComparison.Ordinal)
             || businessControllerSource.Contains("_launcherCommands.UpdateOrder", StringComparison.Ordinal))
            && businessControllerSource.Contains("LauncherShortcutOrderScope.AllGroups", StringComparison.Ordinal)
            && businessControllerSource.Contains("LauncherShortcutOrderScope.Group", StringComparison.Ordinal),
            "普通分组拖拽和 All Groups 投影都必须提交类型化顺序命令。" );
        string[] forbiddenPageOrderWrites =
        {
            "SyncShortcutTileOrderToSource",
            "SyncAllGroupsTileOrderToSource",
            "PersistAllGroupsFlatOrderKeys",
            "AppConfig.AllGroupsFlatIconOrderKeys =",
            "CommonCode.SaveAppDataDebounced"
        };
        Assert.AreEqual(
            0,
            forbiddenPageOrderWrites.Count(value =>
                rightCardSource.Contains(value, StringComparison.Ordinal)),
            "RightCard 不得再直接重排源集合、写 All Groups 键或保存。" );
        Assert.IsTrue(
            handlerSource.Contains("public LauncherCommandResult UpdateOrder", StringComparison.Ordinal)
            && handlerSource.Contains("LauncherShortcutOrderScope.Group", StringComparison.Ordinal)
            && handlerSource.Contains("LauncherShortcutOrderScope.AllGroups", StringComparison.Ordinal)
            && handlerSource.Contains("AllGroupsFlatIconOrderKeys = keys", StringComparison.Ordinal)
            && handlerSource.Contains("ApplyVisibleOrder", StringComparison.Ordinal),
            "Launcher Infrastructure 必须原子维护分组相对顺序和 All Groups 平面键。" );
        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        Assert.IsTrue(
            (rightCardSource.Contains("LauncherAllGroupsProjectionBuilder.Build", StringComparison.Ordinal)
                || controllerSource.Contains("LauncherAllGroupsProjectionBuilder.Build", StringComparison.Ordinal))
            && projectionSource.Contains("BuildPersistedOrder", StringComparison.Ordinal)
            && projectionSource.Contains("ResolveOrderKey", StringComparison.Ordinal)
            && projectionSource.Contains("BuildDefaultFlatOrder", StringComparison.Ordinal),
            "All Groups 持久化键兼容、默认合并和可见项投影必须由 Launcher Projection 构建器负责。" );
        Assert.IsFalse(
            rightCardSource.Contains("BuildFlatOrderFromPersistedKeys", StringComparison.Ordinal)
            || rightCardSource.Contains("FindIconByOrderKey", StringComparison.Ordinal)
            || rightCardSource.Contains("BuildIconOrderKey", StringComparison.Ordinal),
            "RightCard 不得重新承担 All Groups 持久化键解析。" );
    }

    [TestMethod]
    public void RightCard_DelegatesAllGroupsOwnedFolderWritesToLauncherCommand()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string handlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "AppDataLauncherCommandHandler.cs"));

        Assert.IsTrue(
            rightCardSource.Contains("_dragBusiness.TryExecuteAllGroupsFolderMutation", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherAllGroupsFolderMutationKind.MoveOut", StringComparison.Ordinal),
            "All Groups 自有文件夹的移出必须提交类型化 Launcher 命令。" );
        Assert.IsFalse(
            rightCardSource.Contains("AllGroupsShortcutViewService.TryCreateOwnedFolder", StringComparison.Ordinal)
            || rightCardSource.Contains("AllGroupsShortcutViewService.TryMoveIntoOwnedFolder", StringComparison.Ordinal)
            || rightCardSource.Contains("AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder", StringComparison.Ordinal),
            "RightCard 不得直接修改 All Groups 自有文件夹容器。" );

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragBusinessController.cs"));
        Assert.IsTrue(
            controllerSource.Contains("LauncherAllGroupsFolderMutationKind.Create", StringComparison.Ordinal)
            && controllerSource.Contains("LauncherAllGroupsFolderMutationKind.MoveInto", StringComparison.Ordinal)
            && controllerSource.Contains("_ctx.CrossGroupMoveCoordinator", StringComparison.Ordinal),
            "LauncherDragBusinessController 必须负责创建和移入文件夹变异，并通过 CrossGroupMoveCoordinator 协调。" );

        Assert.IsTrue(
            handlerSource.Contains("public LauncherCommandResult MutateAllGroupsFolder", StringComparison.Ordinal)
            && handlerSource.Contains("TryResolveExactAllGroupsOrder", StringComparison.Ordinal)
            && handlerSource.Contains("AllGroupsShortcutViewService.TryCreateOwnedFolder", StringComparison.Ordinal)
            && handlerSource.Contains("AllGroupsShortcutViewService.TryMoveIntoOwnedFolder", StringComparison.Ordinal)
            && handlerSource.Contains("AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder", StringComparison.Ordinal)
            && handlerSource.Contains("SetAllGroupsFlatOrderKeys", StringComparison.Ordinal),
            "Launcher Infrastructure 必须先验证完整稳定 ID 投影，再原子维护自有文件夹和顺序键。" );
    }

    [TestMethod]
    public void LauncherViews_DelegateTitlePersistenceAndBatchSessionStateToModules()
    {
        string repositoryRoot = FindRepositoryRoot();
        string panelRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard");
        string rightCardSource = File.ReadAllText(Path.Combine(panelRoot, "RightCardControl.xaml.cs"));
        string leftCardSource = File.ReadAllText(Path.Combine(panelRoot, "LeftCardControl.xaml.cs"));
        // Title visibility and batch mode delegation now live in LauncherContextMenuHandler
        string contextHandlerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherContextMenuHandler.cs"));
        string appearanceSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Appearance",
            "Infrastructure",
            "AppDataAppearanceHandlers.cs"));
        string batchStateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherBatchModeState.cs"));
        string batchControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherBatchModeController.cs"));
        string appConfigSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "ViewModel",
            "AppConfig.cs"));
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));

        Assert.IsTrue(
            contextHandlerSource.Contains("_appearanceQuery.GetCurrent", StringComparison.Ordinal)
            && contextHandlerSource.Contains("_appearanceCommand.SetIconTitleVisibility", StringComparison.Ordinal)
            && appearanceSource.Contains("public AppearanceCommandResult SetIconTitleVisibility", StringComparison.Ordinal),
            "图标标题显示设置必须通过 Appearance 查询/命令边界读取和提交。" );
        Assert.IsFalse(
            rightCardSource.Contains("AppConfig.ShowIconTitle =", StringComparison.Ordinal),
            "RightCard 不得直接写持久化外观配置。" );
        Assert.IsTrue(
            rightCardSource.Contains("LauncherBatchModeState", StringComparison.Ordinal)
            && leftCardSource.Contains("LauncherBatchModeState", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherBatchModeController<IconInfo>", StringComparison.Ordinal)
            && batchStateSource.Contains("public event EventHandler? Changed", StringComparison.Ordinal)
            && batchControllerSource.Contains("_listView.SelectionMode =", StringComparison.Ordinal)
            && batchControllerSource.Contains("public void Toggle()", StringComparison.Ordinal)
            && appServicesSource.Contains("AddSingleton<LauncherBatchModeState>", StringComparison.Ordinal),
            "左右面板必须共享 Launcher Presentation 中唯一的批量模式会话状态。" );
        Assert.IsFalse(
            rightCardSource.Contains("IconBatch_NoWrite", StringComparison.Ordinal)
            || leftCardSource.Contains("IconBatch_NoWrite", StringComparison.Ordinal)
            || appConfigSource.Contains("IconBatch_NoWrite", StringComparison.Ordinal),
            "批量选择是会话状态，不得继续存放在 AppConfig 或由页面直接切换。" );
    }

    [TestMethod]
    public void RightCard_DelegatesShortcutLayoutSchedulingToLauncherController()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        string rightCardXaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml"));
        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherLayoutScheduleState.cs"));
        string schedulerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutLayoutScheduler.cs"));
        string transitionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutTransitionController.cs"));
        string panelMetricsSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutPanelMetricsAdapter.cs"));
        string itemSpanSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutItemSpanAdapter.cs"));
        string folderTileSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherFolderTileVisualAdapter.cs"));
        string gridPanelSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherGridPanel.cs"));
        string viewportSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutViewportAdapter.cs"));
        string containerStateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutContainerStateController.cs"));

        Assert.IsTrue(
            rightCardSource.Contains("LauncherShortcutLayoutScheduler", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherShortcutLayoutApplyController", StringComparison.Ordinal)
            && rightCardSource.Contains("_layoutApply.Schedule", StringComparison.Ordinal)
            && rightCardSource.Contains("_layoutApply.ApplyFromScheduler", StringComparison.Ordinal)
            && schedulerSource.Contains("LauncherLayoutScheduleState", StringComparison.Ordinal)
            && schedulerSource.Contains("TimeSpan.FromMilliseconds(16)", StringComparison.Ordinal)
            && schedulerSource.Contains("ScheduleTransitionRestore", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherShortcutTransitionController", StringComparison.Ordinal)
            && transitionSource.Contains("_listView.ItemContainerTransitions =", StringComparison.Ordinal)
            && transitionSource.Contains("SetDesiredContainerUpdateDuration", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherShortcutPanelMetricsAdapter.Apply", StringComparison.Ordinal)
            && panelMetricsSource.Contains("case ItemsWrapGrid", StringComparison.Ordinal)
            && panelMetricsSource.Contains("case LauncherGridPanel", StringComparison.Ordinal)
            && panelMetricsSource.Contains("case VariableSizedWrapGrid", StringComparison.Ordinal)
            && panelMetricsSource.Contains("InvalidateMeasure", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherShortcutItemSpanAdapter.Apply", StringComparison.Ordinal)
            && itemSpanSource.Contains("CalculatePanelTileDisplaySize", StringComparison.Ordinal)
            && itemSpanSource.Contains("VariableSizedWrapGrid.ColumnSpanProperty", StringComparison.Ordinal)
            && itemSpanSource.Contains("ResetTemplate", StringComparison.Ordinal)
            && rightCardSource.Contains("LauncherFolderTileVisualAdapter.Apply", StringComparison.Ordinal)
            && folderTileSource.Contains("FolderVisualRoot", StringComparison.Ordinal)
            && folderTileSource.Contains("FolderPreviewItems", StringComparison.Ordinal)
            && folderTileSource.Contains("GetBodyHeight", StringComparison.Ordinal)
            && gridPanelSource.Contains(
                "namespace raindesk.Features.Launcher.Presentation.WinUI;",
                StringComparison.Ordinal)
            && rightCardXaml.Contains(
                "xmlns:launcher=\"using:raindesk.Features.Launcher.Presentation.WinUI\"",
                StringComparison.Ordinal)
            && rightCardXaml.Contains("<launcher:LauncherGridPanel", StringComparison.Ordinal)
            && rightCardSource.Contains(
                "LauncherShortcutViewportAdapter.GetAvailableWidth",
                StringComparison.Ordinal)
            && viewportSource.Contains("scrollViewer.ViewportWidth", StringComparison.Ordinal)
            && viewportSource.Contains("GetVerticalScrollbarOccupiedWidth", StringComparison.Ordinal)
            && viewportSource.Contains("FindDescendantScrollViewer", StringComparison.Ordinal)
            && rightCardSource.Contains(
                "LauncherShortcutContainerStateController",
                StringComparison.Ordinal)
            && rightCardSource.Contains(
                "_shortcutContainers.Restore(shortcutTiles)",
                StringComparison.Ordinal)
            && containerStateSource.Contains("ContainerFromIndex", StringComparison.Ordinal)
            && containerStateSource.Contains("_animations.Contains(item)", StringComparison.Ordinal)
            && containerStateSource.Contains("_refreshItemSpans();", StringComparison.Ordinal),
            "快捷区布局请求合并、代次校验和过渡恢复计时必须由 Launcher 调度控制器负责。" );
        Assert.IsFalse(
            File.Exists(Path.Combine(
                repositoryRoot,
                "raindesk",
                "Control",
                "UserControls",
                "PannelCard",
                "LauncherGridPanel.cs")),
            "LauncherGridPanel 必须归属 Launcher Presentation/WinUI，不能回到旧页面控件目录。" );
        string[] forbiddenPageSchedulerState =
        {
            "shortcutLayoutTimer",
            "shortcutTransitionRestoreTimer",
            "_shortcutLayoutUpdateScheduled",
            "_shortcutLayoutGeneration",
            "_shortcutLayoutAnimateResize",
            "CreateShortcutLayoutTimer",
            "CreateShortcutTransitionRestoreTimer",
            "SetShortcutItemTransitionsEnabled",
            "ApplyShortcutItemTransitionState",
            "RestoreShortcutItemTransitionsAfterDrag",
            "shortcutItemTransitions",
            "ApplyWrapGridMetrics",
            "ApplyLauncherGridPanelMetrics",
            "ApplyVariableSizedWrapGridMetrics",
            "ApplyShortcutItemSpan",
            "ResetShortcutTemplateLocalLayout",
            "SupportsVariableSizedShortcutPanel",
            "GetShortcutColumnSpan",
            "GetShortcutRowSpan",
            "ApplyFolderVisualLayoutFromSpan",
            "ApplyFolderPreviewContentLayout",
            "SetFolderPreviewGridTracks",
            "GetFolderPreviewIconSize",
            "GetIconListContentWidth",
            "RequiresVerticalScrollbarReservation",
            "GetVerticalScrollbarOccupiedWidth",
            "FindChildScrollViewer",
            "clearedTransforms",
            "preservedTransforms"
        };
        Assert.AreEqual(
            0,
            forbiddenPageSchedulerState.Count(value =>
                rightCardSource.Contains(value, StringComparison.Ordinal)),
            "布局计时器和请求合并状态不得重新散落到 RightCard。" );
        Assert.IsFalse(
            stateSource.Contains("Microsoft.UI", StringComparison.Ordinal)
            || stateSource.Contains("DispatcherQueue", StringComparison.Ordinal)
            || stateSource.Contains("IconInfo", StringComparison.Ordinal),
            "布局请求状态必须保持 UI 和业务模型无关。" );
    }

    [TestMethod]
    public void RightCard_DoesNotOwnLayoutSnapshotFields()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        string[] forbiddenFields =
        {
            "lastAppliedLayoutWidth",
            "lastAppliedColumnCount",
            "lastAppliedSlotWidth",
            "lastAppliedCardWidth",
            "lastAppliedGapWidth",
            "lastAppliedHorizontalOffset",
            "lastAppliedTileCount",
            "_isApplyingLayout",
            "_shortcutLayoutMetricsReady",
            "ApplyShortcutLayoutUpdateCore",
            "ApplyWrapGridLayoutMetrics",
            "ForceWrapGridLayout",
            "CalculateInitialShortcutColumns"
        };

        Assert.AreEqual(
            0,
            forbiddenFields.Count(value =>
                rightCardSource.Contains(value, StringComparison.Ordinal)),
            "布局快照字段和核心应用方法必须迁入 LauncherShortcutLayoutApplyController。" );

        string controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherShortcutLayoutApplyController.cs"));
        Assert.IsTrue(
            controllerSource.Contains("LauncherLayoutApplyState", StringComparison.Ordinal)
            && controllerSource.Contains("ApplyCore", StringComparison.Ordinal)
            && controllerSource.Contains("Compact", StringComparison.Ordinal),
            "布局应用控制器必须持有状态类型并实现核心应用和压缩方法。" );
    }

    [TestMethod]
    public void RightCard_DoesNotOwnDragTimerAndHandlerFields()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        string[] forbiddenFields =
        {
            "_dragFollowTimer",
            "_batchDropReleaseCleanupTimer",
            "_windowDragPointerMovedHandler",
            "_listViewDragPointerMovedHandler",
            "_systemDragSuppressHandler",
            "_systemDragSuppressHandlersRegistered",
            "EnsureDragFollowTimer",
            "StopDragFollowTimer",
            "RegisterSystemDragVisualSuppressionHandlers",
            "UnregisterSystemDragVisualSuppressionHandlers"
        };

        Assert.AreEqual(
            0,
            forbiddenFields.Count(value =>
                rightCardSource.Contains(value, StringComparison.Ordinal)),
            "拖拽计时器和指针处理器管理必须迁入 LauncherDragOrchestrator。" );

        string orchestratorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "LauncherDragOrchestrator.cs"));
        Assert.IsTrue(
            orchestratorSource.Contains("LauncherDragSessionState", StringComparison.Ordinal)
            && orchestratorSource.Contains("BeginPointerTracking", StringComparison.Ordinal)
            && orchestratorSource.Contains("DragFollowTimer_Tick", StringComparison.Ordinal),
            "拖拽编排器必须持有会话状态并管理指针跟踪和计时器。" );

        string stateSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherDragSessionState.cs"));
        Assert.IsTrue(
            stateSource.Contains("IsCustomDragActive", StringComparison.Ordinal)
            && stateSource.Contains("DraggedIcon", StringComparison.Ordinal)
            && stateSource.Contains("BatchDrag", StringComparison.Ordinal),
            "拖拽会话状态必须聚合所有拖拽纯状态字段。" );

        // Verify the page delegates to _dragState rather than owning its own fields
        Assert.IsTrue(
            rightCardSource.Contains("_dragState.IsCustomDragActive", StringComparison.Ordinal)
            || rightCardSource.Contains("_dragState.DraggedIcon", StringComparison.Ordinal),
            "RightCardControl 必须通过 _dragState 委托拖拽状态，而非拥有独立字段。" );

        // Verify the page uses _dragOrchestrator for timer/handler management
        Assert.IsTrue(
            rightCardSource.Contains("_dragOrchestrator.BeginPointerTracking", StringComparison.Ordinal)
            && rightCardSource.Contains("_dragOrchestrator.EndPointerTracking", StringComparison.Ordinal),
            "RightCardControl 必须通过 _dragOrchestrator 管理指针跟踪生命周期。" );
    }

    [TestMethod]
    public void LauncherViews_DelegateSystemDiscoveryAndFilteringToLauncherModule()
    {
        string repositoryRoot = FindRepositoryRoot();
        string rightCardSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));
        // System items delegation now lives in InlineAddPanelController
        string inlineControllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "WinUI",
            "InlineAddPanelController.cs"));
        string systemItemWindowSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "Windows",
            "SystemItemWindow.xaml.cs"));
        string systemItemControlSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "SystemItem",
            "SystemItem.xaml.cs"));
        string coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation",
            "LauncherInlineSystemItemsCoordinator.cs"));
        string sourceAdapter = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Infrastructure",
            "WindowsLauncherSystemCandidateSource.cs"));
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));

        Assert.IsTrue(
            inlineControllerSource.Contains("LauncherInlineAddPanelState", StringComparison.Ordinal)
            && inlineControllerSource.Contains("LauncherInlineSystemItemsCoordinator<IconInfo>", StringComparison.Ordinal)
            && inlineControllerSource.Contains("_systemItems.SelectAsync", StringComparison.Ordinal)
            && inlineControllerSource.Contains("_systemItems.FilterAsync", StringComparison.Ordinal),
            "InlineAddPanelController 必须将内联面板状态、加载、过滤与取消委托给 Launcher Presentation。" );
        Assert.IsTrue(
            systemItemWindowSource.Contains("_candidateSource.GetBuiltInSystemItemsAsync", StringComparison.Ordinal)
            && systemItemWindowSource.Contains("_candidateSource.GetStartMenuItemsAsync", StringComparison.Ordinal)
            && systemItemWindowSource.Contains("LauncherSystemCandidatePresentationMapper.ToIconInfo", StringComparison.Ordinal)
            && systemItemWindowSource.Contains("ReplaceCandidateLoadCancellation", StringComparison.Ordinal),
            "独立系统项目窗口也必须复用 Launcher 候选源，并支持加载取消。" );
        Assert.IsTrue(
            systemItemControlSource.Contains("LauncherSystemCandidatePresentationMapper.ToDraft", StringComparison.Ordinal)
            && systemItemControlSource.Contains("_launcherCommands.Add", StringComparison.Ordinal),
            "系统候选项点击必须直接映射 Contracts 草稿并通过 Launcher 命令添加。" );
        Assert.IsFalse(
            systemItemControlSource.Contains("CommonCode.GetIconInfoByPath", StringComparison.Ordinal)
            || systemItemControlSource.Contains("Constants.SYSTEM_APP_NAMES", StringComparison.Ordinal)
            || systemItemControlSource.Contains("new IconInfo", StringComparison.Ordinal),
            "SystemItem UI 不得重新解析路径或构造临时业务模型。" );

        string[] forbiddenPageDetails =
        {
            "inlineAddPanelOpen",
            "inlineSystemIcons",
            "inlineStartMenuIcons",
            "inlineStartMenuLoadCancel",
            "inlineSystemSearchTimer",
            "inlineLastSystemKeyword",
            "GetInlineSystemIconInfos",
            "GetInlineStartMenuInfos",
            "CollectInlineStartMenuFiles",
            "ApplyInlineStartMenuFilter",
            "Constants.SYSTEM_APP_NAMES",
            "Directory.GetFiles",
            "CommonCode.GetIconInfoByPath_NoWrite"
        };
        var pageViolations = forbiddenPageDetails
            .Where(value => rightCardSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            pageViolations.Length,
            "文件发现、缓存、取消和过滤状态不应重新散落到 RightCard:" + Environment.NewLine
                + string.Join(Environment.NewLine, pageViolations));

        string[] forbiddenWindowDetails =
        {
            "Environment.SpecialFolder.StartMenu",
            "Environment.SpecialFolder.CommonStartMenu",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "CollectFilePaths",
            "GetInfosParallel",
            "GetSysteIconInfos",
            "CommonCode.GetIconInfoByPath_NoWrite"
        };
        var windowViolations = forbiddenWindowDetails
            .Where(value => systemItemWindowSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            windowViolations.Length,
            "Windows 文件发现和旧图标提取只能留在 Launcher Infrastructure:" + Environment.NewLine
                + string.Join(Environment.NewLine, windowViolations));

        Assert.IsTrue(
            coordinatorSource.Contains("CancellationTokenSource", StringComparison.Ordinal)
            && coordinatorSource.Contains("generation != _generation", StringComparison.Ordinal)
            && coordinatorSource.Contains("Task.Delay(_filterDelay", StringComparison.Ordinal)
            && coordinatorSource.Contains("StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal),
            "Launcher 内联系统项目协调器必须具备代次校验、取消和大小写无关防抖过滤。" );
        Assert.IsTrue(
            sourceAdapter.Contains("Environment.SpecialFolder.StartMenu", StringComparison.Ordinal)
            && sourceAdapter.Contains("Environment.SpecialFolder.CommonStartMenu", StringComparison.Ordinal)
            && sourceAdapter.Contains("Directory.EnumerateFiles", StringComparison.Ordinal)
            && sourceAdapter.Contains("CommonCode.GetIconInfoByPath_NoWrite", StringComparison.Ordinal),
            "Windows 候选项适配器必须独占开始菜单扫描和旧图标提取适配。" );
        Assert.IsTrue(
            appServicesSource.Contains(
                "ILauncherSystemCandidateSource, WindowsLauncherSystemCandidateSource",
                StringComparison.Ordinal),
            "Launcher 系统候选项 Port 必须在 composition root 注册。" );
    }

    [TestMethod]
    public void LauncherAlgorithms_DoNotLiveInUiControlDirectory()
    {
        string repositoryRoot = FindRepositoryRoot();
        string oldRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard");
        string applicationRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application");
        string presentationRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Presentation");
        string[] applicationFiles =
        {
            "LauncherGridLayoutEngine.cs",
            "ShortcutDragPolicy.cs",
            "ShortcutLayoutCalculator.cs"
        };
        string[] presentationFiles =
        {
            "LauncherGridOrganizer.cs",
            "LauncherDragSession.cs"
        };

        var staleFiles = applicationFiles
            .Concat(presentationFiles)
            .Where(file => File.Exists(Path.Combine(oldRoot, file)))
            .ToArray();
        var missingFiles = applicationFiles
            .Where(file => !File.Exists(Path.Combine(applicationRoot, file)))
            .Concat(presentationFiles.Where(file => !File.Exists(Path.Combine(presentationRoot, file))))
            .ToArray();

        Assert.AreEqual(
            0,
            staleFiles.Length,
            "Launcher 算法不应继续留在 UI 控件目录:" + Environment.NewLine
                + string.Join(Environment.NewLine, staleFiles));
        Assert.AreEqual(
            0,
            missingFiles.Length,
            "Launcher 模块缺少已迁移的算法文件:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingFiles));
    }

    [TestMethod]
    public void LauncherPanelControls_DoNotMutateWindowStaticState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string panelRoot = Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard");
        string[] panelFiles =
        {
            Path.Combine(panelRoot, "LeftCardControl.xaml.cs"),
            Path.Combine(panelRoot, "RightCardControl.xaml.cs")
        };

        var violations = panelFiles
            .Where(file => File.ReadAllText(file).Contains("MainWindow.appData", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file))
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "Launcher/Workspace 面板不应重新依赖窗口静态状态:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void ApplicationCode_HasNoWindowStaticAppDataBridge()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectRoot = Path.Combine(repositoryRoot, "raindesk");
        var violations = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                string source = File.ReadAllText(file);
                return source.Contains("MainWindow.appData", StringComparison.Ordinal)
                    || source.Contains("public static AppData appData", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(repositoryRoot, file))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "应用代码不应恢复 MainWindow 的静态 AppData 桥接:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void MainWindow_RebindsDataContextWhenAppStateIsReplaced()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(repositoryRoot, "raindesk", "MainWindow.xaml.cs"));

        Assert.IsTrue(
            source.Contains("_dataProvider.StateReplaced += AppDataProvider_StateReplaced", StringComparison.Ordinal)
            && source.Contains("DataContext = replacement", StringComparison.Ordinal)
            && source.Contains("ObserveAppConfig(replacement.AppConfig)", StringComparison.Ordinal),
            "主窗口必须在导入/恢复替换 AppData 后重新绑定 DataContext 和配置观察器。");
    }

    [TestMethod]
    public void TodoPresentation_UsesStoreAndTodoServiceInsteadOfWindowState()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] files =
        {
            Path.Combine(repositoryRoot, "raindesk", "Control", "Windows", "ToDoWindow.xaml.cs"),
            Path.Combine(repositoryRoot, "raindesk", "Control", "Windows", "ToDoInfoWindow.xaml.cs"),
            Path.Combine(repositoryRoot, "raindesk", "Control", "UserControls", "ToDo", "TodoControl.xaml.cs"),
            Path.Combine(repositoryRoot, "raindesk", "Control", "UserControls", "ToDo", "ProjectSidebar.xaml.cs")
        };

        var violations = files
            .Where(file => File.ReadAllText(file).Contains("MainWindow.appData", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file))
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "待办 UI 不应绕过 IAppDataProvider / IToDoService:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void PluginSettingsContribution_IsCapabilityGatedAndRegisteredInSettingsRuntime()
    {
        string repositoryRoot = FindRepositoryRoot();
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));
        string routeSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Settings",
            "Presentation",
            "PluginSettingsContributionRoutes.cs"));
        string contributionSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Plugins",
            "Presentation",
            "WinUI",
            "PluginManagementSettingsContribution.cs"));

        string[] requiredComposition =
        {
            "PluginCapability.Settings",
            "PluginContributionKind.SettingsSection",
            "PluginGatedSettingsSectionProvider",
            "PluginGatedSettingsPageFactory",
            "PluginManagementSettingsSectionProvider",
            "PluginManagementSettingsPageFactory"
        };
        var missingComposition = requiredComposition
            .Where(value => !appServicesSource.Contains(value, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            0,
            missingComposition.Length,
            "Settings 插件贡献未完整接入 manifest、门禁、目录或页面解析器:" + Environment.NewLine
                + string.Join(Environment.NewLine, missingComposition));

        Assert.IsTrue(
            routeSource.Contains("PluginContributionKind.SettingsSection", StringComparison.Ordinal)
            && routeSource.Contains("_gate.IsEnabled", StringComparison.Ordinal)
            && routeSource.Contains("return Array.Empty<SettingsSectionDescriptor>()", StringComparison.Ordinal)
            && routeSource.Contains("page = null", StringComparison.Ordinal),
            "Settings 插件路由必须在元数据与页面创建两次执行类型化门禁，并隔离失败贡献。");
        Assert.IsTrue(
            contributionSource.Contains("ISettingsSectionProvider", StringComparison.Ordinal)
            && contributionSource.Contains("ISettingsPageFactory", StringComparison.Ordinal)
            && contributionSource.Contains("_catalog.GetPlugins()", StringComparison.Ordinal),
            "内置插件管理贡献必须同时提供真实设置分区和页面工厂。");
    }

    [TestMethod]
    public void PluginRuntimeRoutes_CoverAllDeclaredContributionKinds()
    {
        string repositoryRoot = FindRepositoryRoot();
        string appServicesSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Services",
            "AppServices.cs"));
        string searchSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Search",
            "Infrastructure",
            "EverythingSearchProvider.cs"));
        string settingsSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Settings",
            "Presentation",
            "PluginSettingsContributionRoutes.cs"));
        string launcherSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "Launcher",
            "Application",
            "LauncherPluginCommandRouter.cs"));
        string importerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Features",
            "ImportExport",
            "Application",
            "PluginImporterRouter.cs"));

        Assert.IsTrue(
            searchSource.Contains("PluginContributionKind.SearchProvider", StringComparison.Ordinal),
            "Search provider 必须使用类型化插件门禁。");
        Assert.IsTrue(
            settingsSource.Contains("PluginContributionKind.SettingsSection", StringComparison.Ordinal),
            "Settings section 必须使用类型化插件门禁。");
        Assert.IsTrue(
            launcherSource.Contains("PluginContributionKind.LauncherCommand", StringComparison.Ordinal)
            && launcherSource.Contains("CancelAfter(_executionTimeout)", StringComparison.Ordinal)
            && launcherSource.Contains("ExecutionFailed", StringComparison.Ordinal),
            "Launcher command 路由必须具备类型化门禁、超时和结构化失败隔离。");
        Assert.IsTrue(
            importerSource.Contains("PluginContributionKind.Importer", StringComparison.Ordinal)
            && importerSource.Contains("CancelAfter(_importTimeout)", StringComparison.Ordinal)
            && importerSource.Contains("ImportFailed", StringComparison.Ordinal),
            "Importer 路由必须具备类型化门禁、超时和结构化失败隔离。");
        Assert.IsTrue(
            appServicesSource.Contains("ILauncherPluginCommandRouter", StringComparison.Ordinal)
            && appServicesSource.Contains("IPluginImporterRouter", StringComparison.Ordinal),
            "Launcher command 与 Importer 路由必须在 composition root 注册。" );
    }

    [TestMethod]
    public void ModuleContractsAndApplications_DoNotReferenceUiOrInfrastructure()
    {
        string repositoryRoot = FindRepositoryRoot();
        string searchRoot = Path.Combine(repositoryRoot, "raindesk", "Features", "Search");
        string[] protectedDirectories =
        {
            Path.Combine(repositoryRoot, "raindesk", "Features", "Appearance", "Contracts"),
            Path.Combine(searchRoot, "Contracts"),
            Path.Combine(searchRoot, "Application"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "ImportExport", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Launcher", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Launcher", "Application"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Plugins", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Plugins", "Application"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Settings", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Settings", "Application"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Sidecar", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Sidecar", "Application"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Tray", "Contracts"),
            Path.Combine(repositoryRoot, "raindesk", "Features", "Workspace", "Contracts")
        };
        string[] forbiddenDependencies =
        {
            "Microsoft.UI",
            "raindesk.ViewModel",
            "raindesk.Plugins.EveryThing",
            "raindesk.Services",
            "raindesk.Util"
        };

        var violations = protectedDirectories
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .SelectMany(file => forbiddenDependencies
                .Where(dependency => File.ReadAllText(file).Contains(dependency, StringComparison.Ordinal))
                .Select(dependency => $"{Path.GetRelativePath(repositoryRoot, file)} -> {dependency}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "模块 Contracts/Application 越过了边界:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void RightCard_GroupEditDelegatedToController()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        // These method bodies should have been moved to GroupTabEditController;
        // only thin forwarding stubs should remain.
        string[] forbiddenBodies =
        {
            "_groupEdit.Begin(menuInfo",
            "_groupEditVisual.PrepareEditor(",
            "_groupEditVisual.DetachEditor(GroupListView",
            "_groupTabDrag.SuspendReorder()",
            "_groupTabDrag.RestoreReorder(",
            "_groupTabDeleteAnimation.AnimateOutAsync(",
            "WorkspaceGroupSelectionProjection.Apply(",
        };

        var violations = forbiddenBodies
            .Where(pattern => source.Contains(pattern, StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "分组编辑逻辑应迁入 GroupTabEditController:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void RightCard_InlineAddDelegatedToController()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        // These method bodies should have been moved to InlineAddPanelController.
        // Note: _inlineSystemItems.CancelPending() is allowed in Unloaded cleanup.
        string[] forbiddenBodies =
        {
            "_inlinePanel.TryOpen(LauncherInlinePanelMode.Url)",
            "_inlinePanel.TryOpen(LauncherInlinePanelMode.SystemItems)",
            "_inlinePanel.Close()",
            "_inlineSystemItems.SelectAsync(",
            "_inlineSystemItems.FilterAsync(",
        };

        var violations = forbiddenBodies
            .Where(pattern => source.Contains(pattern, StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "内联添加面板逻辑应迁入 InlineAddPanelController:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void RightCard_ContextMenuDelegatedToHandler()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "raindesk",
            "Control",
            "UserControls",
            "PannelCard",
            "RightCardControl.xaml.cs"));

        // These method bodies should have been moved to LauncherContextMenuHandler.
        string[] forbiddenBodies =
        {
            "_launcherContextMenu.Resolve(icon)",
            "_launcherContextMenuView.Apply(plan",
            "_launcherContextMenu.CanUse(icon",
            "_launcherContextMenu.CreateRemoveCommand(",
            "_launcherContextMenu.CreateResizeFolderCommand(",
            "DialogHelper.ConfirmDeleteAsync(",
            "IconInfoUrlDialog urlDialog = new IconInfoUrlDialog(info)",
            "IconInfoDialog dialog = new IconInfoDialog(info)",
            "IconRenameDialog renameDialog = new(",
        };

        var violations = forbiddenBodies
            .Where(pattern => source.Contains(pattern, StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(
            0,
            violations.Count,
            "右键菜单逻辑应迁入 LauncherContextMenuHandler:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "raindesk"))
                && Directory.Exists(Path.Combine(directory.FullName, "raindesk.Tests")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("无法定位 raindesk 仓库根目录。");
    }
}
