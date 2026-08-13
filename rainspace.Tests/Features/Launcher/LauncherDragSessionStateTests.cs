using rainspace.Features.Launcher.Presentation;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherDragSessionStateTests
{
    [TestMethod]
    public void Begin_SetsCustomDragActiveAndResetsTransients()
    {
        var state = new LauncherDragSessionState();
        state.IsCrossGroupDrag = true;
        state.HighlightedFolderDropTarget = new IconInfo();

        state.Begin();

        Assert.IsTrue(state.IsCustomDragActive);
        Assert.IsFalse(state.IsCrossGroupDrag);
        Assert.IsFalse(state.DragStartedFromFolderOverlay);
        Assert.IsFalse(state.SingleIconLiveMoveMutatedTiles);
        Assert.IsFalse(state.FolderDropCommittedByDropEvent);
        Assert.IsNull(state.HighlightedFolderDropTarget);
    }

    [TestMethod]
    public void Reset_ClearsAllState()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.DraggedIcon = new IconInfo();
        state.DraggedIcons.Add(new IconInfo());
        state.DragSourceIndex = 5;

        state.Reset();

        Assert.IsFalse(state.IsCustomDragActive);
        Assert.IsNull(state.DraggedIcon);
        Assert.AreEqual(0, state.DraggedIcons.Count);
        Assert.AreEqual(-1, state.DragSourceIndex);
    }

    [TestMethod]
    public void IsMergedBatchDragActive_ReturnsTrueWhenSuppressedAndMultipleDragged()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.IsDraggedIconsSuppressedFromTiles = true;
        state.DraggedIcons.Add(new IconInfo());
        state.DraggedIcons.Add(new IconInfo());

        Assert.IsTrue(state.IsMergedBatchDragActive);
    }

    [TestMethod]
    public void IsMergedBatchDragActive_ReturnsFalseWhenNotSuppressed()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.IsDraggedIconsSuppressedFromTiles = false;
        state.DraggedIcons.Add(new IconInfo());
        state.DraggedIcons.Add(new IconInfo());

        Assert.IsFalse(state.IsMergedBatchDragActive);
    }

    [TestMethod]
    public void IsMergedBatchDragActive_ReturnsFalseWhenSingleDragged()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.IsDraggedIconsSuppressedFromTiles = true;
        state.DraggedIcons.Add(new IconInfo());

        Assert.IsFalse(state.IsMergedBatchDragActive);
    }

    [TestMethod]
    public void IsYieldPlaceholderDragActive_ReturnsTrueWhenSuppressedAndHasPlaceholders()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.IsDraggedIconsSuppressedFromTiles = true;
        state.BatchDragYieldPlaceholders.Add(new IconInfo());

        Assert.IsTrue(state.IsYieldPlaceholderDragActive);
    }

    [TestMethod]
    public void BatchDragInsertIndex_DelegatesToBatchDragState()
    {
        var state = new LauncherDragSessionState();
        state.BatchDragInsertIndex = 42;

        Assert.AreEqual(42, state.BatchDrag.InsertIndex);
    }

    [TestMethod]
    public void BatchDragSourceInsertIndex_DelegatesToBatchDragState()
    {
        var state = new LauncherDragSessionState();
        state.BatchDragSourceInsertIndex = 7;

        Assert.AreEqual(7, state.BatchDrag.SourceInsertIndex);
    }

    [TestMethod]
    public void SubStateInstances_AreNotNull()
    {
        var state = new LauncherDragSessionState();

        Assert.IsNotNull(state.SingleIconPreview);
        Assert.IsNotNull(state.PointerCache);
        Assert.IsNotNull(state.FolderDropIntent);
        Assert.IsNotNull(state.AllGroupsOrder);
        Assert.IsNotNull(state.CrossGroupPreview);
        Assert.IsNotNull(state.BatchPlaceholders);
        Assert.IsNotNull(state.BatchDrag);
    }

    [TestMethod]
    public void Reset_ClearsSubStates()
    {
        var state = new LauncherDragSessionState();
        state.Begin();
        state.SingleIconPreview.Begin(5);
        state.BatchDrag.InsertIndex = 3;

        state.Reset();

        // SingleIconPreview.ClearPending is called by Reset; TargetIndex is not reset
        // since it's managed by the drag lifecycle, not the session state.
        Assert.AreEqual(-1, state.BatchDrag.InsertIndex);
        Assert.IsFalse(state.IsCustomDragActive);
    }
}
