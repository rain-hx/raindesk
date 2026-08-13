using rainspace.Features.Launcher.Presentation.WinUI;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherDragPreviewControllerTests
{
    [TestMethod]
    public void EmptyController_ExposesSafeNoOpLifecycle()
    {
        var controller = new LauncherDragPreviewController();

        controller.SetSuppressed(true);
        controller.AnimateToPlacement(10d, 20d);
        controller.ResumeFollowMode();
        controller.ExitPlacementFollowAt(30d, 40d);
        controller.UpdatePosition(50d, 60d);

        Assert.IsFalse(controller.HasPreview);
        Assert.IsFalse(controller.IsPlacementPreviewActive);
        Assert.IsFalse(controller.End(animate: false));
    }
}
