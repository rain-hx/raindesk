using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Services;

/// <summary>
/// 待办数据迁移服务。
/// 将旧的 ToDoList + HiToDoList 双集合模式迁移到新的 Tasks 单集合模式。
///
/// 迁移规则：
/// - ToDoList 中的项 → Status=Pending 的 ToDoTask
/// - HiToDoList 中的项 → Status=Completed 的 ToDoTask
/// - ExeTime (string) → DueDate (DateTime) + DueTime (string)
/// - DoneTime (string) → CompletedAt (DateTime)
/// - ExecType.CRON + Cron → Recurring { Type=Cron, Expression=Cron }
/// - 自动生成 Id, CreatedAt, Priority=Medium
/// </summary>
public static class ToDoMigrationService
{
    /// <summary>
    /// 检测是否需要迁移（旧集合非空且新集合为空）。
    /// </summary>
    public static bool NeedsMigration(AppData data)
    {
        if (data == null) return false;

        // 如果新集合已有数据，不需要迁移
        if (data.Tasks != null && data.Tasks.Count > 0) return false;

        // 如果旧集合有数据，需要迁移
        bool hasOld = (data.ToDoList != null && data.ToDoList.Count > 0)
                   || (data.HiToDoList != null && data.HiToDoList.Count > 0);

        return hasOld;
    }

    /// <summary>
    /// 执行迁移。返回迁移后的任务列表。
    /// </summary>
    public static List<ToDoTask> Migrate(AppData data)
    {
        var tasks = new List<ToDoTask>();

        if (data == null) return tasks;

        // 迁移活跃待办
        if (data.ToDoList != null)
        {
            foreach (var legacy in data.ToDoList)
            {
                try
                {
                    var task = ToDoTask.FromLegacy(legacy);
                    task.Status = TaskItemStatus.Pending;
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, $"迁移待办项失败: {legacy.Title}");
                }
            }
        }

        // 迁移历史待办
        if (data.HiToDoList != null)
        {
            foreach (var legacy in data.HiToDoList)
            {
                try
                {
                    var task = ToDoTask.FromLegacy(legacy);
                    task.Status = TaskItemStatus.Completed;
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, $"迁移历史待办项失败: {legacy.Title}");
                }
            }
        }

        LogUtil.WriteLog($"待办数据迁移完成: {tasks.Count} 项（活跃 {data.ToDoList?.Count ?? 0}，历史 {data.HiToDoList?.Count ?? 0}）");

        return tasks;
    }

    /// <summary>
    /// 双写兼容：将新 Tasks 同步回旧集合（过渡期使用）。
    /// </summary>
    public static void SyncBackToLegacy(AppData data)
    {
        if (data?.Tasks == null) return;

        var active = new ObservableCollection<ToDoInfo>();
        var history = new ObservableCollection<ToDoInfo>();

        foreach (var task in data.Tasks)
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

        data.ToDoList = active;
        data.HiToDoList = history;
    }
}
