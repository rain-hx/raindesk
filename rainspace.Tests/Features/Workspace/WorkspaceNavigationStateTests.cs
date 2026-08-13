using rainspace.Features.Workspace.Presentation;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceNavigationStateTests
{
    [TestMethod]
    public void SelectedGroup_FollowsStableIdWhenGroupsAreReordered()
    {
        var state = new WorkspaceNavigationState();
        Assert.IsTrue(state.TrySelect("second", new[] { "first", "second" }));

        int selectedIndex = state.GetSelectedIndex(new[] { "second", "first" });

        Assert.IsFalse(state.IsAllGroups);
        Assert.AreEqual("second", state.SelectedGroupId);
        Assert.AreEqual(0, selectedIndex);
    }

    [TestMethod]
    public void Reconcile_SelectedGroupRemoved_FallsBackToAllGroups()
    {
        var state = new WorkspaceNavigationState();
        state.TrySelect("selected", new[] { "selected", "other" });

        bool changed = state.Reconcile(new[] { "other" });

        Assert.IsTrue(changed);
        Assert.IsTrue(state.IsAllGroups);
        Assert.AreEqual(-1, state.GetSelectedIndex(new[] { "other" }));
    }

    [TestMethod]
    public void RestoreFromPersistedIndex_InvalidIndex_SelectsAllGroups()
    {
        var state = new WorkspaceNavigationState();
        state.TrySelect("first", new[] { "first" });

        bool restored = state.RestoreFromPersistedIndex(3, new[] { "first" });

        Assert.IsFalse(restored);
        Assert.IsTrue(state.IsAllGroups);
    }

    [TestMethod]
    public void TrySelect_UnknownGroup_PreservesCurrentSelection()
    {
        var state = new WorkspaceNavigationState();
        state.TrySelect("first", new[] { "first" });

        bool selected = state.TrySelect("missing", new[] { "first" });

        Assert.IsFalse(selected);
        Assert.AreEqual("first", state.SelectedGroupId);
    }

    [TestMethod]
    public void Snapshot_RestoresStableSelectionAfterTemporaryNavigation()
    {
        var state = new WorkspaceNavigationState();
        state.TrySelect("second", new[] { "first", "second" });
        WorkspaceNavigationSnapshot snapshot = state.Capture();
        state.SelectAll();

        bool restored = state.Restore(snapshot, new[] { "second", "first" });

        Assert.IsTrue(restored);
        Assert.AreEqual("second", state.SelectedGroupId);
        Assert.AreEqual(0, state.GetSelectedIndex(new[] { "second", "first" }));
    }

    [TestMethod]
    public void Snapshot_MissingGroup_FallsBackToAllGroups()
    {
        var state = new WorkspaceNavigationState();
        state.TrySelect("removed", new[] { "removed" });
        WorkspaceNavigationSnapshot snapshot = state.Capture();

        bool restored = state.Restore(snapshot, Array.Empty<string>());

        Assert.IsFalse(restored);
        Assert.IsTrue(state.IsAllGroups);
    }
}
