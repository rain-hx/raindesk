using System;
using System.Collections.Generic;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Services;

/// <summary>
/// 循环任务处理器（参考 TaskTrove 的 recurring-task-processor.ts）。
///
/// 核心逻辑：
/// 当用户完成一个循环任务时：
/// 1. 创建一个"历史快照"（已完成的副本）
/// 2. 将原任务重置为未完成，计算下次截止日期
/// 3. 如果没有更多循环次数，直接标记完成
/// </summary>
public static class RecurringTaskProcessor
{
    /// <summary>
    /// 处理循环任务的完成事件。
    /// 返回值：(historyTask, nextTask) 或 (null, null) 如果不需要特殊处理。
    /// - historyTask: 已完成的快照（用于历史记录）
    /// - nextTask: 下一个循环实例（重置状态+新日期）
    /// </summary>
    public static (ToDoTask? historyTask, ToDoTask? nextTask) ProcessCompletion(ToDoTask task)
    {
        if (task?.Recurring?.IsRecurring != true)
            return (null, null);

        var config = task.Recurring;

        // 计算下次截止日期
        DateTime? nextDueDate = CalculateNextDueDate(config, task.DueDate);
        if (nextDueDate == null)
        {
            // 没有下次了，直接完成
            return (null, null);
        }

        // 1. 创建历史快照（已完成的副本）
        var historyTask = new ToDoTask
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Status = TaskItemStatus.Completed,
            DueDate = task.DueDate,
            DueTime = task.DueTime,
            CompletedAt = DateTime.Now,
            CreatedAt = task.CreatedAt,
            ProjectId = task.ProjectId,
            Tags = new List<string>(task.Tags),
            Recurring = new RecurringConfig { Type = RecurringType.None }, // 历史快照不再是循环任务
            SortOrder = task.SortOrder
        };

        // 2. 创建下一个循环实例
        var nextTask = new ToDoTask
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Status = TaskItemStatus.Pending,
            DueDate = nextDueDate,
            DueTime = task.DueTime,
            CreatedAt = DateTime.Now,
            ProjectId = task.ProjectId,
            Tags = new List<string>(task.Tags),
            Recurring = new RecurringConfig
            {
                Type = config.Type,
                Expression = config.Expression,
                Mode = config.Mode
            },
            SortOrder = task.SortOrder
        };

        // 重置子任务（参考 TaskTrove：新实例的子任务都是未完成的）
        foreach (var subtask in task.Subtasks)
        {
            nextTask.Subtasks.Add(new Subtask
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = subtask.Title,
                IsCompleted = false,
                SortOrder = subtask.SortOrder
            });
        }

        LogUtil.WriteLog($"循环任务完成处理: {task.Title}, 下次: {nextDueDate:yyyy-MM-dd}");

        return (historyTask, nextTask);
    }

    /// <summary>
    /// 计算下次截止日期（参考 TaskTrove 的 calculateNextDueDate）。
    /// </summary>
    public static DateTime? CalculateNextDueDate(RecurringConfig config, DateTime? currentDueDate)
    {
        if (config == null || !config.IsRecurring)
            return null;

        var referenceDate = config.Mode == RecurringMode.CompletedAt
            ? DateTime.Now
            : (currentDueDate ?? DateTime.Today);

        return config.Type switch
        {
            RecurringType.Cron => CalculateNextCronDate(config.Expression, referenceDate),
            RecurringType.RRule => null, // 暂未实现
            _ => null
        };
    }

    /// <summary>
    /// 计算下次 cron 触发时间。
    /// 支持简化的 cron 表达式：
    /// - "0 0 * * *" = 每天
    /// - "0 0 * * 0" = 每周日
    /// - "0 0 1 * *" = 每月1日
    /// - 通用 cron: 使用 Quartz 解析
    /// </summary>
    private static DateTime? CalculateNextCronDate(string? cronExpression, DateTime referenceDate)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return null;

        try
        {
            // 使用 Quartz 的 CronExpression 计算下次触发时间
            var cron = new Quartz.CronExpression(cronExpression);
            var next = cron.GetNextValidTimeAfter(referenceDate);
            return next?.LocalDateTime;
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, $"解析 cron 表达式失败: {cronExpression}");
            return null;
        }
    }

    /// <summary>
    /// 获取循环任务的参考日期（参考 TaskTrove 的 getRecurringReferenceDate）。
    /// - DueDate 模式：使用截止日期
    /// - CompletedAt 模式：使用 max(完成时间, 截止日期)，防止倒退
    /// </summary>
    public static DateTime GetRecurringReferenceDate(ToDoTask task)
    {
        if (task.Recurring?.Mode == RecurringMode.CompletedAt)
        {
            var actionDate = DateTime.Now;
            var dueDate = task.DueDate ?? DateTime.Today;
            return actionDate > dueDate ? actionDate : dueDate;
        }
        return task.DueDate ?? DateTime.Today;
    }
}
