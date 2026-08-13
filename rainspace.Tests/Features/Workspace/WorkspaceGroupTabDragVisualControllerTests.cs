using rainspace.Features.Workspace.Presentation.WinUI;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabDragVisualControllerTests
{
    [TestMethod]
    public void DefaultController_HasNoPlaceholderHeight()
    {
        var controller = new WorkspaceGroupTabDragVisualController();

        controller.EndInteraction();

        Assert.AreEqual(0d, controller.PlaceholderHeight);
    }
}
