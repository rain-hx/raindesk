using System.Collections.ObjectModel;
using System.Linq;
using raindesk.Constant;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Integration;

[TestClass]
public class ToDoSystemIntegrationTests
{
    [TestMethod]
    public void FullWorkflow_MigrateAndUseNewSystem()
    {
        // Arrange - 创建旧数据
        var oldData = new AppData
        {
            ToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo
                {
                    Title = "待办任务1",
                    ExeTime = "2026-06-11 10:00:00",
                    ExecType = TodoTaskExecType.SET_TIME
                },
                new ToDoInfo
                {
                    Title = "待办任务2",
                    ExeTime = "2026-06-12 14:00:00",
                    ExecType = TodoTaskExecType.CRON,
                    Cron = "0 0 9 * * ?"
                }
            },
            HiToDoList = new ObservableCollection<ToDoInfo>
            {
                new ToDoInfo
                {
                    Title = "已完成任务",
                    DoneTime = "2026-06-10 15:00:00"
                }
            }
        };

        // Act - 执行迁移
        Assert.IsTrue(ToDoMigrationService.NeedsMigration(oldData));
        var tasks = ToDoMigrationService.Migrate(oldData);

        // Assert - 验证迁移结果
        Assert.AreEqual(3, tasks.Count);

        var pendingTasks = tasks.Where(t => t.Status == TaskItemStatus.Pending).ToList();
        var completedTasks = tasks.Where(t => t.Status == TaskItemStatus.Completed).ToList();

        Assert.AreEqual(2, pendingTasks.Count);
        Assert.AreEqual(1, completedTasks.Count);

        // 验证循环任务配置
        var cronTask = pendingTasks.FirstOrDefault(t => t.Title == "待办任务2");
        Assert.IsNotNull(cronTask);
        Assert.IsTrue(cronTask.Recurring.IsRecurring);
        Assert.AreEqual(RecurringType.Cron, cronTask.Recurring.Type);
    }
}
