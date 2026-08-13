using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherReorderSuppressionStateTests
{
    [TestMethod]
    public void IndependentReasons_MustAllRestoreBeforeSuppressionEnds()
    {
        var state = new LauncherReorderSuppressionState<string>();

        Assert.IsTrue(state.Suppress("batch"));
        Assert.IsTrue(state.Suppress("preview"));
        Assert.IsFalse(state.Suppress("preview"));
        Assert.AreEqual(2, state.ReasonCount);

        Assert.IsTrue(state.Restore("preview"));
        Assert.IsTrue(state.IsSuppressed);
        Assert.IsTrue(state.Contains("batch"));
        Assert.IsTrue(state.Restore("batch"));
        Assert.IsFalse(state.IsSuppressed);
    }

    [TestMethod]
    public void Clear_RemovesEverySuppressionReason()
    {
        var state = new LauncherReorderSuppressionState<int>();
        state.Suppress(1);
        state.Suppress(2);

        state.Clear();

        Assert.AreEqual(0, state.ReasonCount);
        Assert.IsFalse(state.IsSuppressed);
    }
}
