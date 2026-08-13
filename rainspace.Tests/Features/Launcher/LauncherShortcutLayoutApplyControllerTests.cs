using System;
using System.Collections.Generic;
using System.Linq;
using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Presentation;
using rainspace.Features.Launcher.Presentation.WinUI;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherShortcutLayoutApplyControllerTests
{
    private static LauncherShortcutLayoutApplyController CreateController(
        LauncherLayoutScheduleState? scheduleState = null,
        double availableWidth = 300d,
        int tileCount = 5,
        bool isCustomDragActive = false,
        bool isYieldPlaceholderDragActive = false,
        ILauncherCommandHandler? commandHandler = null,
        IEnumerable<IconInfo>? icons = null,
        Func<bool>? canApplyPanelMetrics = null,
        Action<ShortcutLayoutMetrics, bool>? applyMetricsToPanels = null,
        Action? forcePanelLayout = null,
        Action? restoreContainerState = null,
        Action<ShortcutLayoutMetrics, double, int>? applyMetricsToPage = null,
        Func<bool>? canPersistGridLayout = null)
    {
        scheduleState ??= new LauncherLayoutScheduleState();
        commandHandler ??= new TestCommandHandler();
        icons ??= Enumerable.Empty<IconInfo>();
        canApplyPanelMetrics ??= () => true;

        return new LauncherShortcutLayoutApplyController(
            scheduleState,
            getAvailableWidth: () => availableWidth,
            getTileCount: () => tileCount,
            isCustomDragActive: () => isCustomDragActive,
            isYieldPlaceholderDragActive: () => isYieldPlaceholderDragActive,
            commandHandler: commandHandler,
            shortcutIconsProvider: () => icons,
            applyMetricsToPanels: applyMetricsToPanels ?? ((_, _) => { }),
            forcePanelLayout: forcePanelLayout ?? (() => { }),
            refreshLayout: () => { },
            refreshSpans: () => { },
            restoreContainerState: restoreContainerState ?? (() => { }),
            applyMetricsToPage: applyMetricsToPage ?? ((_, _, _) => { }),
            canApplyPanelMetrics: canApplyPanelMetrics,
            reportError: _ => { },
            canPersistGridLayout: canPersistGridLayout);
    }

    private static void SimulateSchedulerTick(LauncherLayoutScheduleState state, out int generation, out bool animate)
    {
        state.Request(false);
        state.TryConsume(out var request);
        generation = request.Generation;
        animate = request.AnimateRequested;
    }

    [TestMethod]
    public void ApplyFromScheduler_DoesNothingWhenWidthIsZero()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(scheduleState: scheduleState, availableWidth: 0d);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        Assert.IsFalse(controller.State.MetricsReady);
    }

    [TestMethod]
    public void ApplyFromScheduler_DoesNothingWhenWidthIsNegative()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(scheduleState: scheduleState, availableWidth: -10d);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        Assert.IsFalse(controller.State.MetricsReady);
    }

    [TestMethod]
    public void ApplyFromScheduler_UpdatesSnapshotOnFirstApply()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(scheduleState: scheduleState, availableWidth: 300d, tileCount: 5);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        Assert.IsTrue(controller.State.MetricsReady);
        Assert.AreEqual(300d, controller.State.LastApplied.AvailableWidth);
        Assert.AreEqual(5, controller.State.LastApplied.TileCount);
    }

    [TestMethod]
    public void ApplyFromScheduler_RefreshesSpansWhenNoMeaningfulChange()
    {
        bool spansRefreshed = false;
        var scheduleState = new LauncherLayoutScheduleState();

        var controller = new LauncherShortcutLayoutApplyController(
            scheduleState,
            getAvailableWidth: () => 300d,
            getTileCount: () => 5,
            isCustomDragActive: () => false,
            isYieldPlaceholderDragActive: () => false,
            commandHandler: new TestCommandHandler(),
            shortcutIconsProvider: () => Enumerable.Empty<IconInfo>(),
            applyMetricsToPanels: (_, _) => { },
            forcePanelLayout: () => { },
            refreshLayout: () => { },
            refreshSpans: () => spansRefreshed = true,
            restoreContainerState: () => { },
            applyMetricsToPage: (_, _, _) => { },
            canApplyPanelMetrics: () => true,
            reportError: _ => { });

        // First apply sets the snapshot
        SimulateSchedulerTick(scheduleState, out int gen1, out bool anim1);
        controller.ApplyFromScheduler(gen1, anim1);
        spansRefreshed = false;

        // Second apply with same params should only refresh spans
        SimulateSchedulerTick(scheduleState, out int gen2, out bool anim2);
        controller.ApplyFromScheduler(gen2, anim2);

        Assert.IsTrue(spansRefreshed, "Spans should be refreshed when no meaningful change.");
    }

    [TestMethod]
    public void ApplyFromScheduler_AppliesPanelMetricsWhenChangeDetected()
    {
        bool panelsApplied = false;
        bool layoutForced = false;
        bool containersRestored = false;
        var scheduleState = new LauncherLayoutScheduleState();

        var controller = new LauncherShortcutLayoutApplyController(
            scheduleState,
            getAvailableWidth: () => 300d,
            getTileCount: () => 5,
            isCustomDragActive: () => false,
            isYieldPlaceholderDragActive: () => false,
            commandHandler: new TestCommandHandler(),
            shortcutIconsProvider: () => Enumerable.Empty<IconInfo>(),
            applyMetricsToPanels: (_, _) => panelsApplied = true,
            forcePanelLayout: () => layoutForced = true,
            refreshLayout: () => { },
            refreshSpans: () => { },
            restoreContainerState: () => containersRestored = true,
            applyMetricsToPage: (_, _, _) => { },
            canApplyPanelMetrics: () => true,
            reportError: _ => { });

        // Simulate scheduler: request advances generation, then consume provides the generation
        scheduleState.Request(false);
        scheduleState.TryConsume(out var request);

        controller.ApplyFromScheduler(request.Generation, request.AnimateRequested);

        Assert.IsTrue(panelsApplied, "Panel metrics should be applied on first layout.");
        Assert.IsTrue(layoutForced, "Panel layout should be forced after metrics apply.");
        Assert.IsTrue(containersRestored, "Container state should be restored.");
    }

    [TestMethod]
    public void ApplyFromScheduler_DoesNotApplyPanelsWhenGenerationExpired()
    {
        bool panelsApplied = false;
        var scheduleState = new LauncherLayoutScheduleState();

        var controller = new LauncherShortcutLayoutApplyController(
            scheduleState,
            getAvailableWidth: () => 300d,
            getTileCount: () => 5,
            isCustomDragActive: () => false,
            isYieldPlaceholderDragActive: () => false,
            commandHandler: new TestCommandHandler(),
            shortcutIconsProvider: () => Enumerable.Empty<IconInfo>(),
            applyMetricsToPanels: (_, _) => panelsApplied = true,
            forcePanelLayout: () => { },
            refreshLayout: () => { },
            refreshSpans: () => { },
            restoreContainerState: () => { },
            applyMetricsToPage: (_, _, _) => { },
            canApplyPanelMetrics: () => true,
            reportError: _ => { });

        // Advance the generation past what we'll pass
        scheduleState.Request(false);
        scheduleState.Request(false);

        // Apply with generation 1 (expired)
        controller.ApplyFromScheduler(1, animateLayout: false);

        Assert.IsFalse(panelsApplied, "Panel metrics should not be applied with expired generation.");
    }

    [TestMethod]
    public void ApplyFromScheduler_DoesNotAutoCompactWhenCustomDragActive()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var commandHandler = new TestCommandHandler();
        var controller = CreateController(
            scheduleState: scheduleState,
            availableWidth: 300d,
            tileCount: 5,
            isCustomDragActive: true,
            commandHandler: commandHandler);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        Assert.AreEqual(0, commandHandler.UpdateGridLayoutCallCount,
            "Auto-compact should not run during custom drag.");
    }

    [TestMethod]
    public void ApplyFromScheduler_DoesNotRestoreContainersDuringYieldPlaceholderDrag()
    {
        bool containersRestored = false;
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(
            scheduleState: scheduleState,
            availableWidth: 300d,
            tileCount: 5,
            isYieldPlaceholderDragActive: true,
            restoreContainerState: () => containersRestored = true);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        Assert.IsFalse(containersRestored,
            "Container state should not be restored during yield placeholder drag.");
    }

    [TestMethod]
    public void ApplyInitial_DoesNotUpdateSnapshot()
    {
        var controller = CreateController(availableWidth: 300d, tileCount: 5);

        controller.ApplyInitial(0d);

        Assert.IsFalse(controller.State.MetricsReady,
            "ApplyInitial should not update the layout snapshot.");
    }

    [TestMethod]
    public void ApplyInitial_UsesFallbackWidthWhenAvailableWidthIsZero()
    {
        double appliedWidth = 0d;
        var controller = CreateController(
            availableWidth: 0d,
            tileCount: 5,
            applyMetricsToPage: (_, w, _) => appliedWidth = w);

        controller.ApplyInitial(250d);

        Assert.AreEqual(250d, appliedWidth,
            "ApplyInitial should use fallback width when available width is zero.");
    }

    [TestMethod]
    public void Compact_UsesControllerColumnCount()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var commandHandler = new TestCommandHandler();
        var controller = CreateController(
            scheduleState: scheduleState,
            availableWidth: 300d,
            tileCount: 5,
            commandHandler: commandHandler);

        // First apply to set the snapshot and column count
        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);

        // Now compact with some icons
        var icon = new IconInfo();
        controller.Compact(new[] { icon });

        Assert.AreEqual(1, commandHandler.UpdateGridLayoutCallCount,
            "Compact should call UpdateGridLayout.");
    }

    [TestMethod]
    public void Compact_DoesNothingWhenMetricsNotReady()
    {
        var commandHandler = new TestCommandHandler();
        var controller = CreateController(commandHandler: commandHandler);

        var icon = new IconInfo();
        controller.Compact(new[] { icon });

        Assert.AreEqual(0, commandHandler.UpdateGridLayoutCallCount,
            "Compact should not call UpdateGridLayout when metrics are not ready.");
    }

    [TestMethod]
    public void Compact_DoesNotWriteSharedGridCoordinatesWhenPersistenceIsDisabled()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var commandHandler = new TestCommandHandler();
        var controller = CreateController(
            scheduleState: scheduleState,
            commandHandler: commandHandler,
            canPersistGridLayout: () => false);

        SimulateSchedulerTick(scheduleState, out int generation, out bool animate);
        controller.ApplyFromScheduler(generation, animate);
        controller.Compact(new[] { new IconInfo() });

        Assert.AreEqual(
            0,
            commandHandler.UpdateGridLayoutCallCount,
            "The aggregate All Groups view must not overwrite grid coordinates owned by source groups.");
    }

    [TestMethod]
    public void ResetState_ClearsLayoutState()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(scheduleState: scheduleState, availableWidth: 300d, tileCount: 5);

        SimulateSchedulerTick(scheduleState, out int gen, out bool anim);
        controller.ApplyFromScheduler(gen, anim);
        Assert.IsTrue(controller.State.MetricsReady);

        controller.ResetState();

        Assert.IsFalse(controller.State.MetricsReady);
    }

    [TestMethod]
    public void ApplyFromScheduler_PreventsReentrancy()
    {
        int applyCount = 0;
        var scheduleState = new LauncherLayoutScheduleState();

        var controller = new LauncherShortcutLayoutApplyController(
            scheduleState,
            getAvailableWidth: () => 300d,
            getTileCount: () => 5,
            isCustomDragActive: () => false,
            isYieldPlaceholderDragActive: () => false,
            commandHandler: new TestCommandHandler(),
            shortcutIconsProvider: () => Enumerable.Empty<IconInfo>(),
            applyMetricsToPanels: (_, _) => applyCount++,
            forcePanelLayout: () => { },
            refreshLayout: () => { },
            refreshSpans: () => { },
            restoreContainerState: () => { },
            applyMetricsToPage: (_, _, _) => { },
            canApplyPanelMetrics: () => true,
            reportError: _ => { });

        // First call
        SimulateSchedulerTick(scheduleState, out int gen1, out bool anim1);
        controller.ApplyFromScheduler(gen1, anim1);

        // Second call with same params - should not reapply panels (no change detected)
        SimulateSchedulerTick(scheduleState, out int gen2, out bool anim2);
        controller.ApplyFromScheduler(gen2, anim2);

        // Only the first call should have applied panel metrics
        Assert.AreEqual(1, applyCount);
    }

    [TestMethod]
    public void CurrentGeneration_ReturnsScheduleStateGeneration()
    {
        var scheduleState = new LauncherLayoutScheduleState();
        var controller = CreateController(scheduleState: scheduleState);

        Assert.AreEqual(0, controller.CurrentGeneration);

        scheduleState.Request(false);
        Assert.AreEqual(1, controller.CurrentGeneration);
    }

    /// <summary>Test shim that tracks UpdateGridLayout calls.</summary>
    private sealed class TestCommandHandler : ILauncherCommandHandler
    {
        public int UpdateGridLayoutCallCount { get; private set; }

        public LauncherCommandResult UpdateGridLayout(UpdateLauncherGridLayoutCommand command)
        {
            UpdateGridLayoutCallCount++;
            return new LauncherCommandResult(Success: true, Changed: true);
        }

        public LauncherCommandResult UpdateOrder(UpdateLauncherShortcutOrderCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult Execute(LauncherMutationCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult Add(AddLauncherShortcutCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult AddMany(AddLauncherShortcutsCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult Rename(RenameLauncherShortcutCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult Update(UpdateLauncherShortcutCommand command)
            => new(Success: true, Changed: false);

        public LauncherCommandResult MutateAllGroupsFolder(LauncherAllGroupsFolderMutationCommand command)
            => new(Success: true, Changed: false);
    }
}
