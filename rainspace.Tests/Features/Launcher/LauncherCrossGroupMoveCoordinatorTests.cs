using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupMoveCoordinatorTests
{
    [TestMethod]
    public void Execute_UsesPreviewIndexAndStableIds()
    {
        var handler = new RecordingHandler(new LauncherCommandResult(true, true));
        var coordinator = new LauncherCrossGroupMoveCoordinator(handler);

        LauncherCrossGroupMoveResult result = coordinator.Execute(new LauncherCrossGroupMoveRequest(
            new[] { "first", "first", "second" },
            "target",
            TargetItemCount: 3,
            PreviewInsertIndex: 2,
            PreserveAllGroupsOrder: false));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(2, result.InsertIndex);
        Assert.AreEqual(LauncherMutationKind.MoveToGroup, handler.Command!.Kind);
        CollectionAssert.AreEqual(new[] { "first", "second" }, handler.Command.ShortcutIds!.ToArray());
        Assert.AreEqual("target", handler.Command.TargetGroupId);
        Assert.AreEqual(2, handler.Command.InsertIndex);
    }

    [TestMethod]
    public void Execute_ClampsPreviewIndexAndRequestsAllGroupsOrderRestore()
    {
        var handler = new RecordingHandler(new LauncherCommandResult(true, true));
        var coordinator = new LauncherCrossGroupMoveCoordinator(handler);

        LauncherCrossGroupMoveResult result = coordinator.Execute(new LauncherCrossGroupMoveRequest(
            new[] { "shortcut" },
            "target",
            TargetItemCount: 2,
            PreviewInsertIndex: 20,
            PreserveAllGroupsOrder: true));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(2, result.InsertIndex);
        Assert.AreEqual(
            LauncherCrossGroupMoveViewAction.RestoreCapturedAllGroupsOrder,
            result.ViewAction);
    }

    [TestMethod]
    public void Execute_CommandFailureIsNotReportedAsCommittedInAllGroupsView()
    {
        var handler = new RecordingHandler(LauncherCommandResult.Failed("failed"));
        var coordinator = new LauncherCrossGroupMoveCoordinator(handler);

        LauncherCrossGroupMoveResult result = coordinator.Execute(new LauncherCrossGroupMoveRequest(
            new[] { "shortcut" },
            "target",
            TargetItemCount: 0,
            PreviewInsertIndex: null,
            PreserveAllGroupsOrder: true));

        Assert.IsFalse(result.Committed);
        Assert.AreEqual(LauncherCrossGroupMoveViewAction.None, result.ViewAction);
        Assert.AreEqual("failed", result.Error);
    }

    [TestMethod]
    public void Execute_NoChangeIsNotReportedAsCommitted()
    {
        var handler = new RecordingHandler(new LauncherCommandResult(true, false));
        var coordinator = new LauncherCrossGroupMoveCoordinator(handler);

        LauncherCrossGroupMoveResult result = coordinator.Execute(new LauncherCrossGroupMoveRequest(
            new[] { "shortcut" },
            "target",
            TargetItemCount: 1,
            PreviewInsertIndex: null,
            PreserveAllGroupsOrder: true));

        Assert.IsFalse(result.Committed);
        Assert.AreEqual(LauncherCrossGroupMoveViewAction.None, result.ViewAction);
    }

    [TestMethod]
    public void Execute_InvalidIdsAreRejectedWithoutCallingHandler()
    {
        var handler = new RecordingHandler(new LauncherCommandResult(true, true));
        var coordinator = new LauncherCrossGroupMoveCoordinator(handler);

        LauncherCrossGroupMoveResult result = coordinator.Execute(new LauncherCrossGroupMoveRequest(
            new[] { "", " " },
            "target",
            TargetItemCount: 0,
            PreviewInsertIndex: null,
            PreserveAllGroupsOrder: false));

        Assert.IsFalse(result.Committed);
        Assert.IsNull(handler.Command);
    }

    private sealed class RecordingHandler : ILauncherCommandHandler
    {
        private readonly LauncherCommandResult _result;

        public RecordingHandler(LauncherCommandResult result)
        {
            _result = result;
        }

        public LauncherMutationCommand? Command { get; private set; }

        public LauncherCommandResult Execute(LauncherMutationCommand command)
        {
            Command = command;
            return _result;
        }

        public LauncherCommandResult Add(AddLauncherShortcutCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult AddMany(AddLauncherShortcutsCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult Rename(RenameLauncherShortcutCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult Update(UpdateLauncherShortcutCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult UpdateGridLayout(UpdateLauncherGridLayoutCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult UpdateOrder(UpdateLauncherShortcutOrderCommand command) =>
            throw new NotSupportedException();

        public LauncherCommandResult MutateAllGroupsFolder(
            LauncherAllGroupsFolderMutationCommand command) =>
            throw new NotSupportedException();
    }
}
