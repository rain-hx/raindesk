using System;
using System.Collections.Generic;
using raindesk.ViewModel;

namespace raindesk.Services.Interfaces;

/// <summary>
/// 待办任务服务接口（参考 TaskTrove 的 task atoms）。
/// 替代散落在 UI 代码中的待办操作逻辑。
/// </summary>
public interface IToDoService
{
    // ── 查询 ──────────────────────────────────────────────────────

    /// <summary>获取所有任务</summary>
    IReadOnlyList<ToDoTask> GetAll();

    /// <summary>获取活跃任务（Pending + InProgress）</summary>
    IReadOnlyList<ToDoTask> GetActive();

    /// <summary>获取已完成任务</summary>
    IReadOnlyList<ToDoTask> GetCompleted();

    /// <summary>获取已过期任务</summary>
    IReadOnlyList<ToDoTask> GetOverdue();

    /// <summary>获取今天的任务（截止日期为今天）</summary>
    IReadOnlyList<ToDoTask> GetToday();

    /// <summary>按项目获取任务</summary>
    IReadOnlyList<ToDoTask> GetByProject(string projectId);

    /// <summary>按标签获取任务</summary>
    IReadOnlyList<ToDoTask> GetByTag(string tag);

    /// <summary>按 ID 获取任务</summary>
    ToDoTask? GetById(string id);

    // ── 变更 ──────────────────────────────────────────────────────

    /// <summary>添加任务</summary>
    ToDoTask Add(ToDoTask task);

    /// <summary>从旧待办编辑器添加任务，并返回带稳定 ID 的兼容视图。</summary>
    ToDoInfo AddLegacy(ToDoInfo item);

    /// <summary>用旧待办编辑器的结果更新任务，并将其恢复为活跃状态。</summary>
    ToDoInfo? ReplaceLegacy(string existingId, ToDoInfo replacement);

    /// <summary>更新任务</summary>
    void Update(ToDoTask task);

    /// <summary>删除任务</summary>
    void Delete(string id);

    /// <summary>切换完成状态</summary>
    void ToggleComplete(string id);

    /// <summary>批量完成</summary>
    void CompleteAll(IEnumerable<string> ids);

    /// <summary>批量删除</summary>
    void DeleteAll(IEnumerable<string> ids);

    /// <summary>移动到项目</summary>
    void MoveToProject(string id, string? projectId);

    /// <summary>重新排序</summary>
    void Reorder(string id, int newOrder);

    // ── 子任务 ────────────────────────────────────────────────────

    /// <summary>添加子任务</summary>
    Subtask AddSubtask(string taskId, string title);

    /// <summary>切换子任务完成状态</summary>
    void ToggleSubtask(string taskId, string subtaskId);

    /// <summary>删除子任务</summary>
    void DeleteSubtask(string taskId, string subtaskId);

    // ── 事件 ──────────────────────────────────────────────────────

    /// <summary>任务添加后触发</summary>
    event Action<ToDoTask>? TaskAdded;

    /// <summary>任务更新后触发</summary>
    event Action<ToDoTask>? TaskUpdated;

    /// <summary>任务删除后触发</summary>
    event Action<string>? TaskDeleted;

    /// <summary>任务完成时触发</summary>
    event Action<ToDoTask>? TaskCompleted;
}
