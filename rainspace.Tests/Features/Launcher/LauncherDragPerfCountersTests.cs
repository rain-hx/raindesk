using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests;

/// <summary>
/// P0: 拖拽性能计数器行为测试。
/// </summary>
[TestClass]
public class LauncherDragPerfCountersTests
{
    [TestMethod]
    public void Counters_BeginSession_ResetsAllValues()
    {
        var counters = new LauncherDragPerfCounters();
        counters.RecordInputEvent();
        counters.RecordFrameUpdate();
        counters.RecordHitTest();
        counters.BeginSession();

        var summary = counters.GetSummary();
        Assert.AreEqual(0L, summary.InputEventCount);
        Assert.AreEqual(0L, summary.FrameUpdateCount);
        Assert.AreEqual(0L, summary.HitTestCount);
        Assert.AreEqual(0L, summary.UpdateLayoutCount);
        Assert.AreEqual(0L, summary.CollectionMoveCount);
        Assert.AreEqual(0L, summary.CollectionResetCount);
    }

    [TestMethod]
    public void Counters_RecordInputEvent_IncrementsCount()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordInputEvent();
        counters.RecordInputEvent();
        counters.RecordInputEvent();

        var summary = counters.GetSummary();
        Assert.AreEqual(3L, summary.InputEventCount);
    }

    [TestMethod]
    public void Counters_RecordFrameUpdate_IncrementsCount()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordFrameUpdate();
        counters.RecordFrameUpdate();

        var summary = counters.GetSummary();
        Assert.AreEqual(2L, summary.FrameUpdateCount);
    }

    [TestMethod]
    public void Counters_RecordUpdateLayout_IncrementsCount()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordUpdateLayout();
        counters.RecordUpdateLayout();
        counters.RecordUpdateLayout();

        var summary = counters.GetSummary();
        Assert.AreEqual(3L, summary.UpdateLayoutCount);
    }

    [TestMethod]
    public void Counters_RecordCollectionMove_Reset_TrackSeparately()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordCollectionMove();
        counters.RecordCollectionMove();
        counters.RecordCollectionReset();

        var summary = counters.GetSummary();
        Assert.AreEqual(2L, summary.CollectionMoveCount);
        Assert.AreEqual(1L, summary.CollectionResetCount);
    }

    [TestMethod]
    public void Counters_HasHitTestThisFrame_TracksPerFrame()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        Assert.IsFalse(counters.HasHitTestThisFrame);

        counters.RecordHitTest();
        Assert.IsTrue(counters.HasHitTestThisFrame);

        // 新帧重置
        counters.RecordFrameUpdate();
        Assert.IsFalse(counters.HasHitTestThisFrame);
    }

    [TestMethod]
    public void Counters_RecordPointerToYield_ComputesAverage()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordPointerToYieldMs(40);
        counters.RecordPointerToYieldMs(60);
        counters.RecordPointerToYieldMs(80);

        var summary = counters.GetSummary();
        Assert.AreEqual(60d, summary.PointerToYieldAvgMs, 0.01d);
    }

    [TestMethod]
    public void Counters_RecordDropToLayout_ComputesAverage()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordDropToLayoutMs(20);
        counters.RecordDropToLayoutMs(30);

        var summary = counters.GetSummary();
        Assert.AreEqual(25d, summary.DropToLayoutAvgMs, 0.01d);
    }

    [TestMethod]
    public void Counters_FormatSummary_ProducesNonEmptyString()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();
        counters.RecordInputEvent();
        counters.RecordFrameUpdate();
        counters.RecordHitTest();

        string result = counters.FormatSummary();
        Assert.IsTrue(result.Contains("[DragPerf]"), "摘要应包含 [DragPerf] 前缀");
        Assert.IsTrue(result.Contains("input=1"), "摘要应包含 input 计数");
    }

    [TestMethod]
    public void Counters_EndSession_StopsRecording()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();
        counters.RecordInputEvent();
        counters.EndSession();

        // 结束后不应记录
        counters.RecordInputEvent();
        var summary = counters.GetSummary();
        Assert.AreEqual(1L, summary.InputEventCount);
    }

    [TestMethod]
    public void Counters_NegativeTimingValues_AreIgnored()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordPointerToYieldMs(-5);
        counters.RecordDropToLayoutMs(-10);

        var summary = counters.GetSummary();
        Assert.AreEqual(0d, summary.PointerToYieldAvgMs);
        Assert.AreEqual(0d, summary.DropToLayoutAvgMs);
    }

    [TestMethod]
    public void Counters_StoryboardCreate_Increments()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordStoryboardCreate();
        counters.RecordStoryboardCreate();

        var summary = counters.GetSummary();
        Assert.AreEqual(2L, summary.StoryboardCreateCount);
    }

    [TestMethod]
    public void Counters_LogCall_Increments()
    {
        var counters = new LauncherDragPerfCounters();
        counters.BeginSession();

        counters.RecordLogCall();
        counters.RecordLogCall();
        counters.RecordLogCall();

        var summary = counters.GetSummary();
        Assert.AreEqual(3L, summary.LogCallCount);
    }
}
