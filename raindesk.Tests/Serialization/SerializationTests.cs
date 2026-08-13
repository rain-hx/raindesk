using Newtonsoft.Json;
using raindesk.ViewModel;

namespace raindesk.Tests.Serialization;

[TestClass]
public class SerializationTests
{
    [TestMethod]
    public void ToDoTask_ShouldSerializeCorrectly()
    {
        var task = new ToDoTask
        {
            Id = "12345678-1234-4234-8234-123456789abc",
            Title = "测试任务",
            Description = "测试描述",
            Priority = TaskPriority.High,
            Status = TaskItemStatus.Pending,
            DueDate = new DateTime(2026, 6, 11),
            DueTime = "10:00",
            CreatedAt = new DateTime(2026, 6, 10),
            Tags = new List<string> { "工作", "重要" },
            SortOrder = 1
        };

        string json = JsonConvert.SerializeObject(task);
        var deserialized = JsonConvert.DeserializeObject<ToDoTask>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(task.Id, deserialized!.Id);
        Assert.AreEqual(task.Title, deserialized.Title);
        Assert.AreEqual(task.Description, deserialized.Description);
        Assert.AreEqual(task.Priority, deserialized.Priority);
        Assert.AreEqual(task.Status, deserialized.Status);
        Assert.AreEqual(task.DueDate, deserialized.DueDate);
        Assert.AreEqual(task.DueTime, deserialized.DueTime);
        Assert.AreEqual(task.CreatedAt, deserialized.CreatedAt);
        Assert.AreEqual(task.SortOrder, deserialized.SortOrder);
        CollectionAssert.AreEqual(task.Tags, deserialized.Tags);
    }

    [TestMethod]
    public void ToDoTask_ComputedProperties_ShouldNotSerialize()
    {
        var task = new ToDoTask
        {
            Title = "过期任务",
            DueDate = new DateTime(2020, 1, 1),
            Status = TaskItemStatus.Pending
        };

        string json = JsonConvert.SerializeObject(task);

        Assert.IsFalse(json.Contains("IsOverdue"));
        Assert.IsFalse(json.Contains("IsCompleted"));
        Assert.IsFalse(json.Contains("IsActive"));
        Assert.IsFalse(json.Contains("DisplayTitle"));
        Assert.IsFalse(json.Contains("PriorityColor"));
    }

    [TestMethod]
    public void ToDoTask_DefaultValues_ShouldSerialize()
    {
        var task = new ToDoTask { Title = "最小任务" };

        string json = JsonConvert.SerializeObject(task);
        var deserialized = JsonConvert.DeserializeObject<ToDoTask>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual("最小任务", deserialized!.Title);
        Assert.AreEqual(TaskPriority.Medium, deserialized.Priority);
        Assert.AreEqual(TaskItemStatus.Pending, deserialized.Status);
        Assert.IsNotNull(deserialized.Tags);
        Assert.IsNotNull(deserialized.Subtasks);
        Assert.IsNotNull(deserialized.Recurring);
    }

    [TestMethod]
    public void AppData_ShouldSerializeWithTasks()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new ToDoTask { Title = "任务1" },
                new ToDoTask { Title = "任务2" }
            }
        };

        string json = JsonConvert.SerializeObject(data);
        var deserialized = JsonConvert.DeserializeObject<AppData>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(2, deserialized!.Tasks.Count);
        Assert.AreEqual("任务1", deserialized.Tasks[0].Title);
        Assert.AreEqual("任务2", deserialized.Tasks[1].Title);
    }

    [TestMethod]
    public void AppData_ShouldPreserveBothOldAndNewCollections()
    {
        var data = new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new ToDoTask { Title = "新任务" }
            }
        };

        string json = JsonConvert.SerializeObject(data);
        var deserialized = JsonConvert.DeserializeObject<AppData>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(1, deserialized!.Tasks.Count);
        Assert.IsNotNull(deserialized.ToDoList);
        Assert.IsNotNull(deserialized.HiToDoList);
    }

    [TestMethod]
    public void ToDoTask_Roundtrip_ShouldPreserveId()
    {
        var original = new ToDoTask { Title = "ID测试" };
        string originalId = original.Id;

        string json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<ToDoTask>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(originalId, deserialized!.Id);
        Assert.AreNotEqual(Guid.Empty.ToString("N"), deserialized.Id);
    }
}
