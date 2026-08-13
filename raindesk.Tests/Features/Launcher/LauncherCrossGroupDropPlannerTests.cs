using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupDropPlannerTests
{
    [TestMethod]
    public void DragEnd_PointerTargetAlwaysWins()
    {
        var pointer = new object();

        LauncherCrossGroupDropTarget<object> result =
            LauncherCrossGroupDropPlanner.ResolveAtDragEnd(
                pointer,
                previewActive: false,
                previewTarget: new object(),
                hoverTarget: new object(),
                pointerOverGroupBar: false);

        Assert.AreSame(pointer, result.Target);
        Assert.AreEqual(LauncherCrossGroupDropTargetSource.Pointer, result.Source);
    }

    [TestMethod]
    public void DragEnd_UsesPreviewThenHoverOnlyWhileActiveAndOverGroupBar()
    {
        var preview = new object();
        var hover = new object();

        LauncherCrossGroupDropTarget<object> previewResult =
            LauncherCrossGroupDropPlanner.ResolveAtDragEnd(
                null,
                true,
                preview,
                hover,
                true);
        LauncherCrossGroupDropTarget<object> hoverResult =
            LauncherCrossGroupDropPlanner.ResolveAtDragEnd(
                null,
                true,
                null,
                hover,
                true);
        LauncherCrossGroupDropTarget<object> outsideResult =
            LauncherCrossGroupDropPlanner.ResolveAtDragEnd(
                null,
                true,
                preview,
                hover,
                false);

        Assert.AreSame(preview, previewResult.Target);
        Assert.AreEqual(LauncherCrossGroupDropTargetSource.Preview, previewResult.Source);
        Assert.AreSame(hover, hoverResult.Target);
        Assert.AreEqual(LauncherCrossGroupDropTargetSource.Hover, hoverResult.Source);
        Assert.IsNull(outsideResult.Target);
    }

    [TestMethod]
    public void DropEvent_UsesDirectThenHoverThenPreviewPriority()
    {
        var direct = new object();
        var hover = new object();
        var preview = new object();

        Assert.AreSame(
            direct,
            LauncherCrossGroupDropPlanner.ResolveDropEvent(direct, hover, preview).Target);
        Assert.AreSame(
            hover,
            LauncherCrossGroupDropPlanner.ResolveDropEvent<object>(null, hover, preview).Target);
        Assert.AreSame(
            preview,
            LauncherCrossGroupDropPlanner.ResolveDropEvent<object>(null, null, preview).Target);
    }

    [TestMethod]
    public void Complete_RequiresAnActivePreviewTarget()
    {
        var preview = new object();

        Assert.AreSame(
            preview,
            LauncherCrossGroupDropPlanner.ResolveOnComplete(true, preview).Target);
        Assert.IsNull(
            LauncherCrossGroupDropPlanner.ResolveOnComplete(false, preview).Target);
        Assert.IsNull(
            LauncherCrossGroupDropPlanner.ResolveOnComplete<object>(true, null).Target);
    }

    [TestMethod]
    public void CommitPreparation_FinalizesOnlyAcceptedBatchDrop()
    {
        LauncherCrossGroupDropPreparation acceptedBatch =
            LauncherCrossGroupDropPlanner.PrepareCommit(true, true);
        LauncherCrossGroupDropPreparation rejectedBatch =
            LauncherCrossGroupDropPlanner.PrepareCommit(false, true);

        Assert.IsTrue(acceptedBatch.CanCommit);
        Assert.IsTrue(acceptedBatch.ShouldFinalizeBatchPlaceholder);
        Assert.IsFalse(rejectedBatch.CanCommit);
        Assert.IsFalse(rejectedBatch.ShouldFinalizeBatchPlaceholder);
    }

    [TestMethod]
    [DataRow(true, 3, true, 3)]
    [DataRow(true, -1, true, null)]
    [DataRow(true, 3, false, null)]
    [DataRow(false, 3, true, null)]
    public void PreviewInsertIndex_RequiresPlaceholderValidIndexAndCompatibleTarget(
        bool hasPlaceholders,
        int batchInsertIndex,
        bool targetCompatible,
        int? expected)
    {
        Assert.AreEqual(
            expected,
            LauncherCrossGroupDropPlanner.ResolvePreviewInsertIndex(
                hasPlaceholders,
                batchInsertIndex,
                targetCompatible));
    }
}
