using System.Collections.Specialized;
using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherShortcutListStateTests
{
    [TestMethod]
    public void ApplySnapshot_SmallReorderUsesIncrementalNotifications()
    {
        var first = Icon("first");
        var second = Icon("second");
        var third = Icon("third");
        var state = new LauncherShortcutListState();
        state.ApplySnapshot(new[] { first, second, third });
        var actions = new List<NotifyCollectionChangedAction>();
        state.Items.CollectionChanged += (_, e) => actions.Add(e.Action);

        LauncherShortcutListUpdateKind update = state.ApplySnapshot(
            new[] { second, first, third });

        Assert.AreEqual(LauncherShortcutListUpdateKind.Incremental, update);
        CollectionAssert.AreEqual(new[] { second, first, third }, state.Items.ToArray());
        CollectionAssert.DoesNotContain(actions, NotifyCollectionChangedAction.Reset);
        CollectionAssert.Contains(actions, NotifyCollectionChangedAction.Move);
    }

    [TestMethod]
    public void ApplySnapshot_MostlyDifferentItemsUsesSingleReset()
    {
        var state = new LauncherShortcutListState();
        state.ApplySnapshot(new[] { Icon("one"), Icon("two"), Icon("three") });
        var actions = new List<NotifyCollectionChangedAction>();
        state.Items.CollectionChanged += (_, e) => actions.Add(e.Action);
        var replacement = new[] { Icon("four"), Icon("five"), Icon("six") };

        LauncherShortcutListUpdateKind update = state.ApplySnapshot(replacement);

        Assert.AreEqual(LauncherShortcutListUpdateKind.Reset, update);
        CollectionAssert.AreEqual(replacement, state.Items.ToArray());
        CollectionAssert.AreEqual(
            new[] { NotifyCollectionChangedAction.Reset },
            actions.ToArray());
    }

    [TestMethod]
    public void ApplySnapshot_IdenticalSnapshotDoesNotNotify()
    {
        var icon = Icon("same");
        var state = new LauncherShortcutListState();
        state.ApplySnapshot(new[] { icon });
        int notifications = 0;
        state.Items.CollectionChanged += (_, _) => notifications++;

        LauncherShortcutListUpdateKind update = state.ApplySnapshot(new[] { icon });

        Assert.AreEqual(LauncherShortcutListUpdateKind.None, update);
        Assert.AreEqual(0, notifications);
    }

    private static IconInfo Icon(string id) => new()
    {
        IconId = id,
        Name = id
    };
}
