using System;
using System.Collections.Generic;
using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Contracts;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Orchestrates the full shortcut layout application flow:
/// scheduling, change detection, metrics projection, panel refresh,
/// auto-compaction, and transition/container restoration.
/// </summary>
internal sealed class LauncherShortcutLayoutApplyController : IDisposable
{
    private readonly LauncherLayoutApplyState _state = new();
    private readonly LauncherLayoutScheduleState _scheduleState;
    private readonly LauncherShortcutLayoutScheduler? _scheduler;
    private readonly Func<double> _getAvailableWidth;
    private readonly Func<int> _getTileCount;
    private readonly Func<bool> _isCustomDragActive;
    private readonly Func<bool> _isYieldPlaceholderDragActive;
    private readonly ILauncherCommandHandler _commandHandler;
    private readonly Func<IEnumerable<IconInfo>> _shortcutIconsProvider;
    private readonly Action<ShortcutLayoutMetrics, bool> _applyMetricsToPanels;
    private readonly Action _forcePanelLayout;
    private readonly Action _refreshLayout;
    private readonly Action _refreshSpans;
    private readonly Action _restoreContainerState;
    private readonly Action<ShortcutLayoutMetrics, double, int> _applyMetricsToPage;
    private readonly Func<bool> _canApplyPanelMetrics;
    private readonly Func<bool> _canPersistGridLayout;
    private readonly Action<Exception> _reportError;
    private bool _isApplyingLayout;

    public LauncherShortcutLayoutApplyController(
        LauncherShortcutLayoutScheduler scheduler,
        Func<double> getAvailableWidth,
        Func<int> getTileCount,
        Func<bool> isCustomDragActive,
        Func<bool> isYieldPlaceholderDragActive,
        ILauncherCommandHandler commandHandler,
        Func<IEnumerable<IconInfo>> shortcutIconsProvider,
        Action<ShortcutLayoutMetrics, bool> applyMetricsToPanels,
        Action forcePanelLayout,
        Action refreshLayout,
        Action refreshSpans,
        Action restoreContainerState,
        Action<ShortcutLayoutMetrics, double, int> applyMetricsToPage,
        Func<bool> canApplyPanelMetrics,
        Action<Exception> reportError,
        Func<bool>? canPersistGridLayout = null)
        : this(
              scheduler?.ScheduleState ?? throw new ArgumentNullException(nameof(scheduler)),
              scheduler,
              getAvailableWidth,
              getTileCount,
              isCustomDragActive,
              isYieldPlaceholderDragActive,
              commandHandler,
              shortcutIconsProvider,
              applyMetricsToPanels,
              forcePanelLayout,
              refreshLayout,
              refreshSpans,
              restoreContainerState,
              applyMetricsToPage,
              canApplyPanelMetrics,
              reportError,
              canPersistGridLayout)
    {
    }

    internal LauncherShortcutLayoutApplyController(
        LauncherLayoutScheduleState scheduleState,
        Func<double> getAvailableWidth,
        Func<int> getTileCount,
        Func<bool> isCustomDragActive,
        Func<bool> isYieldPlaceholderDragActive,
        ILauncherCommandHandler commandHandler,
        Func<IEnumerable<IconInfo>> shortcutIconsProvider,
        Action<ShortcutLayoutMetrics, bool> applyMetricsToPanels,
        Action forcePanelLayout,
        Action refreshLayout,
        Action refreshSpans,
        Action restoreContainerState,
        Action<ShortcutLayoutMetrics, double, int> applyMetricsToPage,
        Func<bool> canApplyPanelMetrics,
        Action<Exception> reportError,
        Func<bool>? canPersistGridLayout = null)
        : this(
              scheduleState,
              scheduler: null,
              getAvailableWidth,
              getTileCount,
              isCustomDragActive,
              isYieldPlaceholderDragActive,
              commandHandler,
              shortcutIconsProvider,
              applyMetricsToPanels,
              forcePanelLayout,
              refreshLayout,
              refreshSpans,
              restoreContainerState,
              applyMetricsToPage,
              canApplyPanelMetrics,
              reportError,
              canPersistGridLayout)
    {
    }

