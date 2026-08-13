using System.Collections.Specialized;
using rainspace.Util;

namespace rainspace.Tests.Util;

[TestClass]
public class BulkObservableCollectionTests
{
    [TestMethod]
    public void ReplaceAll_WithSameItems_DoesNotRaiseCollectionChanged()
    {
        var collection = new BulkObservableCollection<string>();
        collection.AddRange(["Alpha", "Beta"]);
        int changeCount = 0;
        collection.CollectionChanged += (_, _) => changeCount++;

        collection.ReplaceAll(["Alpha", "Beta"]);

        Assert.AreEqual(0, changeCount);
        CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, collection.ToArray());
    }

    [TestMethod]
    public void ReplaceAll_WithDifferentItems_RaisesSingleReset()
    {
        var collection = new BulkObservableCollection<string>();
        collection.AddRange(["Alpha", "Beta"]);
        int changeCount = 0;
        NotifyCollectionChangedAction? action = null;
        collection.CollectionChanged += (_, e) =>
        {
            changeCount++;
            action = e.Action;
        };

        collection.ReplaceAll(["Alpha", "Gamma"]);

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, action);
        CollectionAssert.AreEqual(new[] { "Alpha", "Gamma" }, collection.ToArray());
    }

    [TestMethod]
    public void SynchronizeWith_ReusesExistingItemsWithoutReset()
    {
        var collection = new BulkObservableCollection<string>();
        collection.AddRange(["Alpha", "Beta", "Gamma"]);
        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        collection.SynchronizeWith(["Gamma", "Alpha", "Delta"]);

        CollectionAssert.AreEqual(new[] { "Gamma", "Alpha", "Delta" }, collection.ToArray());
        Assert.IsFalse(actions.Contains(NotifyCollectionChangedAction.Reset));
        CollectionAssert.Contains(actions, NotifyCollectionChangedAction.Move);
        CollectionAssert.Contains(actions, NotifyCollectionChangedAction.Add);
        CollectionAssert.Contains(actions, NotifyCollectionChangedAction.Remove);
    }
}
