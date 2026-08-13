using System.Collections.ObjectModel;
using raindesk.Constant;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class ToDoMigrationServiceTests
{
    [TestMethod]
    public void NeedsMigration_WithOldTasks_ShouldReturnTrue()
    {
        var data = new AppData
        {
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo { Title = "测试任务" }
            }
        };

        bool result = ToDoMigrationService.NeedsMigration(data);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void NeedsMigration_WithNewTasks_ShouldReturnFalse()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new ToDoTask { Title = "新任务" }
            }
        };

        bool result = ToDoMigrationService.NeedsMigration(data);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void NeedsMigration_WithNullData_ShouldReturnFalse()
    {
        bool result = ToDoMigrationService.NeedsMigration(null!);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void NeedsMigration_WithEmptyCollections_ShouldReturnFalse()
    {
        var data = new AppData();

        bool result = ToDoMigrationService.NeedsMigration(data);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Migrate_ShouldConvertOldToNew()
    {
        var data = new AppData
        {
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo
                {
                    Title = "测试任务",
                    ExeTime = "2026-06-11 10:00:00",
                    ExecType = TodoTaskExecType.SET_TIME
                }
            }
        };

        var tasks = ToDoMigrationService.Migrate(data);

        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual("测试任务", tasks[0].Title);
        Assert.AreEqual(TaskItemStatus.Pending, tasks[0].Status);
        Assert.IsNotNull(tasks[0].DueDate);
        Assert.AreEqual(new DateTime(2026, 6, 11), tasks[0].DueDate!.Value.Date);
        Assert.AreEqual("10:00", tasks[0].DueTime);
    }

    [TestMethod]
    public void Migrate_HiToDoList_ShouldMarkCompleted()
    {
        var data = new AppData
        {
            HiToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo
                {
                    Title = "已完成任务",
                    DoneTime = "2026-06-10 15:30:00"
                }
            }
        };

        var tasks = ToDoMigrationService.Migrate(data);

        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual(TaskItemStatus.Completed, tasks[0].Status);
        Assert.IsNotNull(tasks[0].CompletedAt);
    }

    [TestMethod]
    public void Migrate_MixedLists_ShouldMergeBoth()
    {
        var data = new AppData
        {
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo { Title = "活跃任务" }
            },
            HiToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo { Title = "历史任务", DoneTime = "2026-06-01 00:00:00" }
            }
        };

        var tasks = ToDoMigrationService.Migrate(data);

        Assert.AreEqual(2, tasks.Count);
        Assert.IsTrue(tasks.Any(t => t.Title == "活跃任务" && t.Status == TaskItemStatus.Pending));
        Assert.IsTrue(tasks.Any(t => t.Title == "历史任务" && t.Status == TaskItemStatus.Completed));
    }

    [TestMethod]
    public void Migrate_CronTask_ShouldPreserveRecurring()
    {
        var data = new AppData
        {
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo
                {
                    Title = "每日站会",
                    ExecType = TodoTaskExecType.CRON,
                    Cron = "0 9 * * MON-FRI"
                }
            }
        };

        var tasks = ToDoMigrationService.Migrate(data);

        Assert.AreEqual(1, tasks.Count);
        Assert.IsTrue(tasks[0].Recurring.IsRecurring);
        Assert.AreEqual(RecurringType.Cron, tasks[0].Recurring.Type);
        Assert.AreEqual("0 9 * * MON-FRI", tasks[0].Recurring.Expression);
    }

    [TestMethod]
    public void Migrate_WithNullData_ShouldReturnEmpty()
    {
        var tasks = ToDoMigrationService.Migrate(null!);

        Assert.AreEqual(0, tasks.Count);
    }

    [TestMethod]
    public void FromLegacy_ShouldMapTitleAndDescription()
    {
        var legacy = new ToDoInfo
        {
            Title = "买牛奶",
            Msg = "去超市买两盒牛奶"
        };

        var task = ToDoTask.FromLegacy(legacy);

        Assert.AreEqual("买牛奶", task.Title);
        Assert.AreEqual("去超市买两盒牛奶", task.Description);
        Assert.AreEqual(TaskPriority.Medium, task.Priority);
    }

    [TestMethod]
    public void ToLegacy_ShouldMapBackCorrectly()
    {
        var task = new ToDoTask
        {
            Title = "写报告",
            Description = "季度总结",
            DueDate = new DateTime(2026, 6, 15),
            DueTime = "14:30"
        };

        var legacy = task.ToLegacy();

        Assert.AreEqual("写报告", legacy.Title);
        Assert.AreEqual("季度总结", legacy.Description);
        Assert.AreEqual("2026-06-15 14:30:00", legacy.ExeTime);
    }

    [TestMethod]
    public void SyncBackToLegacy_ShouldSplitByStatus()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new ToDoTask { Title = "待办", Status = TaskItemStatus.Pending },
                new ToDoTask { Title = "完成", Status = TaskItemStatus.Completed }
            }
        };

        ToDoMigrationService.SyncBackToLegacy(data);

        Assert.AreEqual(1, data.ToDoList.Count);
        Assert.AreEqual("待办", data.ToDoList[0].Title);
        Assert.AreEqual(1, data.HiToDoList.Count);
        Assert.AreEqual("完成", data.HiToDoList[0].Title);
    }
}
