using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System.Collections.ObjectModel;

namespace rainspace.Tests.Services;

[TestClass]
public class AppDataChangeTrackerTests
{
    [TestMethod]
    public void PersistedPropertyChange_RequestsSave()
    {
        var store = new AppDataStore();
        var data = new AppData();
        store.Replace(data);
        var saveService = new RecordingSaveService();
        using var tracker = new AppDataChangeTracker(store, saveService);
        tracker.Start();

        data.AppConfig.CustomIconUrl = "https://example.test/icons.json";

        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void RuntimeOnlyPropertyChange_DoesNotRequestSave()
    {
        var menu = new MenuInfo();
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var saveService = new RecordingSaveService();
        using var tracker = new AppDataChangeTracker(store, saveService);
        tracker.Start();

        menu.IsSelected_NoWrite = true;
        store.Current.AppConfig.SelectedMenuIcons = menu.IconList;

        Assert.AreEqual(0, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void AttachedIcon_IsTrackedOnlyAfterCollectionChange()
    {
        var menu = new MenuInfo();
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var saveService = new RecordingSaveService();
        using var tracker = new AppDataChangeTracker(store, saveService);
        tracker.Start();
        var icon = new IconInfo { Name = "Detached" };

        icon.Name = "Still detached";
        Assert.AreEqual(0, saveService.DebouncedSaveCount);

        menu.IconList.Add(icon);
        icon.Name = "Attached";

        Assert.AreEqual(2, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void StateReplacement_DetachesOldGraphAndTracksNewGraph()
    {
        var oldData = new AppData();
        var newData = new AppData();
        var store = new AppDataStore();
        store.Replace(oldData);
        var saveService = new RecordingSaveService();
        using var tracker = new AppDataChangeTracker(store, saveService);
        tracker.Start();

        store.Replace(newData);
        oldData.AppConfig.CustomIconUrl = "old";
        newData.AppConfig.CustomIconUrl = "new";

        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    private sealed class RecordingSaveService : ISaveService
    {
        public int DebouncedSaveCount { get; private set; }

        public void SaveNow() { }

        public void SaveDebounced() => DebouncedSaveCount++;

        public void CancelPendingSave() { }

        public void Flush() { }
    }
}
