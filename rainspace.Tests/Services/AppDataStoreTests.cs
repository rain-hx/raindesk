using rainspace.Services;
using rainspace.ViewModel;

namespace rainspace.Tests.Services;

[TestClass]
public class AppDataStoreTests
{
    [TestMethod]
    public void Replace_NewInstance_PublishesCurrentStateAfterReplacement()
    {
        var store = new AppDataStore();
        var replacement = new AppData();
        AppData? published = null;
        AppData? currentDuringEvent = null;
        store.StateReplaced += data =>
        {
            published = data;
            currentDuringEvent = store.Current;
        };

        store.Replace(replacement);

        Assert.AreSame(replacement, store.Current);
        Assert.AreSame(replacement, published);
        Assert.AreSame(replacement, currentDuringEvent);
    }

    [TestMethod]
    public void Replace_SameInstance_DoesNotPublishDuplicateEvent()
    {
        var store = new AppDataStore();
        AppData current = store.Current;
        int eventCount = 0;
        store.StateReplaced += _ => eventCount++;

        store.Replace(current);

        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void Write_MutatesCurrentStateWithoutPublishingReplacement()
    {
        var store = new AppDataStore();
        int eventCount = 0;
        store.StateReplaced += _ => eventCount++;

        store.Write(data => data.AppConfig.TrackingUuid = "mutated");

        Assert.AreEqual("mutated", store.Current.AppConfig.TrackingUuid);
        Assert.AreEqual(0, eventCount);
    }
}
