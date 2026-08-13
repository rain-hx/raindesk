using raindesk.Features.Launcher.Presentation.WinUI;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupPreviewOverlayControllerTests
{
    [TestMethod]
    public void Controller_CanBeConstructedWithoutWindowOrApplicationState()
    {
        var controller = new LauncherCrossGroupPreviewOverlayController();

        Assert.IsFalse(controller.IsVisible);
        Assert.IsNull(controller.View);
        Assert.AreEqual(0, controller.Tiles.Count);
    }

    [TestMethod]
    public void ReorderSuppressionController_CanClearWithoutAListView()
    {
        var controller = new LauncherIconListReorderSuppressionController();

        controller.Clear(null);

        Assert.IsFalse(controller.IsSuppressed);
    }

    [TestMethod]
    public void PlacementController_RejectsStaleTokenBeforeResolvingUiGeometry()
    {
        var controller = new LauncherCrossGroupPreviewPlacementController<object, object>(
            static () => false,
            static () => null,
            static () => Array.Empty<object>(),
            static () => null,
            static () => Array.Empty<object>(),
            static () => Array.Empty<object>(),
            static () => Array.Empty<object>(),
            static () => null,
            static () => null,
            static _ => null,
            static _ => null);
        int animations = 0;

        bool animated = controller.TryAnimatePlacement(
            token: 1,
            isCurrent: static _ => false,
            hasPreview: true,
            animationsAllowed: true,
            hideDraggedSourceItems: true,
            animate: (_, _, _) => animations++);

        Assert.IsFalse(animated);
        Assert.AreEqual(0, animations);
    }

    [TestMethod]
    public void GroupBarDragController_CanClearStateWithoutWindowOrBusinessModels()
    {
        int previewRestores = 0;
        var controller = new LauncherGroupBarDragController<object, object>(
            static () => null,
            static () => null,
            static () => null,
            static () => Array.Empty<object>(),
            static (_, _) => { },
            static () => false,
            static () => false,
            static () => null,
            static _ => false,
            static () => Array.Empty<object>(),
            static () => null,
            static (_, _) => false,
            static _ => { },
            () => previewRestores++,
            static () => throw new InvalidOperationException("Drag bitmap should not be requested."));

        controller.ClearIndicators();
        controller.ClearHighlight();

        Assert.IsNull(controller.HoverTarget);
        Assert.AreEqual(1, previewRestores);
    }
}
