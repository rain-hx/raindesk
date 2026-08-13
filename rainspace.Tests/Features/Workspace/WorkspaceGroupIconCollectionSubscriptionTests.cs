using System.Collections.ObjectModel;
using rainspace.Features.Workspace.Presentation;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupIconCollectionSubscriptionTests
{
    [TestMethod]
    public void IconListReplacement_DetachesOldCollectionAndObservesNewCollection()
    {
        var original = new ObservableCollection<IconInfo>();
        var replacement = new ObservableCollection<IconInfo>();
        var group = new MenuInfo { IconList = original };
        var subscription = new WorkspaceGroupIconCollectionSubscription();
        int replacements = 0;
        int changes = 0;
        subscription.IconListReplaced += _ => replacements++;
        subscription.IconsChanged += (_, _) => changes++;
        subscription.Synchronize(new[] { group });

        original.Add(new IconInfo());
        group.IconList = replacement;
        original.Add(new IconInfo());
        replacement.Add(new IconInfo());

        Assert.AreEqual(1, replacements);
        Assert.AreEqual(2, changes);
        Assert.AreEqual(1, subscription.TrackedGroupCount);
    }

    [TestMethod]
    public void Synchronize_RemovesSubscriptionsForGroupsNoLongerActive()
    {
        var removed = new MenuInfo();
        var retained = new MenuInfo();
        var subscription = new WorkspaceGroupIconCollectionSubscription();
        int replacements = 0;
        int changes = 0;
        subscription.IconListReplaced += _ => replacements++;
        subscription.IconsChanged += (_, _) => changes++;
        subscription.Synchronize(new[] { removed, retained });

        subscription.Synchronize(new[] { retained });
        removed.IconList.Add(new IconInfo());
        removed.IconList = new ObservableCollection<IconInfo>();
        retained.IconList.Add(new IconInfo());

        Assert.AreEqual(0, replacements);
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, subscription.TrackedGroupCount);
    }

    [TestMethod]
    public void RepeatedSynchronization_DoesNotDuplicateCollectionHandler()
    {
        var group = new MenuInfo();
        var subscription = new WorkspaceGroupIconCollectionSubscription();
        int changes = 0;
        subscription.IconsChanged += (_, _) => changes++;

        subscription.Synchronize(new[] { group });
        subscription.Synchronize(new[] { group });
        group.IconList.Add(new IconInfo());

        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void DetachAll_StopsMenuAndCollectionNotifications()
    {
        var group = new MenuInfo();
        var subscription = new WorkspaceGroupIconCollectionSubscription();
        int notifications = 0;
        subscription.IconListReplaced += _ => notifications++;
        subscription.IconsChanged += (_, _) => notifications++;
        subscription.Synchronize(new[] { group });

        subscription.DetachAll();
        group.IconList.Add(new IconInfo());
        group.IconList = new ObservableCollection<IconInfo>();

        Assert.AreEqual(0, notifications);
        Assert.AreEqual(0, subscription.TrackedGroupCount);
    }
}
