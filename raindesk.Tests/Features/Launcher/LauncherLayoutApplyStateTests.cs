using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherLayoutApplyStateTests
{
    [TestMethod]
    public void ShouldApply_ReturnsTrueWhenNoSnapshotApplied()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);

        bool result = state.ShouldApply(metrics, 300d, 5);

        Assert.IsTrue(result, "First apply with sentinel snapshot should always be meaningful.");
    }

    [TestMethod]
    public void ShouldApply_ReturnsFalseWhenMetricsIdentical()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldApply(metrics, 300d, 5);

        Assert.IsFalse(result, "Identical metrics and tile count should not be a meaningful change.");
    }

    [TestMethod]
    public void ShouldApply_ReturnsTrueWhenColumnCountChanges()
    {
        var state = new LauncherLayoutApplyState();
        var initial = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(initial, 300d, 5);

        var updated = new ShortcutLayoutMetrics(4, 75d, 104d, 88d, 12d);
        bool result = state.ShouldApply(updated, 300d, 5);

        Assert.IsTrue(result, "Column count change should be meaningful.");
    }

    [TestMethod]
    public void ShouldApply_ReturnsTrueWhenTileCountChanges()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldApply(metrics, 300d, 8);

        Assert.IsTrue(result, "Tile count change should be meaningful.");
    }

    [TestMethod]
    public void ShouldApply_ReturnsTrueWhenWidthChangesSignificantly()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldApply(metrics, 350d, 5);

        Assert.IsTrue(result, "Width change >= 1px should be meaningful.");
    }

    [TestMethod]
    public void ShouldApply_ReturnsFalseForSubPixelWidthChange()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldApply(metrics, 300.5d, 5);

        Assert.IsFalse(result, "Sub-pixel width change < 1px should not be meaningful.");
    }

    [TestMethod]
    public void ShouldAutoCompact_ReturnsTrueOnFirstReadyLayout()
    {
        var state = new LauncherLayoutApplyState();

        // Before UpdateSnapshot, MetricsReady is false
        bool result = state.ShouldAutoCompact(3, 3);

        Assert.IsTrue(result, "First ready layout should trigger auto-compact.");
    }

    [TestMethod]
    public void ShouldAutoCompact_ReturnsTrueWhenColumnCountChanges()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldAutoCompact(3, 4);

        Assert.IsTrue(result, "Column count change should trigger auto-compact.");
    }

    [TestMethod]
    public void ShouldAutoCompact_ReturnsFalseWhenColumnCountUnchanged()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        bool result = state.ShouldAutoCompact(3, 3);

        Assert.IsFalse(result, "Same column count after first apply should not trigger auto-compact.");
    }

    [TestMethod]
    public void UpdateSnapshot_StoresValuesCorrectly()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(4, 75d, 104d, 88d, 12d);

        state.UpdateSnapshot(metrics, 300d, 10);

        Assert.IsTrue(state.MetricsReady);
        Assert.AreEqual(300d, state.LastApplied.AvailableWidth);
        Assert.AreEqual(10, state.LastApplied.TileCount);
        Assert.AreEqual(4, state.LastApplied.ColumnCount);
        Assert.AreEqual(75d, state.LastApplied.SlotWidth);
        Assert.AreEqual(88d, state.LastApplied.CardWidth);
        Assert.AreEqual(12d, state.LastApplied.GapWidth);
    }

    [TestMethod]
    public void Reset_ClearsAllState()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        state.Reset();

        Assert.IsFalse(state.MetricsReady);
        Assert.IsTrue(state.ShouldApply(metrics, 300d, 5));
        Assert.IsTrue(state.ShouldAutoCompact(3, 3));
    }

    [TestMethod]
    public void IsFirstReadyLayout_ReturnsTrueBeforeFirstUpdate()
    {
        var state = new LauncherLayoutApplyState();

        Assert.IsTrue(state.IsFirstReadyLayout);
    }

    [TestMethod]
    public void IsFirstReadyLayout_ReturnsFalseAfterUpdate()
    {
        var state = new LauncherLayoutApplyState();
        var metrics = new ShortcutLayoutMetrics(3, 100d, 104d, 88d, 12d);
        state.UpdateSnapshot(metrics, 300d, 5);

        Assert.IsFalse(state.IsFirstReadyLayout);
    }
}
