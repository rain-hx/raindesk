using System;
using Microsoft.UI.Dispatching;

namespace raindesk.Util
{
    /// <summary>
    /// 安全调度辅助：若调用方已在 UI 线程则直接执行，否则投递到 DispatcherQueue。
    /// 统一替代散落的 "if (!HasThreadAccess) TryEnqueue(...)" 与 "?.TryEnqueue(...)"。
    /// </summary>
    public static class UiDispatcher
    {
        /// <summary>投递到 UI 线程（已在 UI 线程时同步执行）。</summary>
        public static void Enqueue(
            DispatcherQueue? queue,
            Action action,
            DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            if (queue == null || action == null)
            {
                return;
            }

            if (queue.HasThreadAccess)
            {
                action();
                return;
            }

            queue.TryEnqueue(priority, () => action());
        }
    }
}
