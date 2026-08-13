using raindesk.Features.Launcher.Application;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherFolderDropPolicyTests
{
    private static readonly LauncherShortcutDropTraits Shortcut = new(false, false, false);
    private static readonly LauncherShortcutDropTraits Folder = new(true, false, false);
    private static readonly LauncherShortcutDropTraits Placeholder = new(false, false, true);

    [TestMethod]
    public void TwoDifferentNormalShortcuts_CanCreateFolder()
    {
        Assert.IsTrue(LauncherFolderDropPolicy.CanCreateFolder(
            Shortcut,
            Shortcut,
            isSameItem: false));
    }

    [TestMethod]
    public void FolderOrPlaceholder_CannotCreateFolder()
    {
        Assert.IsFalse(LauncherFolderDropPolicy.CanCreateFolder(
            Folder,
            Shortcut,
            isSameItem: false));
        Assert.IsFalse(LauncherFolderDropPolicy.CanCreateFolder(
            Shortcut,
            Placeholder,
            isSameItem: false));
    }

    [TestMethod]
    public void NormalShortcut_CanMoveIntoDifferentFolder()
    {
        Assert.IsTrue(LauncherFolderDropPolicy.CanMoveIntoFolder(
            Shortcut,
            Folder,
            isSameItem: false));
    }

    [TestMethod]
    public void FolderOrPlaceholder_CannotMoveIntoFolder()
    {
        Assert.IsFalse(LauncherFolderDropPolicy.CanMoveIntoFolder(
            Folder,
            Folder,
            isSameItem: false));
        Assert.IsFalse(LauncherFolderDropPolicy.CanMoveIntoFolder(
            Placeholder,
            Folder,
            isSameItem: false));
    }
}
