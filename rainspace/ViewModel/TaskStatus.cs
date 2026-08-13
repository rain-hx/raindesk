namespace rainspace.ViewModel;

/// <summary>
/// 任务状态（参考 TaskTrove 的状态机）。
/// 替代旧的 ToDoList/HiToDoList 双集合分离模式。
/// </summary>
public enum TaskItemStatus
{
    /// <summary>待处理（默认）</summary>
    Pending = 0,

    /// <summary>进行中</summary>
    InProgress = 1,

    /// <summary>已完成</summary>
    Completed = 2,

    /// <summary>已归档（不再显示在默认视图中）</summary>
    Archived = 3
}
