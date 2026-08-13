namespace raindesk.ViewModel;

/// <summary>
/// 任务优先级（参考 TaskTrove 的 4 级优先级）。
/// 数值越小优先级越高，便于排序。
/// </summary>
public enum TaskPriority
{
    /// <summary>紧急/最高优先级（红色）</summary>
    Critical = 1,

    /// <summary>高优先级（橙色）</summary>
    High = 2,

    /// <summary>中优先级（蓝色，默认）</summary>
    Medium = 3,

    /// <summary>低优先级（灰色）</summary>
    Low = 4
}
