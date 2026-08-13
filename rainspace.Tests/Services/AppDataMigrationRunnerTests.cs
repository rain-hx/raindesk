using Newtonsoft.Json;
using rainspace.Services;
using rainspace.Services.Migrations;
using rainspace.ViewModel;
using System.Collections.ObjectModel;

namespace rainspace.Tests.Services;

[TestClass]
public class AppDataMigrationRunnerTests
{
    [TestMethod]
    public void AppData_NewInstance_UsesCurrentSchemaVersion()
    {
        var data = new AppData();

        Assert.AreEqual(AppDataSchema.CurrentVersion, data.SchemaVersion);
    }

    [TestMethod]
    public void Deserialize_JsonWithoutSchemaVersion_IsLegacyVersion()
    {
        var data = JsonConvert.DeserializeObject<AppData>("{\"MenuList\":[]}");

        Assert.IsNotNull(data);
        Assert.AreEqual(AppDataSchema.LegacyVersion, data.SchemaVersion);
    }

    [TestMethod]
    public void MigrateToCurrent_LegacyTodoData_UpdatesVersionAndCollections()
    {
        var data = new AppData
        {
            SchemaVersion = AppDataSchema.LegacyVersion,
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new() { Title = "Legacy task" }
            }
        };

        AppDataMigrationResult result = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Changed);
        Assert.AreEqual(AppDataSchema.LegacyVersion, result.OriginalVersion);
        Assert.AreEqual(AppDataSchema.CurrentVersion, result.FinalVersion);
        Assert.AreEqual(AppDataSchema.LegacyVersion, data.SchemaVersion);
        Assert.AreEqual(0, data.Tasks.Count);
        Assert.AreNotSame(data, result.Data);
        Assert.AreEqual(AppDataSchema.CurrentVersion, result.Data.SchemaVersion);
        Assert.AreEqual(1, result.Data.Tasks.Count);
        Assert.AreEqual("Legacy task", result.Data.Tasks[0].Title);
        Assert.AreEqual(1, result.Data.ToDoList.Count);
        Assert.AreEqual(result.Data.Tasks[0].Id, result.Data.ToDoList[0].Id);
    }

    [TestMethod]
    public void MigrateToCurrent_CurrentData_IsIdempotent()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new() { Id = "task", Title = "Current task" }
            }
        };

        AppDataMigrationResult first = AppDataMigrationRunner.Default.MigrateToCurrent(data);
        AppDataMigrationResult second = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsTrue(first.Success);
        Assert.IsFalse(first.Changed);
        Assert.IsTrue(second.Success);
        Assert.IsFalse(second.Changed);
        Assert.AreSame(data, first.Data);
        Assert.AreSame(data, second.Data);
        Assert.AreEqual(1, data.Tasks.Count);
    }

    [TestMethod]
    public void MigrateToCurrent_VersionOneData_AssignsStableUniqueMenuIds()
    {
        var data = new AppData
        {
            SchemaVersion = 1,
            MenuList = new ObservableCollection<MenuInfo>
            {
                new() { MenuId = "duplicate", MenuName = "One" },
                new() { MenuId = "duplicate", MenuName = "Two" },
                new() { MenuId = "", MenuName = "Three" }
            }
        };

        AppDataMigrationResult result = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual(1, data.SchemaVersion);
        Assert.AreEqual(AppDataSchema.CurrentVersion, result.Data.SchemaVersion);
        Assert.AreEqual(3, result.Data.MenuList.Select(menu => menu.MenuId).Distinct().Count());
        Assert.IsFalse(result.Data.MenuList.Any(menu => string.IsNullOrWhiteSpace(menu.MenuId)));
    }

    [TestMethod]
    public void MigrateToCurrent_VersionTwoData_AssignsTaskIdsToLegacyViews()
    {
        var data = new AppData
        {
            SchemaVersion = 2,
            Tasks = new List<ToDoTask>
            {
                new() { Id = "", Title = "One" },
                new() { Id = "duplicate", Title = "Two" },
                new() { Id = "duplicate", Title = "Three", Status = TaskItemStatus.Completed }
            }
        };

        AppDataMigrationResult result = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual(3, result.Data.Tasks.Select(task => task.Id).Distinct().Count());
        Assert.IsFalse(result.Data.Tasks.Any(task => string.IsNullOrWhiteSpace(task.Id)));
        CollectionAssert.AreEquivalent(
            result.Data.Tasks.Select(task => task.Id).ToArray(),
            result.Data.ToDoList.Concat(result.Data.HiToDoList).Select(todo => todo.Id).ToArray());
    }

    [TestMethod]
    public void MigrateToCurrent_FutureVersion_IsRejectedWithoutDowngrade()
    {
        int futureVersion = AppDataSchema.CurrentVersion + 1;
        var data = new AppData { SchemaVersion = futureVersion };

        AppDataMigrationResult result = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.Changed);
        Assert.AreSame(data, result.Data);
        Assert.AreEqual(futureVersion, data.SchemaVersion);
        StringAssert.Contains(result.Error, "高于当前应用支持的版本");
    }

    [TestMethod]
    public void MigrateToCurrent_MigrationThrows_DoesNotMutateOriginalData()
    {
        var data = new AppData { SchemaVersion = AppDataSchema.LegacyVersion };
        var runner = new AppDataMigrationRunner(new IAppDataMigration[] { new ThrowingMigration() });

        AppDataMigrationResult result = runner.MigrateToCurrent(data);

        Assert.IsFalse(result.Success);
        Assert.AreSame(data, result.Data);
        Assert.AreEqual(AppDataSchema.LegacyVersion, data.SchemaVersion);
        Assert.AreEqual(0, data.Tasks.Count);
        StringAssert.Contains(result.Error, "测试迁移失败");
    }

    [TestMethod]
    public void MigrateToCurrent_DuplicateTaskIds_AreRejected()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new() { Id = "duplicate" },
                new() { Id = "duplicate" }
            }
        };

        AppDataMigrationResult result = AppDataMigrationRunner.Default.MigrateToCurrent(data);

        Assert.IsFalse(result.Success);
        Assert.AreSame(data, result.Data);
        StringAssert.Contains(result.Error, "任务集合包含重复 ID");
    }

    private sealed class ThrowingMigration : IAppDataMigration
    {
        public int FromVersion => AppDataSchema.LegacyVersion;

        public int ToVersion => AppDataSchema.CurrentVersion;

        public void Apply(AppData data)
        {
            data.Tasks.Add(new ToDoTask { Title = "只应存在于迁移副本" });
            throw new InvalidOperationException("测试迁移失败");
        }
    }
}
