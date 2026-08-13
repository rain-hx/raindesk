using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherSingleIconDragPreviewStateTests
{
    [TestMethod]
    public void Begin_UsesSourceAsStableTarget()
    {
        var state = new LauncherSingleIconDragPreviewState();

        state.Begin(3);

        Assert.AreEqual(3, state.TargetIndex);
        Assert.IsTrue(state.TryActivateTarget(3, nowTick: 10, activationDelayMilliseconds: 150));
    }

    [TestMethod]
    public void TryActivateTarget_RequiresSameTargetForConfiguredDelay()
    {
        var state = new LauncherSingleIconDragPreviewState();
        state.Begin(1);

        Assert.IsFalse(state.TryActivateTarget(4, nowTick: 1000, activationDelayMilliseconds: 150));
        Assert.IsFalse(state.TryActivateTarget(4, nowTick: 1149, activationDelayMilliseconds: 150));
        Assert.IsTrue(state.TryActivateTarget(4, nowTick: 1150, activationDelayMilliseconds: 150));
    }

    [TestMethod]
    public void TryActivateTarget_ChangingCandidateRestartsDelay()
    {
        var state = new LauncherSingleIconDragPreviewState();
        state.Begin(1);
        state.TryActivateTarget(4, nowTick: 1000, activationDelayMilliseconds: 150);

        Assert.IsFalse(state.TryActivateTarget(5, nowTick: 1149, activationDelayMilliseconds: 150));
        Assert.IsFalse(state.TryActivateTarget(5, nowTick: 1200, activationDelayMilliseconds: 150));
        Assert.AreEqual(5, state.PendingTargetIndex);
        Assert.AreEqual(1149L, state.PendingSinceTick);
    }

    [TestMethod]
    public void CommitAndReset_ClearTransientCandidate()
    {
        var state = new LauncherSingleIconDragPreviewState();
        state.Begin(1);
        state.TryActivateTarget(4, nowTick: 1000, activationDelayMilliseconds: 150);

        state.CommitTarget(4);

        Assert.AreEqual(4, state.TargetIndex);
        Assert.AreEqual(-1, state.PendingTargetIndex);
        state.Reset();
        Assert.AreEqual(-1, state.TargetIndex);
        Assert.AreEqual(0L, state.PendingSinceTick);
    }
}
