using Microsoft.UI.Dispatching;

namespace rainspace.Features.Launcher.Presentation.WinUI;

public sealed class LauncherShortcutLayoutScheduler : IDisposable
{
    private readonly LauncherLayoutScheduleState _state = new();
    private readonly DispatcherQueueTimer _layoutTimer;
    private readonly DispatcherQueueTimer _transitionRestoreTimer;
    private readonly Func<bool> _canAnimate;
    private readonly Action<int, bool> _apply;
    private readonly Func<bool> _canRestoreTransitions;
    private readonly Action _restoreTransitions;
    private readonly Action<Exception> _reportError;
    private readonly TimeSpan _animatedLayoutRestoreDelay;

    public LauncherShortcutLayoutScheduler(
        DispatcherQueue dispatcherQueue,
        Func<bool> canAnimate,
        Action<int, bool> apply,
        Func<bool> canRestoreTransitions,
        Action restoreTransitions,
        Action<Exception> reportError,
        TimeSpan animatedLayoutRestoreDelay)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        _canAnimate = canAnimate ?? throw new ArgumentNullException(nameof(canAnimate));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _canRestoreTransitions = canRestoreTransitions ?? throw new ArgumentNullException(nameof(canRestoreTransitions));
        _restoreTransitions = restoreTransitions ?? throw new ArgumentNullException(nameof(restoreTransitions));
        _reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
        _animatedLayoutRestoreDelay = animatedLayoutRestoreDelay;

        _layoutTimer = dispatcherQueue.CreateTimer();
        _layoutTimer.Interval = TimeSpan.FromMilliseconds(16);
        _layoutTimer.Tick += LayoutTimer_Tick;
        _transitionRestoreTimer = dispatcherQueue.CreateTimer();
        _transitionRestoreTimer.Tick += TransitionRestoreTimer_Tick;
    }

    public int CurrentGeneration => _state.Generation;

    public bool IsCurrent(int generation) => _state.IsCurrent(generation);

    internal LauncherLayoutScheduleState ScheduleState => _state;

    public void Schedule(bool animate = false)
    {
        _transitionRestoreTimer.Stop();
        if (_state.Request(animate))
        {
            _layoutTimer.Start();
        }
    }

    public void ScheduleTransitionRestore(TimeSpan delay)
    {
        _transitionRestoreTimer.Stop();
        _transitionRestoreTimer.Interval = delay;
        _transitionRestoreTimer.Start();
    }

    public void Cancel()
    {
        _layoutTimer.Stop();
        _transitionRestoreTimer.Stop();
        _state.Cancel();
    }

    private void LayoutTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        if (!_state.TryConsume(out LauncherLayoutRequest request))
        {
            return;
        }

        try
        {
            bool animate = request.AnimateRequested && _canAnimate();
            _apply(request.Generation, animate);
            if (animate)
            {
                ScheduleTransitionRestore(_animatedLayoutRestoreDelay);
            }
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    private void TransitionRestoreTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        if (_canRestoreTransitions())
        {
            _restoreTransitions();
        }
    }

    public void Dispose()
    {
        Cancel();
        _layoutTimer.Tick -= LayoutTimer_Tick;
        _transitionRestoreTimer.Tick -= TransitionRestoreTimer_Tick;
    }
}