    private LauncherShortcutLayoutApplyController(
        LauncherLayoutScheduleState scheduleState,
        LauncherShortcutLayoutScheduler? scheduler,
        Func<double> getAvailableWidth,
        Func<int> getTileCount,
        Func<bool> isCustomDragActive,
        Func<bool> isYieldPlaceholderDragActive,
        ILauncherCommandHandler commandHandler,
        Func<IEnumerable<IconInfo>> shortcutIconsProvider,
        Action<ShortcutLayoutMetrics, bool> applyMetricsToPanels,
        Action forcePanelLayout,
        Action refreshLayout,
        Action refreshSpans,
        Action restoreContainerState,
        Action<ShortcutLayoutMetrics, double, int> applyMetricsToPage,
        Func<bool> canApplyPanelMetrics,
        Action<Exception> reportError,
        Func<bool>? canPersistGridLayout)
    {
        _scheduleState = scheduleState ?? throw new ArgumentNullException(nameof(scheduleState));
        _scheduler = scheduler;
        _getAvailableWidth = getAvailableWidth ?? throw new ArgumentNullException(nameof(getAvailableWidth));
        _getTileCount = getTileCount ?? throw new ArgumentNullException(nameof(getTileCount));
        _isCustomDragActive = isCustomDragActive ?? throw new ArgumentNullException(nameof(isCustomDragActive));
        _isYieldPlaceholderDragActive = isYieldPlaceholderDragActive ?? throw new ArgumentNullException(nameof(isYieldPlaceholderDragActive));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _shortcutIconsProvider = shortcutIconsProvider ?? throw new ArgumentNullException(nameof(shortcutIconsProvider));
        _applyMetricsToPanels = applyMetricsToPanels ?? throw new ArgumentNullException(nameof(applyMetricsToPanels));
        _forcePanelLayout = forcePanelLayout ?? throw new ArgumentNullException(nameof(forcePanelLayout));
        _refreshLayout = refreshLayout ?? throw new ArgumentNullException(nameof(refreshLayout));
        _refreshSpans = refreshSpans ?? throw new ArgumentNullException(nameof(refreshSpans));
        _restoreContainerState = restoreContainerState ?? throw new ArgumentNullException(nameof(restoreContainerState));
        _applyMetricsToPage = applyMetricsToPage ?? throw new ArgumentNullException(nameof(applyMetricsToPage));
        _canApplyPanelMetrics = canApplyPanelMetrics ?? throw new ArgumentNullException(nameof(canApplyPanelMetrics));
        _canPersistGridLayout = canPersistGridLayout ?? (() => true);
        _reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
    }

    /// <summary>Expose the scheduler's current generation for callers that need it.</summary>
    public int CurrentGeneration => _scheduleState.Generation;

    /// <summary>Expose the layout state for testing.</summary>
    internal LauncherLayoutApplyState State => _state;

    /// <summary>Schedule a layout update (coalesced via 16ms timer).</summary>
    public void Schedule(bool animate = false)
    {
        _scheduler?.Schedule(animate);
    }

    /// <summary>Schedule a non-animated layout update from window resize.</summary>
    public void ScheduleFromWindow()
    {
        _scheduler?.Schedule(animate: false);
    }

    /// <summary>
    /// Full layout application flow, called by the scheduler callback.
    /// Encapsulates change detection, metrics projection, auto-compaction,
    /// panel refresh, and container restoration.
    /// </summary>
    public void ApplyFromScheduler(int layoutGeneration, bool animateLayout)
    {
        if (_isApplyingLayout) return;
        _isApplyingLayout = true;
        try
        {
            ApplyCore(layoutGeneration, animateLayout);
        }
        finally
        {
            _isApplyingLayout = false;
        }
    }

