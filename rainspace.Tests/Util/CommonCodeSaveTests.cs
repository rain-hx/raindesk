using Newtonsoft.Json;
using rainspace.Features.ImportExport.Contracts;
using rainspace.Features.ImportExport.Infrastructure;
using rainspace.Services;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Tests.Util;

[TestClass]
public class CommonCodeSaveTests
{
    [TestMethod]
    public void CancelDebouncedSave_PreventsStaleDataFromBeingWritten()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"geekdesk-cancel-save-{Guid.NewGuid():N}.json");
        var appData = new AppData();

        try
        {
            CommonCode.SaveAppDataDebounced(appData, filePath);
            CommonCode.CancelDebouncedSave();

            Thread.Sleep(700);

            Assert.IsFalse(File.Exists(filePath));
        }
        finally
        {
            CommonCode.CancelDebouncedSave();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public void SaveAppData_ReturnsFalseWhenTheDestinationCannotBeWritten()
    {
        bool saved = CommonCode.SaveAppData(new AppData(), "\0");

        Assert.IsFalse(saved);
    }

    [TestMethod]
    public void FlushDebouncedSave_WritesTheProvidedAppData()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"geekdesk-save-{Guid.NewGuid():N}.json");
        var appData = new AppData
        {
            AppConfig = new AppConfig
            {
                TrackingUuid = "provided-data"
            }
        };

        try
        {
            CommonCode.FlushDebouncedSave(appData, filePath);

            string json = File.ReadAllText(filePath);
            var saved = JsonConvert.DeserializeObject<AppData>(json);

            Assert.IsNotNull(saved);
            Assert.AreEqual("provided-data", saved.AppConfig.TrackingUuid);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task BackupService_ExportsInjectedStore()
    {
        string destinationPath = Path.Combine(
            Path.GetTempPath(),
            $"geekdesk-export-{Guid.NewGuid():N}.bak");
        string primaryPath = Path.Combine(
            Path.GetTempPath(),
            $"geekdesk-primary-{Guid.NewGuid():N}.json");
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            AppConfig = new AppConfig
            {
                TrackingUuid = "injected-store"
            }
        });

        try
        {
            using var saveService = new SaveService(store, primaryPath);
            var backupService = new BackupService(
                store,
                saveService,
                new BackupDocumentReader(),
                primaryPath);

            BackupOperationResult result = await backupService.ExportAsync(destinationPath);

            Assert.IsTrue(result.Success);
            string json = File.ReadAllText(destinationPath);
            var exportedData = JsonConvert.DeserializeObject<AppData>(json);
            Assert.IsNotNull(exportedData);
            Assert.AreEqual("injected-store", exportedData.AppConfig.TrackingUuid);
        }
        finally
        {
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            if (File.Exists(primaryPath)) File.Delete(primaryPath);
        }
    }
}
