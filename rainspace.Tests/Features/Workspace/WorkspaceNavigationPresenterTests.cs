using System.Collections.ObjectModel;
using rainspace.Features.Workspace.Presentation;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceNavigationPresenterTests
{
    [TestMethod]
    public void Reconcile_ReorderKeepsStableGroupAndUpdatesLegacyIndex()
    {
        var first = Group("first");
        var second = Group("second");
        var data = Data(first, second);
        var presenter = new WorkspaceNavigationPresenter();
        presenter.TrySelect(data, 1);

        data.MenuList.Move(1, 0);
        bool changed = presenter.Reconcile(data);

        Assert.IsFalse(changed);
        Assert.AreSame(second, presenter.GetActiveGroup(data));
        Assert.AreEqual(0, data.AppConfig.SelectedMenuIndex);
    }

    [TestMethod]
    public void Reconcile_SelectedGroupRemoved_ClearsLegacySelection()
    {
        var selected = Group("selected");
        var data = Data(selected, Group("other"));
        data.AppConfig.SelectedMenuIcons = selected.IconList;
        var presenter = new WorkspaceNavigationPresenter();
        presenter.TrySelect(data, 0);

        data.MenuList.Remove(selected);
        bool changed = presenter.Reconcile(data);

        Assert.IsTrue(changed);
        Assert.IsTrue(presenter.IsAllGroups);
        Assert.AreEqual(-1, data.AppConfig.SelectedMenuIndex);
        Assert.IsNull(data.AppConfig.SelectedMenuIcons);
    }

    [TestMethod]
    public void Restore_UsesSnapshotGroupIdInsteadOfOldIndex()
    {
        var first = Group("first");
        var second = Group("second");
        var data = Data(first, second);
        var presenter = new WorkspaceNavigationPresenter();
        presenter.TrySelect(data, 1);
        WorkspaceNavigationSnapshot snapshot = presenter.Capture();
        presenter.SelectAll(data);
        data.MenuList.Move(1, 0);

        bool restored = presenter.Restore(data, snapshot);

        Assert.IsTrue(restored);
        Assert.AreSame(second, presenter.GetActiveGroup(data));
        Assert.AreEqual(0, data.AppConfig.SelectedMenuIndex);
    }

    private static AppData Data(params MenuInfo[] groups) => new()
    {
        MenuList = new ObservableCollection<MenuInfo>(groups)
    };

    private static MenuInfo Group(string id) => new()
    {
        MenuId = id,
        MenuName = id
    };
}
