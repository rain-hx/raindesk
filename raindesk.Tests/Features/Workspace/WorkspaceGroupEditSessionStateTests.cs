using raindesk.Features.Workspace.Presentation;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupEditSessionStateTests
{
    [TestMethod]
    public void Begin_SuppressesLostFocusUntilEditorSettles()
    {
        var state = new WorkspaceGroupEditSessionState<object>();
        var target = new object();

        state.Begin(target, "Original");

        Assert.IsTrue(state.HasSession);
        Assert.IsTrue(state.SuppressLostFocus);
        Assert.AreEqual("Original", state.OriginalName);
        Assert.AreSame(target, state.Target);
    }

    [TestMethod]
    public void FocusCanBeEstablishedOnlyOncePerSession()
    {
        var state = new WorkspaceGroupEditSessionState<object>();
        state.Begin(new object(), "Name");

        Assert.IsTrue(state.TryEstablishFocus());
        Assert.IsFalse(state.TryEstablishFocus());
    }

    [TestMethod]
    public void OutsidePointerCommitsOnlyAfterSuppressionEndsAndPointerIsOutside()
    {
        var state = new WorkspaceGroupEditSessionState<object>();
        state.Begin(new object(), "Name");

        Assert.IsFalse(state.ShouldCommitOutside(pointerInsideEditor: false));
        state.AllowLostFocus();
        Assert.IsFalse(state.ShouldCommitOutside(pointerInsideEditor: true));
        Assert.IsTrue(state.ShouldCommitOutside(pointerInsideEditor: false));
    }

    [TestMethod]
    public void LostFocusRequiresActiveTarget()
    {
        var state = new WorkspaceGroupEditSessionState<object>();
        var target = new object();
        state.Begin(target, "Name");
        state.AllowLostFocus();

        Assert.IsTrue(state.ShouldCommitLostFocus(target));
        Assert.IsFalse(state.ShouldCommitLostFocus(new object()));
    }

    [TestMethod]
    public void Complete_ReturnsRollbackDataAndClearsSession()
    {
        var state = new WorkspaceGroupEditSessionState<object>();
        var target = new object();
        state.Begin(target, "Original");

        WorkspaceGroupEditCompletion<object> completion = state.Complete();

        Assert.AreSame(target, completion.Target);
        Assert.AreEqual("Original", completion.OriginalName);
        Assert.IsFalse(state.HasSession);
        Assert.IsFalse(state.SuppressLostFocus);
    }
}
