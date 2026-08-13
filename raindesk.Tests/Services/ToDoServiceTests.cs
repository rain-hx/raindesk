using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class ToDoServiceTests
{
    [TestMethod]
    public void CompleteAll_CompletedTasks_RaisesTaskUpdatedForEachChangedTask()
    {
        var store = new AppDataStore();
        var first = new ToDoTask { Id = "first", Title = "First task" };
        var second = new ToDoTask { Id = "second", Title = "Second task" };
        store.Replace(new AppData { Tasks = new List<ToDoTask> { first, second } });
        var service = CreateService(store);
        var updatedIds = new List<string>();

        service.TaskUpdated += task => updatedIds.Add(task.Id);

        service.CompleteAll(new[] { "first", "second" });

        CollectionAssert.AreEquivalent(new[] { "first", "second" }, updatedIds);
    }

    [TestMethod]
    public void MoveToProject_ExistingTask_RaisesTaskUpdated()
    {
        var store = new AppDataStore();
        var task = new ToDoTask { Id = "task", Title = "Move me", ProjectId = "old" };
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var service = CreateService(store);
        var updatedIds = new List<string>();

        service.TaskUpdated += updated => updatedIds.Add(updated.Id);

        service.MoveToProject("task", "new");

        CollectionAssert.AreEqual(new[] { "task" }, updatedIds);
        Assert.AreEqual("new", task.ProjectId);
    }

    [TestMethod]
    public void MoveToProject_SameProject_DoesNotRaiseTaskUpdated()
    {
        var store = new AppDataStore();
        var task = new ToDoTask { Id = "task", Title = "Stay put", ProjectId = "same" };
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var service = CreateService(store);
        int updateCount = 0;

        service.TaskUpdated += _ => updateCount++;

        service.MoveToProject("task", "same");

        Assert.AreEqual(0, updateCount);
    }

    [TestMethod]
    public void Reorder_ExistingTask_RaisesTaskUpdated()
    {
        var store = new AppDataStore();
        var task = new ToDoTask { Id = "task", Title = "Reorder me", SortOrder = 1 };
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var service = CreateService(store);
        var updatedIds = new List<string>();

        service.TaskUpdated += updated => updatedIds.Add(updated.Id);

        service.Reorder("task", 12);

        CollectionAssert.AreEqual(new[] { "task" }, updatedIds);
        Assert.AreEqual(12, task.SortOrder);
    }

    [TestMethod]
    public void Reorder_SameOrder_DoesNotRaiseTaskUpdated()
    {
        var store = new AppDataStore();
        var task = new ToDoTask { Id = "task", Title = "Already there", SortOrder = 7 };
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var service = CreateService(store);
        int updateCount = 0;

        service.TaskUpdated += _ => updateCount++;

        service.Reorder("task", 7);

        Assert.AreEqual(0, updateCount);
    }

    [TestMethod]
    public void Update_MissingTask_DoesNotRaiseTaskUpdated()
    {
        var store = new AppDataStore();
        store.Replace(new AppData { Tasks = new List<ToDoTask>() });
        var service = CreateService(store);
        int updateCount = 0;

        service.TaskUpdated += _ => updateCount++;

        service.Update(new ToDoTask { Id = "missing", Title = "Missing" });

        Assert.AreEqual(0, updateCount);
    }

    [TestMethod]
    public void Delete_MissingTask_DoesNotRaiseTaskDeleted()
    {
        var store = new AppDataStore();
        store.Replace(new AppData { Tasks = new List<ToDoTask>() });
        var service = CreateService(store);
        int deleteCount = 0;

        service.TaskDeleted += _ => deleteCount++;

        service.Delete("missing");

        Assert.AreEqual(0, deleteCount);
    }

    [TestMethod]
    public void DeleteAll_MixedIds_RaisesTaskDeletedOnlyForRemovedTasks()
    {
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            Tasks = new List<ToDoTask>
            {
                new() { Id = "existing", Title = "Existing" }
            }
        });
        var service = CreateService(store);
        var deletedIds = new List<string>();

        service.TaskDeleted += id => deletedIds.Add(id);

        service.DeleteAll(new[] { "existing", "missing" });

        CollectionAssert.AreEqual(new[] { "existing" }, deletedIds);
    }

    [TestMethod]
    public void Delete_LastTask_ClearsLegacyCollectionsAndRequestsSave()
    {
        var task = new ToDoTask { Id = "task", Title = "Do not resurrect" };
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            Tasks = new List<ToDoTask> { task },
            ToDoList = new System.Collections.ObjectModel.ObservableCollection<ToDoInfo>
            {
                task.ToLegacy()
            }
        });
        var saveService = new RecordingSaveService();
        var service = CreateService(store, saveService);

        service.Delete(task.Id);

        Assert.AreEqual(0, store.Current.Tasks.Count);
        Assert.AreEqual(0, store.Current.ToDoList.Count);
        Assert.AreEqual(0, store.Current.HiToDoList.Count);
        Assert.IsFalse(ToDoMigrationService.NeedsMigration(store.Current));
        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void DeleteSubtask_ExistingSubtask_RequestsSave()
    {
        var subtask = new Subtask { Id = "subtask", Title = "Child" };
        var task = new ToDoTask { Id = "task", Title = "Parent" };
        task.Subtasks.Add(subtask);
        var store = new AppDataStore();
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var saveService = new RecordingSaveService();
        var service = CreateService(store, saveService);

        service.DeleteSubtask(task.Id, subtask.Id);

        Assert.AreEqual(0, task.Subtasks.Count);
        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void Update_ExistingTask_RequestsExactlyOneSaveAtServiceBoundary()
    {
        var task = new ToDoTask { Id = "task", Title = "Before" };
        var store = new AppDataStore();
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var saveService = new RecordingSaveService();
        var service = CreateService(store, saveService);

        task.Title = "After";
        service.Update(task);

        Assert.AreEqual("After", store.Current.Tasks.Single().Title);
        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void AddLegacy_CreatesCanonicalTaskAndReturnsMatchingStableId()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var saveService = new RecordingSaveService();
        var service = CreateService(store, saveService);

        ToDoInfo legacy = service.AddLegacy(new ToDoInfo
        {
            Title = "Legacy editor task",
            ExeTime = "2026-07-18 09:30:00"
        });

        Assert.AreEqual(1, store.Current.Tasks.Count);
        Assert.AreEqual(store.Current.Tasks[0].Id, legacy.Id);
        Assert.AreEqual(store.Current.ToDoList[0].Id, legacy.Id);
        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    [TestMethod]
    public void ReplaceLegacy_ReactivatesCompletedTaskWithoutChangingId()
    {
        var task = new ToDoTask
        {
            Id = "task",
            Title = "Before",
            Status = TaskItemStatus.Completed,
            CompletedAt = DateTime.Now
        };
        var store = new AppDataStore();
        store.Replace(new AppData { Tasks = new List<ToDoTask> { task } });
        var saveService = new RecordingSaveService();
        var service = CreateService(store, saveService);

        ToDoInfo? result = service.ReplaceLegacy("task", new ToDoInfo
        {
            Id = "task",
            Title = "After"
        });

        Assert.IsNotNull(result);
        Assert.AreEqual("task", result.Id);
        Assert.AreEqual("After", task.Title);
        Assert.AreEqual(TaskItemStatus.Pending, task.Status);
        Assert.IsNull(task.CompletedAt);
        Assert.AreEqual(1, store.Current.ToDoList.Count);
        Assert.AreEqual(0, store.Current.HiToDoList.Count);
        Assert.AreEqual(1, saveService.DebouncedSaveCount);
    }

    private static ToDoService CreateService(
        AppDataStore store,
        RecordingSaveService? saveService = null)
    {
        return new ToDoService(
            store,
            new TaskHistoryService(),
            saveService ?? new RecordingSaveService());
    }

    private sealed class RecordingSaveService : ISaveService
    {
        public int DebouncedSaveCount { get; private set; }

        public void SaveNow()
        {
        }

        public void SaveDebounced()
        {
            DebouncedSaveCount++;
        }

        public void CancelPendingSave()
        {
        }

        public void Flush()
        {
        }
    }
}
