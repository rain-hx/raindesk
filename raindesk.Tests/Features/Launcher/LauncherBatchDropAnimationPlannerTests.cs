using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherBatchDropAnimationPlannerTests
{
    [TestMethod]
    public void GetReleaseOffset_ScattersAcrossThreeColumnsAndRows()
    {
        Assert.AreEqual(new LauncherAnimationOffset(-10d, -8d),
            LauncherBatchDropAnimationPlanner.GetReleaseOffset(0, 4));
        Assert.AreEqual(new LauncherAnimationOffset(0d, -8d),
            LauncherBatchDropAnimationPlanner.GetReleaseOffset(1, 4));
        Assert.AreEqual(new LauncherAnimationOffset(10d, -8d),
            LauncherBatchDropAnimationPlanner.GetReleaseOffset(2, 4));
        Assert.AreEqual(new LauncherAnimationOffset(0d, -12d),
            LauncherBatchDropAnimationPlanner.GetReleaseOffset(4, 4));
    }

    [TestMethod]
    public void GetStepDelay_ClampsNegativeInputs()
    {
        Assert.AreEqual(TimeSpan.Zero,
            LauncherBatchDropAnimationPlanner.GetStepDelay(-1, TimeSpan.FromMilliseconds(24)));
        Assert.AreEqual(TimeSpan.Zero,
            LauncherBatchDropAnimationPlanner.GetStepDelay(3, TimeSpan.FromMilliseconds(-24)));
        Assert.AreEqual(TimeSpan.FromMilliseconds(72),
            LauncherBatchDropAnimationPlanner.GetStepDelay(3, TimeSpan.FromMilliseconds(24)));
    }

    [TestMethod]
    public void GetCleanupDelay_CoversLongestStaggeredSequence()
    {
        TimeSpan delay = LauncherBatchDropAnimationPlanner.GetCleanupDelay(
            startedAnimations: 2,
            trailingCount: 4,
            droppedCount: 3,
            animationDuration: TimeSpan.FromMilliseconds(280),
            stagger: TimeSpan.FromMilliseconds(24),
            padding: TimeSpan.FromMilliseconds(60));

        Assert.AreEqual(TimeSpan.FromMilliseconds(484), delay);
    }
}
