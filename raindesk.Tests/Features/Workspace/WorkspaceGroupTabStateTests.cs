using System.Collections.ObjectModel;
using System.Collections.Specialized;
using raindesk.Features.Workspace.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabStateTests
{
    [TestMethod]
    public void Reset_KeepsAllGroupsEntryBeforeRealGroups()
    {
        var state = new WorkspaceGroupTabState();
        var first = Group("first");
        var second = Group("second");

        state.Reset(new[] { first, second });

        Assert.AreEqual(3, state.Items.Count);
        Assert.AreSame(state.AllGroupsEntry, state.Items[0]);
        Assert.AreSame(first, state.Items[1]);
        Assert.AreSame(second, state.Items[2]);
    }

    [TestMethod]
    public void AddChange_InsertsAtMenuIndexInsteadOfAppending()
    {
        var first = Group("first");
        var third = Group("third");
        var second = Group("second");
        var groups = new ObservableCollection<MenuInfo> { first, third };
        var state = new WorkspaceGroupTabState();
        state.Reset(groups);
        groups.Insert(1, second);

        state.ApplyCollectionChange(
            new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                second,
                1),
            groups);

        CollectionAssert.AreEqual(
            new[] { state.AllGroupsEntry, first, second, third },
            state.Items.ToArray());
    }

    [TestMethod]
    public void OrderedIds_ExcludesSyntheticAllGroupsEntry()
    {
        var state = new WorkspaceGroupTabState();
        state.Reset(new[] { Group("one"), Group("two") });

        IReadOnlyList<string> ids = state.GetOrderedGroupIds();

        CollectionAssert.AreEqual(new[] { "one", "two" }, ids.ToArray());
    }

    [TestMethod]
    public void SuppressedSourceChange_DoesNotApplyTheSameRemovalTwice()
    {
        var first = Group("first");
        var second = Group("second");
        var groups = new ObservableCollection<MenuInfo> { first, second };
        var state = new WorkspaceGroupTabState();
        state.Reset(groups);
        state.Remove(first);
        var change = new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            first,
            0);

        using (state.SuppressSourceSynchronization())
        {
            state.ApplyCollectionChange(change, groups);
            Assert.IsTrue(state.IsSourceSynchronizationSuppressed);
        }

        CollectionAssert.AreEqual(
            new[] { state.AllGroupsEntry, second },
            state.Items.ToArray());
        Assert.IsFalse(state.IsSourceSynchronizationSuppressed);
    }

    [TestMethod]
    public void SuppressionScopes_AreNestedAndIdempotent()
    {
        var state = new WorkspaceGroupTabState();
        IDisposable first = state.SuppressSourceSynchronization();
        IDisposable second = state.SuppressSourceSynchronization();

        first.Dispose();
        Assert.IsTrue(state.IsSourceSynchronizationSuppressed);
        first.Dispose();
        Assert.IsTrue(state.IsSourceSynchronizationSuppressed);
        second.Dispose();
        Assert.IsFalse(state.IsSourceSynchronizationSuppressed);
    }

    private static MenuInfo Group(string id) => new()
    {
        MenuId = id,
        MenuName = id
    };
}
