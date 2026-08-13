using Microsoft.UI.Dispatching;

namespace raindesk.Features.Workspace.Presentation.WinUI;

internal sealed class WorkspaceGroupEditFocusScheduler
{
    public void Schedule(
        DispatcherQueue dispatcherQueue,
        Func<bool> tryFocus,
        Action exhausted,
        Action settled,
        int maxAttempts = 12)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        ArgumentNullException.ThrowIfNull(tryFocus);
        ArgumentNullException.ThrowIfNull(exhausted);
        ArgumentNullException.ThrowIfNull(settled);
        ScheduleAttempt(dispatcherQueue, tryFocus, exhausted, settled, 0, Math.Max(0, maxAttempts));
    }

    private static void ScheduleAttempt(
        DispatcherQueue dispatcherQueue,
        Func<bool> tryFocus,
        Action exhausted,
        Action settled,
        int attempt,
        int maxAttempts)
    {
        dispatcherQueue.TryEnqueue(
            attempt == 0 ? DispatcherQueuePriority.Normal : DispatcherQueuePriority.Low,
            () =>
            {
                if (tryFocus())
                {
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => settled());
                    return;
                }

                if (attempt < maxAttempts)
                {
                    ScheduleAttempt(
                        dispatcherQueue,
                        tryFocus,
                        exhausted,
                        settled,
                        attempt + 1,
                        maxAttempts);
                }
                else
                {
                    exhausted();
                }
            });
    }
}
