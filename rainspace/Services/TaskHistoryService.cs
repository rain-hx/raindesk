using System;
using System.Collections.Generic;
using rainspace.ViewModel;

namespace rainspace.Services;

/// <summary>
/// 任务操作历史服务（参考 TaskTrove 的 history.ts）。
/// 支持撤销/重做操作，最多保留 50 条历史。
/// </summary>
public sealed class TaskHistoryService
{
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();
    private const int MaxHistorySize = 50;

    /// <summary>是否可以撤销</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>是否可以重做</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>最后一次操作描述</summary>
    public string? LastOperation { get; private set; }

    // ── 事件 ──────────────────────────────────────────────────────

    /// <summary>历史状态变化时触发（用于更新 UI 按钮状态）</summary>
    public event Action? HistoryChanged;

    // ── 记录操作 ──────────────────────────────────────────────────

    /// <summary>记录添加操作</summary>
    public void RecordAdd(ToDoTask task)
    {
        PushEntry(new HistoryEntry
        {
            Type = HistoryType.Add,
            Description = $"添加任务: {task.Title}",
            Snapshot = CloneTask(task)
        });
    }

    /// <summary>记录删除操作</summary>
    public void RecordDelete(ToDoTask task)
    {
        PushEntry(new HistoryEntry
        {
            Type = HistoryType.Delete,
            Description = $"删除任务: {task.Title}",
            Snapshot = CloneTask(task)
        });
    }

    /// <summary>记录完成/取消完成操作</summary>
    public void RecordToggleComplete(ToDoTask task, bool wasCompleted)
    {
        PushEntry(new HistoryEntry
        {
            Type = HistoryType.ToggleComplete,
            Description = wasCompleted ? $"取消完成: {task.Title}" : $"完成任务: {task.Title}",
            Snapshot = CloneTask(task),
            PreviousState = wasCompleted
        });
    }

    /// <summary>记录编辑操作</summary>
    public void RecordEdit(ToDoTask task, ToDoTask previousSnapshot)
    {
        PushEntry(new HistoryEntry
        {
            Type = HistoryType.Edit,
            Description = $"编辑任务: {task.Title}",
            Snapshot = CloneTask(previousSnapshot),
            NewSnapshot = CloneTask(task)
        });
    }

    // ── 撤销/重做 ────────────────────────────────────────────────

    /// <summary>撤销最后一次操作。返回需要执行的撤销动作。</summary>
    public HistoryAction? Undo()
    {
        if (_undoStack.Count == 0) return null;

        var entry = _undoStack.Pop();
        _redoStack.Push(entry);
        LastOperation = $"撤销: {entry.Description}";
        HistoryChanged?.Invoke();

        return entry.Type switch
        {
            HistoryType.Add => new HistoryAction
            {
                Type = HistoryActionType.Remove,
                TaskId = entry.Snapshot.Id
            },
            HistoryType.Delete => new HistoryAction
            {
                Type = HistoryActionType.Restore,
                Task = entry.Snapshot
            },
            HistoryType.ToggleComplete => new HistoryAction
            {
                Type = HistoryActionType.ToggleComplete,
                TaskId = entry.Snapshot.Id,
                WasCompleted = entry.PreviousState
            },
            HistoryType.Edit => new HistoryAction
            {
                Type = HistoryActionType.Restore,
                Task = entry.Snapshot
            },
            _ => null
        };
    }

    /// <summary>重做最后一次撤销。返回需要执行的重做动作。</summary>
    public HistoryAction? Redo()
    {
        if (_redoStack.Count == 0) return null;

        var entry = _redoStack.Pop();
        _undoStack.Push(entry);
        LastOperation = $"重做: {entry.Description}";
        HistoryChanged?.Invoke();

        return entry.Type switch
        {
            HistoryType.Add => new HistoryAction
            {
                Type = HistoryActionType.Restore,
                Task = entry.Snapshot
            },
            HistoryType.Delete => new HistoryAction
            {
                Type = HistoryActionType.Remove,
                TaskId = entry.Snapshot.Id
            },
            HistoryType.ToggleComplete => new HistoryAction
            {
                Type = HistoryActionType.ToggleComplete,
                TaskId = entry.Snapshot.Id,
                WasCompleted = !entry.PreviousState
            },
            HistoryType.Edit => new HistoryAction
            {
                Type = HistoryActionType.Restore,
                Task = entry.NewSnapshot ?? entry.Snapshot
            },
            _ => null
        };
    }

    /// <summary>清除所有历史</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        LastOperation = null;
        HistoryChanged?.Invoke();
    }

    // ── 内部方法 ──────────────────────────────────────────────────

    private void PushEntry(HistoryEntry entry)
    {
        _undoStack.Push(entry);
        _redoStack.Clear(); // 新操作清除重做栈

        // 限制历史大小
        while (_undoStack.Count > MaxHistorySize)
        {
            // Stack 不支持直接移除底部元素，重建
            var temp = new Stack<HistoryEntry>();
            while (_undoStack.Count > 1)
                temp.Push(_undoStack.Pop());
            _undoStack.Pop();
            while (temp.Count > 0)
                _undoStack.Push(temp.Pop());
        }

        LastOperation = entry.Description;
        HistoryChanged?.Invoke();
    }

    private static ToDoTask CloneTask(ToDoTask source)
    {
        return new ToDoTask
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Priority = source.Priority,
            Status = source.Status,
            DueDate = source.DueDate,
            DueTime = source.DueTime,
            CompletedAt = source.CompletedAt,
            CreatedAt = source.CreatedAt,
            ProjectId = source.ProjectId,
            Tags = new List<string>(source.Tags),
            Subtasks = new System.Collections.ObjectModel.ObservableCollection<Subtask>(
                source.Subtasks.Select(s => new Subtask
                {
                    Id = s.Id,
                    Title = s.Title,
                    IsCompleted = s.IsCompleted,
                    SortOrder = s.SortOrder
                })),
            Recurring = new RecurringConfig
            {
                Type = source.Recurring?.Type ?? RecurringType.None,
                Expression = source.Recurring?.Expression,
                Mode = source.Recurring?.Mode ?? RecurringMode.DueDate
            },
            SortOrder = source.SortOrder
        };
    }
}

// ── 类型定义 ──────────────────────────────────────────────────────

public enum HistoryType { Add, Delete, ToggleComplete, Edit }
public enum HistoryActionType { Restore, Remove, ToggleComplete }

public class HistoryEntry
{
    public HistoryType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public ToDoTask Snapshot { get; init; } = null!;
    public ToDoTask? NewSnapshot { get; init; }
    public bool PreviousState { get; init; }
}

public class HistoryAction
{
    public HistoryActionType Type { get; init; }
    public ToDoTask? Task { get; init; }
    public string? TaskId { get; init; }
    public bool WasCompleted { get; init; }
}
