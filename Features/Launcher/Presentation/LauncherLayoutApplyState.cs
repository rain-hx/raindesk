using System;
using raindesk.Features.Launcher.Application;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// Holds the last-applied layout snapshot and provides pure decision logic
/// for whether a layout apply or auto-compact is needed.
/// </summary>
internal sealed class LauncherLayoutApplyState
{
    private static readonly ShortcutLayoutSnapshot SentinelSnapshot = new(
        AvailableWidth: double.NaN,
        TileCount: -1,
        ColumnCount: -1,
        SlotWidth: double.NaN,
        CardWidth: double.NaN,
        GapWidth: double.NaN,
        HorizontalOffset: double.NaN,
        SlotHeight: double.NaN);

    private ShortcutLayoutSnapshot _lastApplied = SentinelSnapshot;
    private bool _metricsReady;

    /// <summary>Snapshot of the most recently applied layout values.</summary>
    public ShortcutLayoutSnapshot LastApplied => _lastApplied;

    /// <summary>Whether metrics have been applied at least once.</summary>
    public bool MetricsReady => _metricsReady;

    /// <summary>Whether the next apply should be treated as the first ready layout.</summary>
    public bool IsFirstReadyLayout => !_metricsReady;

    /// <summary>
    /// Pure decision: does the new metrics + counts represent a meaningful change
    /// from the last applied snapshot?
    /// </summary>
    public bool ShouldApply(
        ShortcutLayoutMetrics metrics,
        double availableWidth,
        int tileCount)
    {
        return ShortcutLayoutCalculator.HasMeaningfulLayoutChange(
            metrics,
            _lastApplied,
            availableWidth,
            tileCount);
    }

    /// <summary>
    /// Pure decision: should auto-compaction run?
    /// True when this is the first ready layout or the column count changed.
    /// </summary>
    public bool ShouldAutoCompact(int previousColumnCount, int currentColumnCount)
    {
        return !_metricsReady || previousColumnCount != currentColumnCount;
    }

    /// <summary>
    /// Update the stored snapshot and mark metrics as ready.
    /// Call this after a successful layout apply.
    /// </summary>
    public void UpdateSnapshot(
        ShortcutLayoutMetrics metrics,
        double availableWidth,
        int tileCount)
    {
        _lastApplied = new ShortcutLayoutSnapshot(
            availableWidth,
            tileCount,
            metrics.ColumnCount,
            metrics.SlotWidth,
            metrics.CardWidth,
            metrics.GapWidth,
            metrics.HorizontalOffset,
            metrics.SlotHeight);
        _metricsReady = true;
    }

    /// <summary>
    /// Reset all state. Use when the control is unloaded or data is replaced.
    /// </summary>
    public void Reset()
    {
        _lastApplied = SentinelSnapshot;
        _metricsReady = false;
    }
}
