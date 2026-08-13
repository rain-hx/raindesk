using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherFolderDropCommandResolverTests
{
    private static readonly LauncherShortcutDropTraits Shortcut = new(false, false, false);
    private static readonly LauncherShortcutDropTraits Folder = new(true, false, false);

    [TestMethod]
    public void TargetFolder_ResolvesMoveIntoFolderCommand()
    {
        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(Request(
            targetId: "child-folder",
            targetTraits: Folder,
            belongsToCurrentFolder: true));

        Assert.IsTrue(decision.CanExecute);
        Assert.AreEqual(LauncherMutationKind.MoveIntoFolder, decision.Command!.Kind);
        Assert.AreEqual("child-folder", decision.Command.TargetShortcutId);
    }

    [TestMethod]
    public void NormalSibling_ResolvesCreateFolderCommand()
    {
        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(Request(
            targetId: "sibling",
            targetTraits: Shortcut,
            belongsToCurrentFolder: true));

        Assert.IsTrue(decision.CanExecute);
        Assert.AreEqual(LauncherMutationKind.CreateFolder, decision.Command!.Kind);
        Assert.AreEqual("sibling", decision.Command.TargetShortcutId);
    }

    [TestMethod]
    public void EmptyAreaForExistingChild_ResolvesReorderCommand()
    {
        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(Request(
            targetId: null,
            targetTraits: null,
            belongsToCurrentFolder: true,
            reorderIndex: 3));

        Assert.IsTrue(decision.CanExecute);
        Assert.AreEqual(LauncherMutationKind.Reorder, decision.Command!.Kind);
        Assert.AreEqual(3, decision.Command.InsertIndex);
    }

    [TestMethod]
    public void ExternalShortcutOnEmptyArea_ResolvesMoveIntoCurrentFolder()
    {
        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(Request(
            targetId: null,
            targetTraits: null,
            belongsToCurrentFolder: false));

        Assert.IsTrue(decision.CanExecute);
        Assert.AreEqual(LauncherMutationKind.MoveIntoFolder, decision.Command!.Kind);
        Assert.AreEqual("current", decision.Command.TargetShortcutId);
    }

    [TestMethod]
    public void ExternalFolder_IsRejectedInsteadOfProducingInvalidNestedFolderCommand()
    {
        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(Request(
            targetId: null,
            targetTraits: null,
            belongsToCurrentFolder: false,
            draggedTraits: Folder));

        Assert.IsFalse(decision.CanExecute);
        Assert.IsNotNull(decision.RejectionReason);
    }

    [TestMethod]
    public void MissingStableId_IsRejected()
    {
        LauncherFolderDropCommandRequest request = Request(
            targetId: null,
            targetTraits: null,
            belongsToCurrentFolder: false) with
        {
            DraggedShortcutId = null
        };

        LauncherFolderDropDecision decision = LauncherFolderDropCommandResolver.Resolve(request);

        Assert.IsFalse(decision.CanExecute);
    }

    private static LauncherFolderDropCommandRequest Request(
        string? targetId,
        LauncherShortcutDropTraits? targetTraits,
        bool belongsToCurrentFolder,
        int reorderIndex = 0,
        LauncherShortcutDropTraits? draggedTraits = null) => new(
            DraggedShortcutId: "dragged",
            DraggedTraits: draggedTraits ?? Shortcut,
            CurrentFolderId: "current",
            TargetShortcutId: targetId,
            TargetTraits: targetTraits,
            DraggedBelongsToCurrentFolder: belongsToCurrentFolder,
            ReorderIndex: reorderIndex);
}
