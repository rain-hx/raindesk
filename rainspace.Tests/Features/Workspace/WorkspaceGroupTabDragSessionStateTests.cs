using rainspace.Features.Workspace.Presentation;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabDragSessionStateTests
{
    [TestMethod]
    public void TryActivate_RequiresPointerDistanceThreshold()
    {
        var state = new WorkspaceGroupTabDragSessionState<object>();
        state.Begin(new object(), 2, startX: 10d, startY: 10d);

        WorkspaceGroupTabDragActivation pending = state.TryActivate(14d, 14d, 8d);
        WorkspaceGroupTabDragActivation active = state.TryActivate(20d, 10d, 8d);

        Assert.IsFalse(pending.IsActive);
        Assert.IsTrue(active.IsActive);
        Assert.IsTrue(active.ActivatedNow);
    }

    [TestMethod]
    public void Complete_MoveAwayFromOriginSuppressesExactlyOneClick()
    {
        var state = new WorkspaceGroupTabDragSessionState<object>();
        state.Begin(new object(), 2, 0d, 0d);
        state.TryActivate(20d, 0d, 8d);
        state.CommitLiveMove(3);

        WorkspaceGroupTabDragCompletion completion = state.Complete();

        Assert.IsTrue(completion.Moved);
        Assert.AreEqual(3, completion.FinalIndex);
        Assert.IsTrue(state.ConsumeSuppressedClick());
        Assert.IsFalse(state.ConsumeSuppressedClick());
    }

    [TestMethod]
    public void Complete_MoveBackToOriginDoesNotCommitOrSuppressClick()
    {
        var state = new WorkspaceGroupTabDragSessionState<object>();
        state.Begin(new object(), 2, 0d, 0d);
        state.CommitLiveMove(3);
        state.CommitLiveMove(2);

        WorkspaceGroupTabDragCompletion completion = state.Complete();

        Assert.IsFalse(completion.Moved);
        Assert.AreEqual(2, completion.FinalIndex);
        Assert.IsFalse(state.ConsumeSuppressedClick());
    }

    [TestMethod]
    public void SuspendAndRestoreReorder_PreservesPriorSettingAndAppliesCurrentPolicy()
    {
        var state = new WorkspaceGroupTabDragSessionState<object>();
        state.SetReorderEnabled(true);
        state.SuspendReorder();

        Assert.IsFalse(state.IsReorderEnabled);

        state.RestoreReorder(allowed: true);
        Assert.IsTrue(state.IsReorderEnabled);

        state.SuspendReorder();
        state.RestoreReorder(allowed: false);
        Assert.IsFalse(state.IsReorderEnabled);
    }

    [TestMethod]
    public void TrySetPendingTarget_ReportsOnlyChanges()
    {
        var state = new WorkspaceGroupTabDragSessionState<object>();
        state.Begin(new object(), 2, 0d, 0d);

        Assert.IsFalse(state.TrySetPendingTarget(2));
        Assert.IsTrue(state.TrySetPendingTarget(3));
        Assert.IsFalse(state.TrySetPendingTarget(3));
    }
}
