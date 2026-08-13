using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherAllGroupsOrderStateTests
{
    [TestMethod]
    public void SetCurrent_CopiesMutableInput()
    {
        var source = new List<string> { "a", "b" };
        var state = new LauncherAllGroupsOrderState<string>();

        state.SetCurrent(source);
        source.Clear();

        CollectionAssert.AreEqual(new[] { "a", "b" }, state.CurrentOrder!.ToArray());
    }

    [TestMethod]
    public void CaptureDragStart_PrefersCurrentOrderOverFallback()
    {
        var state = new LauncherAllGroupsOrderState<string>();
        state.SetCurrent(new[] { "current" });

        state.CaptureDragStart(new[] { "fallback" });

        CollectionAssert.AreEqual(new[] { "current" }, state.DragStartOrder!.ToArray());
    }

    [TestMethod]
    public void CaptureBeforePreview_PrefersDragStartAndUpdatesCurrent()
    {
        var state = new LauncherAllGroupsOrderState<string>();
        state.CaptureDragStart(new[] { "drag-start" });

        state.CaptureBeforePreview(new[] { "preview-fallback" });

        CollectionAssert.AreEqual(new[] { "drag-start" }, state.BeforePreviewOrder!.ToArray());
        CollectionAssert.AreEqual(new[] { "drag-start" }, state.CurrentOrder!.ToArray());
    }

    [TestMethod]
    public void CancellationOrder_PrefersDragStartThenBeforePreview()
    {
        var state = new LauncherAllGroupsOrderState<string>();
        state.CaptureBeforePreview(new[] { "before-preview" });
        CollectionAssert.AreEqual(new[] { "before-preview" }, state.CancellationOrder!.ToArray());

        state.CaptureDragStart(new[] { "drag-start" });
        CollectionAssert.AreEqual(new[] { "before-preview" }, state.CancellationOrder!.ToArray());
    }

    [TestMethod]
    public void ClearDragSnapshots_PreservesCurrentOrder()
    {
        var state = new LauncherAllGroupsOrderState<string>();
        state.SetCurrent(new[] { "current" });
        state.CaptureDragStart(new[] { "fallback" });
        state.CaptureBeforePreview(new[] { "preview" });

        state.ClearDragSnapshots();

        Assert.IsNull(state.DragStartOrder);
        Assert.IsNull(state.BeforePreviewOrder);
        CollectionAssert.AreEqual(new[] { "current" }, state.CurrentOrder!.ToArray());
    }
}
