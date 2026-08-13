using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherPointerCacheStateTests
{
    [TestMethod]
    public void StoreTryGetAndClear_ControlCacheLifetime()
    {
        var state = new LauncherPointerCacheState();
        Assert.IsFalse(state.TryGet(out _));

        state.Store(new LauncherPoint(12d, 34d));

        Assert.IsTrue(state.TryGet(out LauncherPoint point));
        Assert.AreEqual(new LauncherPoint(12d, 34d), point);

        state.Clear();
        Assert.IsFalse(state.TryGet(out _));
        Assert.AreEqual(default, state.Value);
    }
}
