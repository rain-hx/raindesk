using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace rainspace.ViewModel;

/// <summary>
/// 待办筛选条件（参考 TaskTrove 的 ViewState + activeFilters）。
/// </summary>
public class ToDoFilter : INotifyPropertyChanged
{
    private ToDoStatusFilter statusFilter = ToDoStatusFilter.Active;
    private ToDoPriorityFilter priorityFilter = ToDoPriorityFilter.All;
    private ToDoDateFilter dateFilter = ToDoDateFilter.All;
    private ToDoSortBy sortBy = ToDoSortBy.DueDate;
    private bool sortAscending = true;
    private string? searchQuery;
    private string? projectId;

    /// <summary>状态筛选</summary>
    public ToDoStatusFilter StatusFilter
    {
        get => statusFilter;
        set => SetProperty(ref statusFilter, value);
    }

    /// <summary>优先级筛选</summary>
    public ToDoPriorityFilter PriorityFilter
    {
        get => priorityFilter;
        set => SetProperty(ref priorityFilter, value);
    }

    /// <summary>日期筛选</summary>
    public ToDoDateFilter DateFilter
    {
        get => dateFilter;
        set => SetProperty(ref dateFilter, value);
    }

    /// <summary>排序方式</summary>
    public ToDoSortBy SortBy
    {
        get => sortBy;
        set => SetProperty(ref sortBy, value);
    }

    /// <summary>是否升序</summary>
    public bool SortAscending
    {
        get => sortAscending;
        set => SetProperty(ref sortAscending, value);
    }

    /// <summary>搜索关键词</summary>
    public string? SearchQuery
    {
        get => searchQuery;
        set => SetProperty(ref searchQuery, NormalizeQuery(value));
    }

    /// <summary>按项目 ID 筛选（null 表示全部）</summary>
    public string? ProjectId
    {
        get => projectId;
        set => SetProperty(ref projectId, NormalizeQuery(value));
    }

    /// <summary>应用筛选和排序</summary>
    public List<ToDoTask> Apply(IEnumerable<ToDoTask> tasks)
    {
        var query = tasks.AsEnumerable();

        // 状态筛选
        query = StatusFilter switch
        {
            ToDoStatusFilter.Active => query.Where(t => t.IsActive),
            ToDoStatusFilter.Completed => query.Where(t => t.IsCompleted),
            ToDoStatusFilter.Overdue => query.Where(t => t.IsOverdue),
            _ => query
        };

        // 优先级筛选
        query = PriorityFilter switch
        {
            ToDoPriorityFilter.High => query.Where(t => t.Priority <= TaskPriority.High),
            ToDoPriorityFilter.Medium => query.Where(t => t.Priority == TaskPriority.Medium),
            ToDoPriorityFilter.Low => query.Where(t => t.Priority == TaskPriority.Low),
            _ => query
        };

        // 日期筛选
        var today = DateTime.Today;
        query = DateFilter switch
        {
            ToDoDateFilter.Today => query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today),
            ToDoDateFilter.ThisWeek => query.Where(t => t.DueDate.HasValue && (t.DueDate.Value.Date - today).TotalDays is >= 0 and <= 7),
            ToDoDateFilter.Overdue => query.Where(t => t.IsOverdue),
            _ => query
        };

        // 搜索
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery;
            query = query.Where(t =>
                (t.Title != null && t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (t.Description != null && t.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                t.Tags.Any(tag => tag.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        // 项目筛选
        if (!string.IsNullOrWhiteSpace(ProjectId))
        {
            query = query.Where(t => t.ProjectId == ProjectId);
        }

        // 排序
        query = SortBy switch
        {
            ToDoSortBy.DueDate => SortAscending
                ? query.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ThenBy(t => t.Priority)
                : query.OrderByDescending(t => t.DueDate ?? DateTime.MinValue).ThenBy(t => t.Priority),
            ToDoSortBy.Priority => SortAscending
                ? query.OrderBy(t => t.Priority).ThenBy(t => t.DueDate)
                : query.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate),
            ToDoSortBy.CreatedAt => SortAscending
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt),
            ToDoSortBy.Title => SortAscending
                ? query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(t => t.Title, StringComparer.OrdinalIgnoreCase),
            ToDoSortBy.Manual => SortAscending
                ? query.OrderBy(t => t.SortOrder)
                : query.OrderByDescending(t => t.SortOrder),
            _ => query
        };

        return query.ToList();
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

    private static string? NormalizeQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

public enum ToDoStatusFilter { All, Active, Completed, Overdue }
public enum ToDoPriorityFilter { All, High, Medium, Low }
public enum ToDoDateFilter { All, Today, ThisWeek, Overdue }
public enum ToDoSortBy { DueDate, Priority, CreatedAt, Title, Manual }
