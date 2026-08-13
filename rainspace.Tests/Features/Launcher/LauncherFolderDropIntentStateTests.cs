using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherFolderDropIntentStateTests
{
    [TestMethod]
    public void EvaluateCandidate_RequiresStableCandidateForActivationDelay()
    {
        var state = new LauncherFolderDropIntentState<object>();
        var target = new object();

        var pending = state.EvaluateCandidate(target, true, nowTick: 1000, activationDelayMilliseconds: 300);
        var stillPending = state.EvaluateCandidate(target, true, nowTick: 1299, activationDelayMilliseconds: 300);
        var active = state.EvaluateCandidate(target, true, nowTick: 1300, activationDelayMilliseconds: 300);

        Assert.IsTrue(pending.IsPending);
        Assert.IsTrue(stillPending.IsPending);
        Assert.AreSame(target, active.ActiveTarget);
        Assert.AreSame(target, state.LastValidTarget);
        Assert.IsNull(state.PendingTarget);
    }

    [TestMethod]
    public void EvaluateCandidate_ChangingTargetRestartsDelayAndClearsPreviousActive()
    {
        var state = new LauncherFolderDropIntentState<object>();
        var first = new object();
        var second = new object();
        state.SetActive(first, true);

        var update = state.EvaluateCandidate(second, true, nowTick: 2000, activationDelayMilliseconds: 300);

        Assert.IsTrue(update.ActiveTargetChanged);
        Assert.IsNull(update.ActiveTarget);
        Assert.AreSame(second, state.PendingTarget);
        Assert.AreSame(first, state.LastValidTarget);
    }

    [TestMethod]
    public void InvalidCandidate_ClearsActiveAndPendingButKeepsLastValidTarget()
    {
        var state = new LauncherFolderDropIntentState<object>();
        var target = new object();
        state.SetActive(target, true);

        var update = state.EvaluateCandidate(null, false, nowTick: 10, activationDelayMilliseconds: 300);

        Assert.IsTrue(update.ActiveTargetChanged);
        Assert.IsNull(state.ActiveTarget);
        Assert.IsNull(state.PendingTarget);
        Assert.AreSame(target, state.LastValidTarget);
    }

    [TestMethod]
    public void Reset_ClearsEntireDragSession()
    {
        var state = new LauncherFolderDropIntentState<object>();
        state.SetActive(new object(), true);

        state.Reset();

        Assert.IsNull(state.ActiveTarget);
        Assert.IsNull(state.LastValidTarget);
        Assert.IsNull(state.PendingTarget);
        Assert.AreEqual(0L, state.PendingSinceTick);
    }
}
