using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupPreviewStateTests
{
    [TestMethod]
    public void Begin_OnlyCapturesSelectionAtStartOfSession()
    {
        var state = new LauncherCrossGroupPreviewState<object, string>("initial");

        bool firstBegin = state.Begin("all-groups");
        bool repeatedBegin = state.Begin("must-not-replace");

        Assert.IsTrue(firstBegin);
        Assert.IsFalse(repeatedBegin);
        Assert.AreEqual("all-groups", state.SavedSelection);
    }

    [TestMethod]
    public void TryRetarget_ChangesTokenOnlyWhenTargetChanges()
    {
        var state = new LauncherCrossGroupPreviewState<object, string>("initial");
        var first = new object();
        var second = new object();
        state.Begin("source");

        bool firstChanged = state.TryRetarget(first, out int firstToken);
        bool repeatedChanged = state.TryRetarget(first, out int repeatedToken);
        bool secondChanged = state.TryRetarget(second, out int secondToken);

        Assert.IsTrue(firstChanged);
        Assert.IsFalse(repeatedChanged);
        Assert.IsTrue(secondChanged);
        Assert.AreEqual(firstToken, repeatedToken);
        Assert.IsTrue(secondToken > firstToken);
        Assert.AreSame(second, state.Target);
    }

    [TestMethod]
    public void End_InvalidatesQueuedUpdatesAndClearsActiveTarget()
    {
        var state = new LauncherCrossGroupPreviewState<object, string>("initial");
        state.Begin("source");
        state.TryRetarget(new object(), out int previewToken);

        state.End();

        Assert.IsFalse(state.IsActive);
        Assert.IsNull(state.Target);
        Assert.IsFalse(state.IsCurrent(previewToken));
        Assert.AreEqual("source", state.SavedSelection);
    }

    [TestMethod]
    public void NewSession_DoesNotAcceptTokenFromPreviousSession()
    {
        var state = new LauncherCrossGroupPreviewState<object, string>("initial");
        state.Begin("first");
        state.TryRetarget(new object(), out int staleToken);
        state.End();

        state.Begin("second");
        state.TryRetarget(new object(), out int currentToken);

        Assert.IsFalse(state.IsCurrent(staleToken));
        Assert.IsTrue(state.IsCurrent(currentToken));
        Assert.AreEqual("second", state.SavedSelection);
    }

    [TestMethod]
    public void Coordinator_RetargetReportsSessionAndTargetChangesSeparately()
    {
        var coordinator = new LauncherCrossGroupPreviewCoordinator<object, string, string>("initial");
        var firstTarget = new object();
        var secondTarget = new object();

        LauncherCrossGroupPreviewUpdate<string> first = coordinator.Retarget(
            firstTarget,
            "source",
            new[] { "a", "b" },
            new[] { "b", "c" },
            static _ => false);
        LauncherCrossGroupPreviewUpdate<string> repeated = coordinator.Retarget(
            firstTarget,
            "must-not-replace",
            new[] { "a" },
            new[] { "c" },
            static _ => false);
        LauncherCrossGroupPreviewUpdate<string> second = coordinator.Retarget(
            secondTarget,
            "must-not-replace",
            new[] { "x" },
            new[] { "y" },
            static _ => false);

        Assert.IsTrue(first.SessionStarted);
        Assert.IsTrue(first.TargetChanged);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, first.Tiles.ToArray());
        Assert.AreEqual(1, first.TargetInsertIndex);
        Assert.IsFalse(repeated.SessionStarted);
        Assert.IsFalse(repeated.TargetChanged);
        Assert.IsFalse(second.SessionStarted);
        Assert.IsTrue(second.TargetChanged);
        Assert.IsTrue(second.Token > first.Token);
        Assert.AreEqual("source", coordinator.SavedSelection);
    }

    [TestMethod]
    public void Coordinator_EndAllowsNextSessionToCaptureNewSelection()
    {
        var coordinator = new LauncherCrossGroupPreviewCoordinator<object, string, string>("initial");
        coordinator.Retarget(
            new object(),
            "first",
            Array.Empty<string>(),
            Array.Empty<string>(),
            static _ => false);

        coordinator.End();
        LauncherCrossGroupPreviewUpdate<string> next = coordinator.Retarget(
            new object(),
            "second",
            Array.Empty<string>(),
            Array.Empty<string>(),
            static _ => false);

        Assert.IsTrue(next.SessionStarted);
        Assert.AreEqual("second", coordinator.SavedSelection);
    }

    [TestMethod]
    public void CancelPolicy_PreservesActiveBatchPlaceholdersWithoutNavigationRestore()
    {
        LauncherCrossGroupPreviewCancelPlan plan =
            LauncherCrossGroupPreviewCompletionPolicy.PlanCancel(
                isActive: true,
                draggedItemsSuppressed: true,
                customDragActive: true,
                reducedMotion: false,
                hasDraggedItem: true);

        Assert.AreEqual(
            LauncherCrossGroupPreviewCancelMode.PreserveBatchPlaceholders,
            plan.Mode);
        Assert.IsFalse(plan.RestoreBatchItems);
        Assert.IsFalse(plan.CapturePreviousPositions);
        Assert.IsFalse(plan.RecommitSingleIconTarget);
    }

    [TestMethod]
    public void CancelPolicy_FullRestoreReportsBatchAnimationAndSingleTargetDecisions()
    {
        LauncherCrossGroupPreviewCancelPlan batch =
            LauncherCrossGroupPreviewCompletionPolicy.PlanCancel(
                true,
                draggedItemsSuppressed: true,
                customDragActive: false,
                reducedMotion: false,
                hasDraggedItem: true);
        LauncherCrossGroupPreviewCancelPlan single =
            LauncherCrossGroupPreviewCompletionPolicy.PlanCancel(
                true,
                draggedItemsSuppressed: false,
                customDragActive: true,
                reducedMotion: true,
                hasDraggedItem: true);

        Assert.AreEqual(LauncherCrossGroupPreviewCancelMode.RestoreNavigation, batch.Mode);
        Assert.IsTrue(batch.RestoreBatchItems);
        Assert.IsTrue(batch.CapturePreviousPositions);
        Assert.IsFalse(batch.RecommitSingleIconTarget);
        Assert.IsFalse(single.RestoreBatchItems);
        Assert.IsFalse(single.CapturePreviousPositions);
        Assert.IsTrue(single.RecommitSingleIconTarget);
    }

    [TestMethod]
    [DataRow(false, false, false, 0)]
    [DataRow(true, true, true, 0)]
    [DataRow(true, false, true, 1)]
    [DataRow(true, false, false, 2)]
    public void NavigationPolicy_SelectsOneExplicitRestoreBranch(
        bool selectionRestored,
        bool showingAllGroups,
        bool hasActiveGroup,
        int expected)
    {
        Assert.AreEqual(
            expected,
            (int)LauncherCrossGroupPreviewCompletionPolicy.ResolveNavigation(
                selectionRestored,
                showingAllGroups,
                hasActiveGroup));
    }

    [TestMethod]
    public void Coordinator_CancelEndsSessionAndInvalidatesCurrentToken()
    {
        var coordinator = new LauncherCrossGroupPreviewCoordinator<object, string, string>("initial");
        LauncherCrossGroupPreviewUpdate<string> update = coordinator.Retarget(
            new object(),
            "source",
            Array.Empty<string>(),
            Array.Empty<string>(),
            static _ => false);

        LauncherCrossGroupPreviewCancelPlan plan = coordinator.Cancel(
            draggedItemsSuppressed: false,
            customDragActive: false,
            reducedMotion: false,
            hasDraggedItem: false);

        Assert.AreEqual(LauncherCrossGroupPreviewCancelMode.RestoreNavigation, plan.Mode);
        Assert.IsFalse(coordinator.IsActive);
        Assert.IsFalse(coordinator.IsCurrent(update.Token));
    }
}
