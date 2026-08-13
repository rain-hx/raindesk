using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace rainspace.ViewModel;

/// <summary>
/// 任务模型（参考 TaskTrove 的 TaskSchema）。
/// 替代旧的 ToDoInfo，提供更丰富的任务管理能力。
///
/// 设计原则：
/// - 使用 DateTime 而非 string 存储时间
/// - 使用单一 Status 枚举替代双集合（ToDoList/HiToDoList）
/// - 支持优先级、标签、子任务
/// - 支持循环任务（Quartz cron）
/// </summary>
[Serializable]
public class ToDoTask : INotifyPropertyChanged
{
    // ── 持久化字段 ────────────────────────────────────────────────

    private string id = Guid.NewGuid().ToString("N");
    private string title = string.Empty;
    private string? description;
    private TaskPriority priority = TaskPriority.Medium;
    private TaskItemStatus status = TaskItemStatus.Pending;
    private DateTime? dueDate;
    private string? dueTime;          // "HH:mm" 格式
    private DateTime? completedAt;
    private DateTime createdAt = DateTime.Now;
    private string? projectId;
    private List<string> tags = new();
    private ObservableCollection<Subtask> subtasks = new();
    private RecurringConfig recurring = new();
    private int sortOrder;

    // ── 运行时字段（不序列化） ────────────────────────────────────

    [NonSerialized]
    private bool isSelected_NoWrite;
    [NonSerialized]
    private bool isExpanded_NoWrite;

    // ── 属性 ──────────────────────────────────────────────────────

