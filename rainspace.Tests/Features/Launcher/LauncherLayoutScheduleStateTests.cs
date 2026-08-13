using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherLayoutScheduleStateTests
{
    [TestMethod]
    public void Request_CoalescesBurstAndKeepsNewestGenerationAndAnimationIntent()
    {
        var state = new LauncherLayoutScheduleState();

        bool startFirstTimer = state.Request(animate: false);
        bool startSecondTimer = state.Request(animate: true);
        bool consumed = state.TryConsume(out LauncherLayoutRequest request);

        Assert.IsTrue(startFirstTimer);
        Assert.IsFalse(startSecondTimer);
        Assert.IsTrue(consumed);
        Assert.AreEqual(2, request.Generation);
        Assert.IsTrue(request.AnimateRequested);
        Assert.IsTrue(state.IsCurrent(request.Generation));
    }

    [TestMethod]
    public void Consume_ResetsPendingAnimationForNextFrame()
    {
        var state = new LauncherLayoutScheduleState();
        state.Request(animate: true);
        state.TryConsume(out _);

        bool startsTimer = state.Request(animate: false);
        state.TryConsume(out LauncherLayoutRequest request);

        Assert.IsTrue(startsTimer);
        Assert.IsFalse(request.AnimateRequested);
        Assert.AreEqual(2, request.Generation);
    }

    [TestMethod]
    public void Cancel_DropsPendingFrameWithoutReusingAnimationIntent()
    {
        var state = new LauncherLayoutScheduleState();
        state.Request(animate: true);

        state.Cancel();

        Assert.IsFalse(state.TryConsume(out _));
        Assert.IsTrue(state.Request(animate: false));
        state.TryConsume(out LauncherLayoutRequest request);
        Assert.IsFalse(request.AnimateRequested);
    }
}
