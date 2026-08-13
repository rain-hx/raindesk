using Newtonsoft.Json;
using rainspace.Constant;
using System;

namespace rainspace.ViewModel;

/// <summary>
/// 循环任务配置（参考 TaskTrove 的 recurring + recurringMode）。
/// 替代旧的 ExecType + Cron 组合。
/// </summary>
[Serializable]
public class RecurringConfig
{
    /// <summary>循环类型</summary>
    public RecurringType Type { get; set; } = RecurringType.None;

    /// <summary>
    /// 循环表达式。
    /// Type=Cron 时为 Quartz cron 表达式（如 "0 9 * * MON-FRI"）。
    /// Type=RRule 时为 RFC 5545 RRULE 表达式。
    /// Type=None 时忽略。
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// 循环基准模式（参考 TaskTrove 的 recurringMode）。
    /// - DueDate: 从截止日期计算下次（固定时间表）
    /// - CompletedAt: 从完成时间计算下次（间隔型）
    /// </summary>
    public RecurringMode Mode { get; set; } = RecurringMode.DueDate;

    /// <summary>是否为循环任务</summary>
    [JsonIgnore]
    public bool IsRecurring => Type != RecurringType.None && !string.IsNullOrWhiteSpace(Expression);

    /// <summary>从旧的 ExecType + Cron 迁移</summary>
    public static RecurringConfig FromLegacy(TodoTaskExecType execType, string? cron)
    {
        if (execType == TodoTaskExecType.CRON && !string.IsNullOrWhiteSpace(cron))
        {
            return new RecurringConfig
            {
                Type = RecurringType.Cron,
                Expression = cron,
                Mode = RecurringMode.DueDate
            };
        }
        return new RecurringConfig();
    }

    /// <summary>转换为旧的 ExecType（向后兼容）</summary>
    public TodoTaskExecType ToLegacyExecType()
    {
        return Type == RecurringType.Cron ? TodoTaskExecType.CRON : TodoTaskExecType.SET_TIME;
    }
}

/// <summary>
/// 循环类型。
/// </summary>
public enum RecurringType
{
    /// <summary>不循环</summary>
    None = 0,

    /// <summary>Quartz cron 表达式</summary>
    Cron = 1,

    /// <summary>RFC 5545 RRULE（预留，暂未实现）</summary>
    RRule = 2
}

/// <summary>
/// 循环基准模式。
/// </summary>
public enum RecurringMode
{
    /// <summary>从截止日期计算下次</summary>
    DueDate = 0,

    /// <summary>从完成时间计算下次（间隔型）</summary>
    CompletedAt = 1
}