    /// <summary>唯一标识（UUID 格式）</summary>
    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    /// <summary>任务标题（必填）</summary>
    public string Title
    {
        get => title;
        set
        {
            if (SetProperty(ref title, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    /// <summary>任务描述/详情</summary>
    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    /// <summary>优先级</summary>
    public TaskPriority Priority
    {
        get => priority;
        set
        {
            if (SetProperty(ref priority, value))
            {
                OnPropertyChanged(nameof(PriorityColor));
            }
        }
    }

    /// <summary>任务状态</summary>
    public TaskItemStatus Status
    {
        get => status;
        set
        {
            var old = status;
            if (old == value)
            {
                return;
            }

            status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsOverdue));
            OnPropertyChanged(nameof(DisplayTitle));

            // 自动设置/清除完成时间
            if (value == TaskItemStatus.Completed && old != TaskItemStatus.Completed)
            {
                CompletedAt = DateTime.Now;
            }
            else if (value != TaskItemStatus.Completed && old == TaskItemStatus.Completed)
            {
                CompletedAt = null;
            }
        }
    }

    /// <summary>截止日期（不含时间部分）</summary>
    public DateTime? DueDate
    {
        get => dueDate?.Date;
        set
        {
            var normalized = value?.Date;
            if (dueDate == normalized)
            {
                return;
            }

            dueDate = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOverdue));
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(DueDateDisplay));
        }
    }

    /// <summary>截止时间（"HH:mm" 格式，可选）</summary>
    public string? DueTime
    {
        get => dueTime;
        set => SetProperty(ref dueTime, value);
    }

    /// <summary>完成时间</summary>
    public DateTime? CompletedAt
    {
        get => completedAt;
        set => SetProperty(ref completedAt, value);
    }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt
    {
        get => createdAt;
        set => SetProperty(ref createdAt, value);
    }

    /// <summary>所属项目 ID（可选）</summary>
    public string? ProjectId
    {
        get => projectId;
        set => SetProperty(ref projectId, value);
    }

    /// <summary>标签列表</summary>
    public List<string> Tags
    {
        get => tags;
        set
        {
            var normalized = value ?? new();
            if (SetProperty(ref tags, normalized))
            {
                OnPropertyChanged(nameof(TagsDisplay));
            }
        }
    }

    /// <summary>子任务列表</summary>
    public ObservableCollection<Subtask> Subtasks
    {
        get => subtasks;
        set
        {
            var normalized = value ?? new();
            if (SetProperty(ref subtasks, normalized))
            {
                OnPropertyChanged(nameof(SubtaskProgress));
            }
        }
    }

    /// <summary>循环配置</summary>
    public RecurringConfig Recurring
    {
        get => recurring;
        set
        {
            var normalized = value ?? new();
            if (SetProperty(ref recurring, normalized))
            {
                OnPropertyChanged(nameof(IsRecurring));
            }
        }
    }

    /// <summary>手动排序权重</summary>
    public int SortOrder
    {
        get => sortOrder;
        set => SetProperty(ref sortOrder, value);
    }

    // ── 计算属性 ──────────────────────────────────────────────────

    /// <summary>是否已完成</summary>
    [JsonIgnore]
    public bool IsCompleted => Status == TaskItemStatus.Completed;

    /// <summary>是否为活跃状态（Pending 或 InProgress）</summary>
    [JsonIgnore]
    public bool IsActive => Status == TaskItemStatus.Pending || Status == TaskItemStatus.InProgress;

    /// <summary>是否已过期</summary>
    [JsonIgnore]
    public bool IsOverdue => DueDate.HasValue
        && DueDate.Value.Date < DateTime.Today
        && !IsCompleted
        && !Recurring.IsRecurring;

    /// <summary>是否为循环任务</summary>
    [JsonIgnore]
    public bool IsRecurring => Recurring.IsRecurring;

    /// <summary>显示用标题（含过期标记）</summary>
    [JsonIgnore]
    public string DisplayTitle => IsOverdue ? $"⚠ {Title}" : Title;

    /// <summary>截止日期显示文本</summary>
    [JsonIgnore]
    public string DueDateDisplay
    {
        get
        {
            if (!DueDate.HasValue) return string.Empty;
            var date = DueDate.Value.Date;
            var today = DateTime.Today;

            if (date == today) return "今天";
            if (date == today.AddDays(1)) return "明天";
            if (date == today.AddDays(-1)) return "昨天";
            if (date < today) return $"已过期 {((TimeSpan)(today - date)).Days} 天";
            if ((date - today).Days <= 7) return $"还剩 {(date - today).Days} 天";
            return date.ToString("MM/dd");
        }
    }

    /// <summary>优先级颜色（用于 UI 绑定）</summary>
    [JsonIgnore]
    public string PriorityColor => Priority switch
    {
        TaskPriority.Critical => "#E74C3C",
        TaskPriority.High => "#E67E22",
        TaskPriority.Medium => "#3498DB",
        TaskPriority.Low => "#95A5A6",
        _ => "#3498DB"
    };

    /// <summary>子任务进度（如 "2/5"）</summary>
    [JsonIgnore]
    public string SubtaskProgress
    {
        get
        {
            if (Subtasks.Count == 0) return string.Empty;
            int done = Subtasks.Count(s => s.IsCompleted);
            return $"{done}/{Subtasks.Count}";
        }
    }

    /// <summary>标签显示文本（逗号分隔）</summary>
    [JsonIgnore]
    public string TagsDisplay => Tags.Count == 0 ? string.Empty : string.Join(", ", Tags);

    // ── UI 状态（不序列化） ───────────────────────────────────────

    [JsonIgnore]
    public bool IsSelected_NoWrite
    {
        get => isSelected_NoWrite;
        set => SetProperty(ref isSelected_NoWrite, value);
    }

    [JsonIgnore]
    public bool IsExpanded_NoWrite
    {
        get => isExpanded_NoWrite;
        set => SetProperty(ref isExpanded_NoWrite, value);
    }

    // ── 方法 ──────────────────────────────────────────────────────

    /// <summary>切换完成状态</summary>
    public void ToggleComplete()
    {
        if (IsCompleted)
        {
            Status = TaskItemStatus.Pending;
        }
        else
        {
            Status = TaskItemStatus.Completed;
        }
    }

    /// <summary>从旧的 ToDoInfo 迁移</summary>
    public static ToDoTask FromLegacy(ToDoInfo legacy)
    {
        var task = new ToDoTask
        {
            Id = string.IsNullOrWhiteSpace(legacy.Id)
                ? Guid.NewGuid().ToString("N")
                : legacy.Id,
            Title = legacy.Title ?? string.Empty,
            Description = legacy.Msg,
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Pending,
            CreatedAt = DateTime.Now,
            Recurring = RecurringConfig.FromLegacy(legacy.ExecType, legacy.Cron)
        };

        // 解析 ExeTime
        if (!string.IsNullOrEmpty(legacy.ExeTime))
        {
            if (DateTime.TryParse(legacy.ExeTime, out var dt))
            {
                task.DueDate = dt.Date;
                task.DueTime = dt.ToString("HH:mm");
            }
        }

        // 解析 DoneTime
        if (!string.IsNullOrEmpty(legacy.DoneTime))
        {
            if (DateTime.TryParse(legacy.DoneTime, out var doneDt))
            {
                task.CompletedAt = doneDt;
                task.Status = TaskItemStatus.Completed;
            }
        }

        return task;
    }

    /// <summary>转换为旧的 ToDoInfo（向后兼容）</summary>
    public ToDoInfo ToLegacy()
    {
        var legacy = new ToDoInfo
        {
            Id = Id,
            Title = Title,
            Description = Description,
            ExecType = Recurring.ToLegacyExecType(),
            Cron = Recurring.Expression
        };

        if (DueDate.HasValue)
        {
            var dt = DueDate.Value.Date;
            if (!string.IsNullOrEmpty(DueTime) && TimeSpan.TryParse(DueTime, out var time))
            {
                dt = dt.Add(time);
            }
            legacy.ExeTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (CompletedAt.HasValue)
        {
            legacy.DoneTime = CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return legacy;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
