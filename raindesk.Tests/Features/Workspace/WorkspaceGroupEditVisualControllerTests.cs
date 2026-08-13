using raindesk.Features.Workspace.Presentation.WinUI;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupEditVisualControllerTests
{
    [TestMethod]
    public void Controller_CanBeCreatedWithoutWindowState()
    {
        Assert.IsNotNull(new WorkspaceGroupEditVisualController());
        Assert.IsNotNull(new WorkspaceGroupEditFocusScheduler());
    }
}
