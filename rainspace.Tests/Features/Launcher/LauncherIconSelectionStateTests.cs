using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherIconSelectionStateTests
{
    [TestMethod]
    public void ResolveRangeTo_UsesAnchorThenMovesAnchorToRangeEnd()
    {
        var state = new LauncherIconSelectionState<string>();
        state.SetAnchor(2);

        LauncherSelectionRange? first = state.ResolveRangeTo(5, itemCount: 8);
        LauncherSelectionRange? second = state.ResolveRangeTo(1, itemCount: 8);

        Assert.AreEqual(new LauncherSelectionRange(2, 5), first);
        Assert.AreEqual(new LauncherSelectionRange(1, 5), second);
        Assert.AreEqual(1, state.AnchorIndex);
    }

    [TestMethod]
    public void ResolveRangeTo_InvalidEnd_DoesNotChangeAnchor()
    {
        var state = new LauncherIconSelectionState<string>();
        state.SetAnchor(3);

        LauncherSelectionRange? range = state.ResolveRangeTo(9, itemCount: 4);

        Assert.IsNull(range);
        Assert.AreEqual(3, state.AnchorIndex);
    }

    [TestMethod]
    public void MarqueeSession_UsesPointerIdentityAndCapturedAdditiveBaseline()
    {
        var state = new LauncherIconSelectionState<string>();
        string[] baseline = { "a", "b", "a" };
        state.BeginMarquee(
            pointerId: 7,
            new LauncherSelectionPoint(10, 20),
            additive: true,
            baseline);

        bool wrongPointer = state.TryGetMarquee(8, out _);
        bool found = state.TryGetMarquee(7, out LauncherMarqueeSnapshot<string> snapshot);
        bool wrongCompletion = state.CompleteMarquee(8);
        bool completed = state.CompleteMarquee(7);

        Assert.IsFalse(wrongPointer);
        Assert.IsTrue(found);
        Assert.AreEqual(new LauncherSelectionPoint(10, 20), snapshot.Start);
        CollectionAssert.AreEqual(new[] { "a", "b" }, snapshot.BaselineSelection.ToArray());
        Assert.IsFalse(wrongCompletion);
        Assert.IsTrue(completed);
        Assert.IsFalse(state.IsMarqueeActive);
    }

    [TestMethod]
    public void DragVisualSnapshots_AreReturnedOnlyOnce()
    {
        var state = new LauncherIconSelectionState<string>();
        state.CaptureDragVisuals(new[]
        {
            new KeyValuePair<string, bool>("a", true),
            new KeyValuePair<string, bool>("b", false)
        });

        IReadOnlyList<KeyValuePair<string, bool>> first = state.TakeDragVisuals();
        IReadOnlyList<KeyValuePair<string, bool>> second = state.TakeDragVisuals();

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(0, second.Count);
        Assert.IsTrue(first.Single(pair => pair.Key == "a").Value);
    }
}
