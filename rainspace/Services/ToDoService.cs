using System;
using System.Collections.Generic;
using System.Linq;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Services;

/// <summary>
/// 待办任务服务实现。
/// 使用 AppDataStore 的锁保护线程安全。
/// </summary>
public sealed class ToDoService : IToDoService
{
    private readonly IAppDataProvider _dataProvider;
    private readonly TaskHistoryService _history;
    private readonly ISaveService _saveService;

    public ToDoService(
        IAppDataProvider dataProvider,
        TaskHistoryService history,
        ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    // ── 事件 ──────────────────────────────────────────────────────

    public event Action<ToDoTask>? TaskAdded;
    public event Action<ToDoTask>? TaskUpdated;
    public event Action<string>? TaskDeleted;
    public event Action<ToDoTask>? TaskCompleted;

    // ── 查询 ──────────────────────────────────────────────────────

    public IReadOnlyList<ToDoTask> GetAll()
    {
        return _dataProvider.Read(d => d.Tasks.ToList());
    }

    public IReadOnlyList<ToDoTask> GetActive()
    {
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.InProgress)
            .ToList());
    }

    public IReadOnlyList<ToDoTask> GetCompleted()
    {
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.Status == TaskItemStatus.Completed)
            .OrderByDescending(t => t.CompletedAt)
            .ToList());
    }

    public IReadOnlyList<ToDoTask> GetOverdue()
    {
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.IsOverdue)
            .OrderBy(t => t.DueDate)
            .ToList());
    }

    public IReadOnlyList<ToDoTask> GetToday()
    {
        var today = DateTime.Today;
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.IsActive && t.DueDate.HasValue && t.DueDate.Value.Date == today)
            .OrderBy(t => t.Priority)
            .ToList());
    }

    public IReadOnlyList<ToDoTask> GetByProject(string projectId)
    {
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.ProjectId == projectId && t.IsActive)
            .ToList());
    }

    public IReadOnlyList<ToDoTask> GetByTag(string tag)
    {
        return _dataProvider.Read(d => d.Tasks
            .Where(t => t.Tags.Contains(tag) && t.IsActive)
            .ToList());
    }

    public ToDoTask? GetById(string id)
    {
        return _dataProvider.Read(d => d.Tasks.FirstOrDefault(t => t.Id == id));
    }

    // ── 变更 ──────────────────────────────────────────────────────

    public ToDoTask Add(ToDoTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (string.IsNullOrWhiteSpace(task.Id))
            task.Id = Guid.NewGuid().ToString("N");

        _dataProvider.Write(d =>
        {
            d.Tasks.Add(task);
            SyncLegacyCollections(d);
        });

        // 记录历史
        _history.RecordAdd(task);

        RequestSave();
        TaskAdded?.Invoke(task);
        return task;
    }

    public ToDoInfo AddLegacy(ToDoInfo item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ToDoTask task = Add(ToDoTask.FromLegacy(item));
        return _dataProvider.Read(data => data.ToDoList
            .Concat(data.HiToDoList)
            .FirstOrDefault(todo => string.Equals(todo.Id, task.Id, StringComparison.Ordinal)))
            ?? task.ToLegacy();
    }

    public ToDoInfo? ReplaceLegacy(string existingId, ToDoInfo replacement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(existingId);
        ArgumentNullException.ThrowIfNull(replacement);

        ToDoTask? existing = GetById(existingId);
        if (existing == null)
        {
            return null;
        }

        ToDoTask edited = ToDoTask.FromLegacy(replacement);
        existing.Title = edited.Title;
        existing.Description = edited.Description;
        existing.DueDate = edited.DueDate;
        existing.DueTime = edited.DueTime;
        existing.Recurring = edited.Recurring;
        existing.Status = TaskItemStatus.Pending;
        existing.CompletedAt = null;
        Update(existing);

        return _dataProvider.Read(data => data.ToDoList.FirstOrDefault(todo =>
            string.Equals(todo.Id, existingId, StringComparison.Ordinal)));
    }

    public void Update(ToDoTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        bool changed = false;
        _dataProvider.Write(d =>
        {
            var existing = d.Tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                int index = d.Tasks.IndexOf(existing);
                d.Tasks[index] = task;
                SyncLegacyCollections(d);
                changed = true;
            }
        });

        if (!changed)
        {
            return;
        }

        RequestSave();
        TaskUpdated?.Invoke(task);
    }

    public void Delete(string id)
    {
        ToDoTask? removedTask = null;
        _dataProvider.Write(d =>
        {
            removedTask = d.Tasks.FirstOrDefault(t => t.Id == id);
            if (removedTask == null)
            {
                return;
            }

            d.Tasks.Remove(removedTask);
            SyncLegacyCollections(d);
        });

        if (removedTask == null)
        {
            return;
        }

        _history.RecordDelete(removedTask);
        RequestSave();
        TaskDeleted?.Invoke(id);
    }

    public void ToggleComplete(string id)
    {
        ToDoTask? task = null;
        bool wasCompleted = false;
        _dataProvider.Write(d =>
        {
            task = d.Tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                wasCompleted = task.IsCompleted;

                // 如果是循环任务且即将完成，特殊处理
                if (!task.IsCompleted && task.Recurring?.IsRecurring == true)
                {
                    var (historyTask, nextTask) = RecurringTaskProcessor.ProcessCompletion(task);
                    if (historyTask != null && nextTask != null)
                    {
                        // 用下一个循环实例替换原任务
                        int index = d.Tasks.IndexOf(task);
                        d.Tasks[index] = nextTask;

                        // 添加历史快照
                        d.Tasks.Add(historyTask);

                        task = nextTask; // 返回新任务
                        SyncLegacyCollections(d);
                        return;
                    }
                }

                task.ToggleComplete();
                SyncLegacyCollections(d);
            }
        });

        if (task != null)
        {
            // 记录历史
            _history.RecordToggleComplete(task, wasCompleted);

            RequestSave();
            TaskUpdated?.Invoke(task);
            if (task.IsCompleted)
            {
                TaskCompleted?.Invoke(task);
            }
        }
    }

    public void CompleteAll(IEnumerable<string> ids)
    {
        var idSet = new HashSet<string>(ids);
        var completed = new List<ToDoTask>();

        _dataProvider.Write(d =>
        {
            foreach (var task in d.Tasks.Where(t => idSet.Contains(t.Id) && !t.IsCompleted))
            {
                task.Status = TaskItemStatus.Completed;
                completed.Add(task);
            }

            if (completed.Count > 0)
            {
                SyncLegacyCollections(d);
            }
        });

        if (completed.Count > 0)
        {
            RequestSave();
        }

        foreach (var task in completed)
        {
            TaskUpdated?.Invoke(task);
            TaskCompleted?.Invoke(task);
        }
    }

    public void DeleteAll(IEnumerable<string> ids)
    {
        var idSet = new HashSet<string>(ids);
        var removedIds = new List<string>();

        _dataProvider.Write(d =>
        {
            removedIds = d.Tasks
                .Where(t => idSet.Contains(t.Id))
                .Select(t => t.Id)
                .ToList();

            d.Tasks.RemoveAll(t => idSet.Contains(t.Id));
            if (removedIds.Count > 0)
            {
                SyncLegacyCollections(d);
            }
        });

        if (removedIds.Count > 0)
        {
            RequestSave();
        }

        foreach (var id in removedIds)
        {
            TaskDeleted?.Invoke(id);
        }
    }

    public void MoveToProject(string id, string? projectId)
    {
        ToDoTask? updated = null;
        _dataProvider.Write(d =>
        {
            var task = d.Tasks.FirstOrDefault(t => t.Id == id);
            if (task != null && task.ProjectId != projectId)
            {
                task.ProjectId = projectId;
                updated = task;
            }
        });

        if (updated != null)
        {
            RequestSave();
            TaskUpdated?.Invoke(updated);
        }
    }

    public void Reorder(string id, int newOrder)
    {
        ToDoTask? updated = null;
        _dataProvider.Write(d =>
        {
            var task = d.Tasks.FirstOrDefault(t => t.Id == id);
            if (task != null && task.SortOrder != newOrder)
            {
                task.SortOrder = newOrder;
                updated = task;
            }
        });

        if (updated != null)
        {
            RequestSave();
            TaskUpdated?.Invoke(updated);
        }
    }

    // ── 子任务 ────────────────────────────────────────────────────

    public Subtask AddSubtask(string taskId, string title)
    {
        var subtask = new Subtask { Title = title };
        bool added = false;

        _dataProvider.Write(d =>
        {
            var task = d.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                subtask.SortOrder = task.Subtasks.Count;
                task.Subtasks.Add(subtask);
                added = true;
            }
        });

        if (added)
        {
            RequestSave();
        }

        return subtask;
    }

    public void ToggleSubtask(string taskId, string subtaskId)
    {
        bool changed = false;
        _dataProvider.Write(d =>
        {
            var task = d.Tasks.FirstOrDefault(t => t.Id == taskId);
            var subtask = task?.Subtasks.FirstOrDefault(s => s.Id == subtaskId);
            if (subtask != null)
            {
                subtask.IsCompleted = !subtask.IsCompleted;
                changed = true;
            }
        });

        if (changed)
        {
            RequestSave();
        }
    }

    public void DeleteSubtask(string taskId, string subtaskId)
    {
        bool removed = false;
        _dataProvider.Write(d =>
        {
            var task = d.Tasks.FirstOrDefault(t => t.Id == taskId);
            var subtask = task?.Subtasks.FirstOrDefault(s => s.Id == subtaskId);
            if (task != null && subtask != null)
            {
                removed = task.Subtasks.Remove(subtask);
            }
        });

        if (removed)
        {
            RequestSave();
        }
    }

    // ── 私有方法 ──────────────────────────────────────────────────

    /// <summary>在淘汰旧待办模型前，始终从 Tasks 重建兼容集合，避免删除项被重新迁移。</summary>
    private static void SyncLegacyCollections(AppData data)
    {
        try
        {
            var active = new List<ToDoInfo>();
            var history = new List<ToDoInfo>();
            foreach (ToDoTask task in data.Tasks)
            {
                var legacy = task.ToLegacy();
                if (task.IsCompleted)
                {
                    history.Add(legacy);
                }
                else
                {
                    active.Add(legacy);
                }
            }

            data.ToDoList.Clear();
            foreach (ToDoInfo item in active)
            {
                data.ToDoList.Add(item);
            }

            data.HiToDoList.Clear();
            foreach (ToDoInfo item in history)
            {
                data.HiToDoList.Add(item);
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "重建旧待办兼容集合失败");
        }
    }

    private void RequestSave()
    {
        _saveService.SaveDebounced();
    }
}
