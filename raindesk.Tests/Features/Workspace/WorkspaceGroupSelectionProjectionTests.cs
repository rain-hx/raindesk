using raindesk.Features.Workspace.Presentation;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupSelectionProjectionTests
{
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 1)]
    [DataRow(3, 4)]
    public void ToTabIndex_AccountsForSyntheticAllGroupsEntry(
        int selectedGroupIndex,
        int expectedTabIndex)
    {
        Assert.AreEqual(
            expectedTabIndex,
            WorkspaceGroupSelectionProjection.ToTabIndex(selectedGroupIndex));
    }

    [TestMethod]
    public void Apply_ProjectsSelectionToAllGroupsOrOneRealGroup()
    {
        var all = new SelectionItem();
        var groups = new[] { new SelectionItem(), new SelectionItem() };

        WorkspaceGroupSelectionProjection.Apply(
            all,
            groups,
            1,
            static (item, selected) => item.IsSelected = selected);

        Assert.IsFalse(all.IsSelected);
        Assert.IsFalse(groups[0].IsSelected);
        Assert.IsTrue(groups[1].IsSelected);

        WorkspaceGroupSelectionProjection.Apply(
            all,
            groups,
            -1,
            static (item, selected) => item.IsSelected = selected);

        Assert.IsTrue(all.IsSelected);
        Assert.IsFalse(groups[0].IsSelected);
        Assert.IsFalse(groups[1].IsSelected);
    }

    [TestMethod]
    public void ResolveTab_ReturnsProjectedTabAndRejectsOutOfRangeIndex()
    {
        var all = new SelectionItem();
        var first = new SelectionItem();
        var tabs = new[] { all, first };

        Assert.AreSame(all, WorkspaceGroupSelectionProjection.ResolveTab(tabs, -1));
        Assert.AreSame(first, WorkspaceGroupSelectionProjection.ResolveTab(tabs, 0));
        Assert.IsNull(WorkspaceGroupSelectionProjection.ResolveTab(tabs, 2));
    }

    [TestMethod]
    public void IsSelectedTab_UsesIdentityForAllAndActiveGroup()
    {
        var all = new SelectionItem();
        var active = new SelectionItem();

        Assert.IsTrue(WorkspaceGroupSelectionProjection.IsSelectedTab(all, all, active, true));
        Assert.IsTrue(WorkspaceGroupSelectionProjection.IsSelectedTab(active, all, active, false));
        Assert.IsFalse(WorkspaceGroupSelectionProjection.IsSelectedTab(new SelectionItem(), all, active, false));
    }

    private sealed class SelectionItem
    {
        public bool IsSelected { get; set; }
    }
}
