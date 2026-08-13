using System.Collections.ObjectModel;
using raindesk.Features.Workspace.Contracts;
using raindesk.Features.Workspace.Infrastructure;
using raindesk.Constant;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class AppDataWorkspaceHandlersTests
{
    [TestMethod]
    public void CreateRenameAndDelete_UseStableIdAndRequestPersistence()
    {
        var store = new AppDataStore();
        var save = new RecordingSaveService();
        var handler = new AppDataWorkspaceCommandHandler(store, save);

        WorkspaceCommandResult created = handler.Create(new CreateWorkspaceGroupCommand("  Work  "));
        WorkspaceCommandResult renamed = handler.Rename(
            new RenameWorkspaceGroupCommand(created.EntityId!, "Focus"));
        WorkspaceCommandResult deleted = handler.Delete(
            new DeleteWorkspaceGroupCommand(created.EntityId!));

        Assert.IsTrue(created.Success && created.Changed);
        Assert.IsFalse(string.IsNullOrWhiteSpace(created.EntityId));
        Assert.IsTrue(renamed.Success && renamed.Changed);
        Assert.IsTrue(deleted.Success && deleted.Changed);
        Assert.AreEqual(0, store.Current.MenuList.Count);
        Assert.AreEqual(3, save.DebouncedCount);
    }

    [TestMethod]
    public void Reorder_PreservesSelectedGroupIdentity()
    {
        var first = Group("first");
        var selected = Group("selected");
        var last = Group("last");
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { first, selected, last }
        };
        data.AppConfig.SelectedMenuIndex = 1;
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataWorkspaceCommandHandler(store, save);

        WorkspaceCommandResult result = handler.Reorder(
            new ReorderWorkspaceGroupsCommand(new[] { "last", "first", "selected" }));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(
            new[] { "last", "first", "selected" },
            data.MenuList.Select(menu => menu.MenuId).ToArray());
        Assert.AreEqual(2, data.AppConfig.SelectedMenuIndex);
        Assert.AreSame(selected, data.MenuList[data.AppConfig.SelectedMenuIndex]);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void CreateLinkedFolder_MapsPureCommandToWorkspaceModel()
    {
        var store = new AppDataStore();
        var save = new RecordingSaveService();
        var handler = new AppDataWorkspaceCommandHandler(store, save);

        WorkspaceCommandResult result = handler.Create(new CreateWorkspaceGroupCommand(
            "Projects",
            WorkspaceGroupKind.LinkedFolder,
            @"D:\Projects"));

        Assert.IsTrue(result.Success && result.Changed);
        MenuInfo group = store.Current.MenuList.Single();
        Assert.AreEqual(MenuType.LINK, group.MenuType);
        Assert.AreEqual(@"D:\Projects", group.LinkPath);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void Reorder_WithIncompleteIds_IsRejectedWithoutMutationOrSave()
    {
        var first = Group("first");
        var second = Group("second");
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { first, second }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataWorkspaceCommandHandler(store, save);

        WorkspaceCommandResult result = handler.Reorder(
            new ReorderWorkspaceGroupsCommand(new[] { "first" }));

        Assert.IsFalse(result.Success);
        CollectionAssert.AreEqual(new[] { first, second }, data.MenuList.ToArray());
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void Query_ReturnsPureOrderedSummaries()
    {
        var first = Group("first", "One");
        first.IconList.Add(new IconInfo { IconId = "shortcut" });
        var second = Group("second", "Two");
        second.IsEncrypt = true;
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { first, second }
        });

        IReadOnlyList<WorkspaceGroupSummary> groups =
            new AppDataWorkspaceQueryHandler(store).GetGroups();

        Assert.AreEqual(2, groups.Count);
        Assert.AreEqual(new WorkspaceGroupSummary("first", "One", 0, 1, false), groups[0]);
        Assert.AreEqual(new WorkspaceGroupSummary("second", "Two", 1, 0, true), groups[1]);
    }

    private static MenuInfo Group(string id, string? name = null) => new()
    {
        MenuId = id,
        MenuName = name ?? id
    };

    private sealed class RecordingSaveService : ISaveService
    {
        public int DebouncedCount { get; private set; }

        public void SaveNow() { }

        public void SaveDebounced() => DebouncedCount++;

        public void CancelPendingSave() { }

        public void Flush() { }
    }
}
