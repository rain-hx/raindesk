using raindesk.ViewModel;

namespace raindesk.Tests.ViewModel;

[TestClass]
public class ToDoTaskTests
{
    [TestMethod]
    public void SettingSameTitle_DoesNotRaisePropertyChanged()
    {
        var task = new ToDoTask { Title = "Plan sprint" };
        int changeCount = 0;
        task.PropertyChanged += (_, _) => changeCount++;

        task.Title = "Plan sprint";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentPriority_RaisesPriorityAndColorChanges()
    {
        var task = new ToDoTask();
        var propertyNames = new List<string?>();
        task.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        task.Priority = TaskPriority.High;

        CollectionAssert.AreEqual(
            new[] { nameof(ToDoTask.Priority), nameof(ToDoTask.PriorityColor) },
            propertyNames);
    }

    [TestMethod]
    public void SettingSameStatus_DoesNotRaisePropertyChanged()
    {
        var task = new ToDoTask { Status = TaskItemStatus.InProgress };
        int changeCount = 0;
        task.PropertyChanged += (_, _) => changeCount++;

        task.Status = TaskItemStatus.InProgress;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingStatusToCompleted_StillSetsCompletedAt()
    {
        var task = new ToDoTask();

        task.Status = TaskItemStatus.Completed;

        Assert.IsTrue(task.IsCompleted);
        Assert.IsNotNull(task.CompletedAt);
    }

    [TestMethod]
    public void SettingDueDateToOverdue_RaisesDisplayTitleChanged()
    {
        var task = new ToDoTask { Title = "Submit report" };
        var propertyNames = new List<string?>();
        task.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        task.DueDate = DateTime.Today.AddDays(-1);

        CollectionAssert.Contains(propertyNames, nameof(ToDoTask.IsOverdue));
        CollectionAssert.Contains(propertyNames, nameof(ToDoTask.DisplayTitle));
        Assert.AreEqual("⚠ Submit report", task.DisplayTitle);
    }

    [TestMethod]
    public void CompletingOverdueTask_RaisesDisplayTitleChanged()
    {
        var task = new ToDoTask
        {
            Title = "Submit report",
            DueDate = DateTime.Today.AddDays(-1)
        };
        var propertyNames = new List<string?>();
        task.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        task.Status = TaskItemStatus.Completed;

        CollectionAssert.Contains(propertyNames, nameof(ToDoTask.IsOverdue));
        CollectionAssert.Contains(propertyNames, nameof(ToDoTask.DisplayTitle));
        Assert.AreEqual("Submit report", task.DisplayTitle);
    }
}
