using System;
using System.Collections.Generic;
using System.Linq;

namespace rainspace.ViewModel;

/// <summary>
/// 任务统计模型。
/// 提供各种统计功能。
/// </summary>
public class TaskStatistics
{
    private readonly List<ToDoTask> _tasks;

    public TaskStatistics(List<ToDoTask> tasks)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
    }

    /// <summary>总任务数</summary>
    public int TotalCount => _tasks.Count;

    /// <summary>完成任务数</summary>
    public int CompletedCount => _tasks.Count(t => t.Status == TaskItemStatus.Completed);

    /// <summary>进行中任务数</summary>
    public int ActiveCount => _tasks.Count(t => t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.InProgress);

    /// <summary>已过期任务数</summary>
    public int OverdueCount => _tasks.Count(t => t.IsOverdue);

    /// <summary>完成率</summary>
    public double CompletionRate => TotalCount > 0 ? (double)CompletedCount / TotalCount : 0;

    /// <summary>最近N天完成趋势</summary>
    public List<DailyCompletion> GetCompletionTrend(int days = 7)
    {
        var result = new List<DailyCompletion>();
        var today = DateTime.Today;

        for (int i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var completed = _tasks.Count(t =>
                t.Status == TaskItemStatus.Completed &&
                t.CompletedAt?.Date == date);

            result.Add(new DailyCompletion
            {
                Date = date,
                Count = completed
            });
        }

        return result;
    }

    /// <summary>优先级分布</summary>
    public Dictionary<TaskPriority, int> GetPriorityDistribution()
    {
        return _tasks
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>项目任务分布</summary>
    public Dictionary<string, int> GetProjectDistribution()
    {
        return _tasks
            .Where(t => !string.IsNullOrEmpty(t.ProjectId))
            .GroupBy(t => t.ProjectId!)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

/// <summary>每日完成统计</summary>
public class DailyCompletion
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
