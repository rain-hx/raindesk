using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherBatchModeStateTests
{
    [TestMethod]
    public void SetEnabled_NotifiesOnlyWhenModeChanges()
    {
        var state = new LauncherBatchModeState();
        int notifications = 0;
        state.Changed += (_, _) => notifications++;

        bool first = state.SetEnabled(true);
        bool duplicate = state.SetEnabled(true);
        bool disabled = state.SetEnabled(false);

        Assert.IsTrue(first);
        Assert.IsFalse(duplicate);
        Assert.IsTrue(disabled);
        Assert.IsFalse(state.IsEnabled);
        Assert.AreEqual(2, notifications);
    }

    [TestMethod]
    public void Toggle_ReturnsNewMode()
    {
        var state = new LauncherBatchModeState();

        Assert.IsTrue(state.Toggle());
        Assert.IsFalse(state.Toggle());
    }
}
