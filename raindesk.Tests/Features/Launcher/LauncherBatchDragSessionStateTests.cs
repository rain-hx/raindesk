using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherBatchDragSessionStateTests
{
    [TestMethod]
    public void OrderDraggedIcons_UsesCapturedOriginalIndices()
    {
        var first = Icon("first");
        var second = Icon("second");
        var state = new LauncherBatchDragSessionState();
        state.OriginalIndices[first] = 1;
        state.OriginalIndices[second] = 0;

        IReadOnlyList<IconInfo> ordered = state.OrderDraggedIcons(new[] { first, second });

        CollectionAssert.AreEqual(new[] { second, first }, ordered.ToArray());
    }

    [TestMethod]
    public void UpdateInsertIndex_UsesFirstSessionPlaceholder()
    {
        var placeholderA = Icon("placeholder-a");
        var placeholderB = Icon("placeholder-b");
        var state = new LauncherBatchDragSessionState();
        state.YieldPlaceholders.AddRange(new[] { placeholderA, placeholderB });

        int index = state.UpdateInsertIndex(new[]
        {
            Icon("before"), placeholderA, placeholderB, Icon("after")
        });

        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void MoveThrottle_BlocksImmediateAndOscillatingTargets()
    {
        var state = new LauncherBatchDragSessionState { InsertIndex = 2 };
        state.RecordCommittedMove(previousIndex: 1, newInsertIndex: 2, nowTick: 1000);

        Assert.IsFalse(state.ShouldCommitTargetMove(3, nowTick: 1020, throttleMilliseconds: 48));
        Assert.IsFalse(state.ShouldCommitTargetMove(1, nowTick: 1100, throttleMilliseconds: 48));
        Assert.IsTrue(state.ShouldCommitTargetMove(4, nowTick: 1100, throttleMilliseconds: 48));
    }

    [TestMethod]
    public void MoveThrottle_AllowsFirstDistinctTargetWithoutWarmupDelay()
    {
        var state = new LauncherBatchDragSessionState { InsertIndex = 2 };

        Assert.IsTrue(state.ShouldCommitTargetMove(3, nowTick: 10, throttleMilliseconds: 48));
    }

    [TestMethod]
    public void Reset_ClearsAllTransientSessionState()
    {
        var state = new LauncherBatchDragSessionState
        {
            IsSuppressedFromTiles = true,
            InsertIndex = 2,
            SourceInsertIndex = 1
        };
        state.OriginalIndices[Icon("item")] = 0;
        state.YieldPlaceholders.Add(Icon("placeholder"));
        state.RecordCommittedMove(1, 2, 100);

        state.Reset();

        Assert.IsFalse(state.IsSuppressedFromTiles);
        Assert.AreEqual(-1, state.InsertIndex);
        Assert.AreEqual(-1, state.SourceInsertIndex);
        Assert.AreEqual(-1, state.LastCommittedTarget);
        Assert.AreEqual(0L, state.LastMoveTick);
        Assert.AreEqual(0, state.OriginalIndices.Count);
        Assert.AreEqual(0, state.YieldPlaceholders.Count);
    }

    private static IconInfo Icon(string id) => new() { IconId = id, Name = id };
}
