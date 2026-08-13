using Newtonsoft.Json;
using raindesk.Features.ImportExport.Contracts;
using raindesk.Features.ImportExport.Infrastructure;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Services.Migrations;
using raindesk.ViewModel;
using System.Collections.ObjectModel;

namespace raindesk.Tests;

[TestClass]
public class BackupServiceTests
{
    [TestMethod]
    public void DocumentReader_RelinksAllGroupsFolderMembersAfterJsonRoundTrip()
    {
        string sourcePath = TempPath("all-groups.json");
        var member = new IconInfo { IconId = "member", Name = "Member" };
        var outside = new IconInfo { IconId = "outside", Name = "Outside" };
        IconInfo folder = ShortcutFolderService.CreateFolder("Owned");
        folder.IconId = "folder";
        folder.FolderItems.Add(member);
        var source = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                new()
                {
                    MenuId = "group",
                    IconList = new ObservableCollection<IconInfo> { member, outside }
                }
            }
        };
        source.AppConfig.AllGroupsIconList.Add(folder);
        File.WriteAllText(sourcePath, JsonConvert.SerializeObject(source));

        try
        {
            AppData? loaded = new BackupDocumentReader().Read(sourcePath);

            Assert.IsNotNull(loaded);
            IconInfo canonical = loaded.MenuList[0].IconList[0];
            IconInfo loadedFolder = loaded.AppConfig.AllGroupsIconList.Single();
            Assert.AreSame(canonical, loadedFolder.FolderItems.Single());
            CollectionAssert.AreEqual(
                new[] { loadedFolder, loaded.MenuList[0].IconList[1] },
                AllGroupsShortcutViewService.BuildDefaultFlatOrder(
                    loaded.MenuList,
                    loaded.AppConfig.AllGroupsIconList));
        }
        finally
        {
            DeleteIfExists(sourcePath);
        }
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Import_LegacyData_MigratesAndCommitsCurrentSchema()
    {
        string sourcePath = TempPath("source.bak");
        string primaryPath = TempPath("primary.json");
        var oldData = new AppData { AppConfig = new AppConfig { TrackingUuid = "old" } };
        var legacyData = new AppData
        {
            SchemaVersion = AppDataSchema.LegacyVersion,
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new() { Title = "Legacy task" }
            }
        };
        File.WriteAllText(sourcePath, JsonConvert.SerializeObject(legacyData));
        var store = new AppDataStore();
        store.Replace(oldData);
        var saveService = new RecordingSaveService();
        var service = new BackupService(store, saveService, new BackupDocumentReader(), primaryPath);

        try
        {
            BackupOperationResult result = await service.ImportAsync(sourcePath);

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreNotSame(oldData, store.Current);
            Assert.AreEqual(AppDataSchema.CurrentVersion, store.Current.SchemaVersion);
            Assert.AreEqual("Legacy task", store.Current.Tasks.Single().Title);
            Assert.AreEqual(1, saveService.CancelCount);

            var persisted = JsonConvert.DeserializeObject<AppData>(File.ReadAllText(primaryPath));
            Assert.IsNotNull(persisted);
            Assert.AreEqual(AppDataSchema.CurrentVersion, persisted.SchemaVersion);
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteIfExists(primaryPath);
        }
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Import_WriteFailure_RestoresPreviousState()
    {
        string sourcePath = TempPath("source.bak");
        var oldData = new AppData { AppConfig = new AppConfig { TrackingUuid = "keep-me" } };
        var imported = new AppData { AppConfig = new AppConfig { TrackingUuid = "new-data" } };
        File.WriteAllText(sourcePath, JsonConvert.SerializeObject(imported));
        var store = new AppDataStore();
        store.Replace(oldData);
        var service = new BackupService(store, new RecordingSaveService(), new BackupDocumentReader(), "\0");

        try
        {
            BackupOperationResult result = await service.ImportAsync(sourcePath);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(BackupErrorCode.WriteFailed, result.ErrorCode);
            Assert.AreSame(oldData, store.Current);
            Assert.AreEqual("keep-me", store.Current.AppConfig.TrackingUuid);
        }
        finally
        {
            DeleteIfExists(sourcePath);
        }
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Import_FutureVersion_IsRejectedWithoutReplacingState()
    {
        string sourcePath = TempPath("source.bak");
        var oldData = new AppData();
        var future = new AppData { SchemaVersion = AppDataSchema.CurrentVersion + 1 };
        File.WriteAllText(sourcePath, JsonConvert.SerializeObject(future));
        var store = new AppDataStore();
        store.Replace(oldData);
        var service = new BackupService(
            store,
            new RecordingSaveService(),
            new BackupDocumentReader(),
            TempPath("primary.json"));

        try
        {
            BackupOperationResult result = await service.ImportAsync(sourcePath);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(BackupErrorCode.UnsupportedVersion, result.ErrorCode);
            Assert.AreSame(oldData, store.Current);
        }
        finally
        {
            DeleteIfExists(sourcePath);
        }
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Export_PreCancelledOperation_ReturnsCancelledResult()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var service = new BackupService(
            store,
            new RecordingSaveService(),
            new BackupDocumentReader(),
            TempPath("primary.json"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        BackupOperationResult result = await service.ExportAsync(
            TempPath("export.bak"),
            cancellation.Token);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BackupErrorCode.Cancelled, result.ErrorCode);
    }

    private static string TempPath(string suffix) => Path.Combine(
        Path.GetTempPath(),
        $"raindesk-backup-{Guid.NewGuid():N}-{suffix}");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class RecordingSaveService : ISaveService
    {
        public int CancelCount { get; private set; }

        public void SaveNow() { }

        public void SaveDebounced() { }

        public void CancelPendingSave() => CancelCount++;

        public void Flush() { }
    }
}
