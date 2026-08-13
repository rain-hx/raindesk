using System.Collections.ObjectModel;
using System.Collections.Specialized;
using raindesk.Features.Workspace.Presentation;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupCollectionSubscriptionTests
{
    [TestMethod]
    public void AttachReplacementAndDetach_KeepOnlyCurrentSourceObserved()
    {
        var first = new ObservableCollection<string>();
        var second = new ObservableCollection<string>();
        var subscription = new WorkspaceGroupCollectionSubscription();
        int notifications = 0;
        NotifyCollectionChangedEventHandler handler = (_, _) => notifications++;

        subscription.Attach(first, handler);
        first.Add("first");
        subscription.Attach(second, handler);
        first.Add("stale");
        second.Add("second");
        subscription.Detach();
        second.Add("detached");

        Assert.AreEqual(2, notifications);
        Assert.IsFalse(subscription.IsAttached);
    }

    [TestMethod]
    public void AttachingSameSourceAndHandler_IsIdempotent()
    {
        var source = new ObservableCollection<string>();
        var subscription = new WorkspaceGroupCollectionSubscription();
        int notifications = 0;
        NotifyCollectionChangedEventHandler handler = (_, _) => notifications++;

        subscription.Attach(source, handler);
        subscription.Attach(source, handler);
        source.Add("one");

        Assert.AreEqual(1, notifications);
    }
}
