using raindesk.Features.Workspace.Presentation.WinUI;

namespace raindesk.Tests.Features.Workspace;

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
