using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherContextMenuPolicyTests
{
    [TestMethod]
    public void Placeholder_HasNoContextActions()
    {
        LauncherContextMenuPlan plan = LauncherContextMenuPolicy.Resolve(
            new LauncherContextMenuRequest("add", IsFolder: false, IsPlaceholder: true, HasFileLocation: false));

        Assert.IsFalse(plan.IsAvailable);
        Assert.IsFalse(plan.IsVisible(LauncherContextMenuAction.Remove));
    }

    [TestMethod]
    public void Folder_OnlyExposesFolderAndEditActions()
    {
        LauncherContextMenuPlan plan = LauncherContextMenuPolicy.Resolve(
            new LauncherContextMenuRequest("folder", IsFolder: true, IsPlaceholder: false, HasFileLocation: false));

        Assert.IsTrue(plan.IsVisible(LauncherContextMenuAction.OpenFolder));
        Assert.IsTrue(plan.IsVisible(LauncherContextMenuAction.ResizeFolder));
        Assert.IsTrue(plan.IsVisible(LauncherContextMenuAction.Rename));
        Assert.IsTrue(plan.IsVisible(LauncherContextMenuAction.Remove));
        Assert.IsFalse(plan.IsVisible(LauncherContextMenuAction.RunAsAdministrator));
        Assert.IsFalse(plan.IsVisible(LauncherContextMenuAction.ShowInFileExplorer));
        Assert.IsFalse(plan.IsVisible(LauncherContextMenuAction.EditProperties));
    }

    [TestMethod]
    public void Shortcut_OnlyShowsFileExplorerActionWhenLocationExists()
    {
        LauncherContextMenuPlan withoutLocation = LauncherContextMenuPolicy.Resolve(
            new LauncherContextMenuRequest("url", IsFolder: false, IsPlaceholder: false, HasFileLocation: false));
        LauncherContextMenuPlan withLocation = LauncherContextMenuPolicy.Resolve(
            new LauncherContextMenuRequest("app", IsFolder: false, IsPlaceholder: false, HasFileLocation: true));

        Assert.IsTrue(withoutLocation.IsVisible(LauncherContextMenuAction.RunAsAdministrator));
        Assert.IsTrue(withoutLocation.IsVisible(LauncherContextMenuAction.EditProperties));
        Assert.IsFalse(withoutLocation.IsVisible(LauncherContextMenuAction.ShowInFileExplorer));
        Assert.IsTrue(withLocation.IsVisible(LauncherContextMenuAction.ShowInFileExplorer));
        Assert.IsFalse(withLocation.IsVisible(LauncherContextMenuAction.OpenFolder));
    }

    [TestMethod]
    public void Coordinator_ConstructsStableIdMutationsAndRejectsInvalidActions()
    {
        var coordinator = new LauncherContextMenuCoordinator<Item>(
            item => item.Id,
            item => item.IsFolder,
            item => item.IsPlaceholder,
            item => item.HasLocation);
        var folder = new Item("folder", IsFolder: true);
        var shortcut = new Item("shortcut");
        var placeholder = new Item("add", IsPlaceholder: true);

        LauncherMutationCommand? resize = coordinator.CreateResizeFolderCommand(
            folder,
            LauncherFolderSize.Large);
        LauncherMutationCommand? remove = coordinator.CreateRemoveCommand(shortcut);
        LauncherMutationCommand? moveOut = coordinator.CreateMoveOutOfFolderCommand(
            folder,
            shortcut,
            insertIndex: 4);

        Assert.AreEqual(LauncherMutationKind.ResizeFolder, resize?.Kind);
        Assert.AreEqual("folder", resize?.SubjectShortcutId);
        Assert.AreEqual(LauncherFolderSize.Large, resize?.FolderSize);
        Assert.AreEqual(LauncherMutationKind.RemoveShortcut, remove?.Kind);
        Assert.AreEqual("shortcut", remove?.SubjectShortcutId);
        Assert.AreEqual(LauncherMutationKind.MoveOutOfFolder, moveOut?.Kind);
        Assert.AreEqual("folder", moveOut?.TargetShortcutId);
        Assert.AreEqual("shortcut", moveOut?.SubjectShortcutId);
        Assert.AreEqual(4, moveOut?.InsertIndex);
        Assert.IsNull(coordinator.CreateResizeFolderCommand(shortcut, LauncherFolderSize.Small));
        Assert.IsNull(coordinator.CreateRemoveCommand(placeholder));
        Assert.IsNull(coordinator.CreateMoveOutOfFolderCommand(shortcut, folder, 0));
    }

    private sealed record Item(
        string Id,
        bool IsFolder = false,
        bool IsPlaceholder = false,
        bool HasLocation = false);
}