    /// <summary>
    /// Apply layout without change detection (for initial layout before metrics are ready).
    /// Always computes and projects metrics, skipping the ShouldApply gate.
    /// Does not update the layout snapshot so the first real scheduler apply
    /// can still detect the initial change.
    /// </summary>
    public void ApplyInitial(double fallbackWidth)
    {
        double availableWidth = _getAvailableWidth();
        if (availableWidth <= 0d && fallbackWidth > 0d)
        {
            availableWidth = fallbackWidth;
        }

        if (availableWidth <= 0d) return;

        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        _applyMetricsToPage(metrics, availableWidth, _getTileCount());
    }

    /// <summary>
    /// Apply the current computed metrics to a specific ListView
    /// (e.g., cross-group preview overlay).
    /// </summary>
    public void ApplyToListView(Microsoft.UI.Xaml.Controls.ListView? listView)
    {
        if (listView == null) return;

        double availableWidth = _getAvailableWidth();
        if (availableWidth <= 0d) return;

        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        LauncherShortcutPanelMetricsAdapter.ApplyAndRefresh(listView, metrics);
    }

    /// <summary>Refresh the layout of the icon list panel.</summary>
    public void RefreshLayout() => _refreshLayout();

    /// <summary>Refresh shortcut item spans.</summary>
    public void RefreshSpans() => _refreshSpans();

    /// <summary>Reset all layout state (on control unload or data replacement).</summary>
    public void ResetState()
    {
        _state.Reset();
        _isApplyingLayout = false;
    }

    /// <summary>
    /// Run grid compaction on the given icons using the current column count.
    /// Used for explicit compaction calls outside the normal layout apply flow.
    /// </summary>
    public void Compact(IEnumerable<IconInfo> icons, bool preserveExistingVisualOrder = true)
    {
        if (!_state.MetricsReady || !_canPersistGridLayout())
        {
            return;
        }

        int columnCount = Math.Max(1, _state.LastApplied.ColumnCount);

        IReadOnlyList<LauncherGridPlacement> placements = LauncherGridOrganizer.Plan(
            icons,
            columnCount,
            preserveExistingVisualOrder);
        if (placements.Count == 0)
        {
            return;
        }

        LauncherCommandResult result = _commandHandler.UpdateGridLayout(
            new UpdateLauncherGridLayoutCommand(placements));
        if (!result.Success)
        {
            _reportError(new InvalidOperationException(result.Error ?? "Launcher 网格布局更新失败。"));
            return;
        }

        if (result.Changed)
        {
            _forcePanelLayout();
        }
    }

    public void Dispose()
    {
        _scheduler?.Dispose();
    }

    private void ApplyCore(int layoutGeneration, bool animateLayout)
    {
        double availableWidth = _getAvailableWidth();
        if (availableWidth <= 0d) return;

        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        int tileCount = _getTileCount();

        if (!_state.ShouldApply(metrics, availableWidth, tileCount))
        {
            _refreshSpans();
            return;
        }

        int previousColumnCount = _state.LastApplied.ColumnCount;
        _state.UpdateSnapshot(metrics, availableWidth, tileCount);
        _applyMetricsToPage(metrics, availableWidth, tileCount);

        if (_state.ShouldAutoCompact(previousColumnCount, metrics.ColumnCount)
            && !_isCustomDragActive()
            && _canPersistGridLayout())
        {
            Compact(_shortcutIconsProvider(), preserveExistingVisualOrder: true);
        }

        if (!_scheduleState.IsCurrent(layoutGeneration) || !_canApplyPanelMetrics())
        {
            return;
        }

        _applyMetricsToPanels(metrics, animateLayout);
        _forcePanelLayout();
        if (!_isYieldPlaceholderDragActive())
        {
            _restoreContainerState();
        }
    }
}
