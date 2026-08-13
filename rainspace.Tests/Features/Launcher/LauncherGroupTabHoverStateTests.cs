using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherGroupTabHoverStateTests
{
    [TestMethod]
    public void SetTarget_ReturnsPreviousAndCurrentTarget()
    {
        var state = new LauncherGroupTabHoverState<object>();
        var first = new object();
        var second = new object();
        state.SetTarget(first);

        LauncherGroupTabHoverChange<object> change = state.SetTarget(second);

        Assert.IsTrue(change.Changed);
        Assert.AreSame(first, change.PreviousTarget);
        Assert.AreSame(second, change.CurrentTarget);
    }

    [TestMethod]
    public void SettingSameTarget_DoesNotReportChange()
    {
        var state = new LauncherGroupTabHoverState<object>();
        var target = new object();
        state.SetTarget(target);

        LauncherGroupTabHoverChange<object> change = state.SetTarget(target);

        Assert.IsFalse(change.Changed);
    }

    [TestMethod]
    public void Clear_RemovesActiveTarget()
    {
        var state = new LauncherGroupTabHoverState<object>();
        var target = new object();
        state.SetTarget(target);

        LauncherGroupTabHoverChange<object> change = state.Clear();

        Assert.AreSame(target, change.PreviousTarget);
        Assert.IsNull(state.Target);
    }
}
