namespace raindesk.Features.Launcher.Presentation;

public readonly record struct LauncherLayoutRequest(int Generation, bool AnimateRequested);

/// <summary>Coalesces layout invalidations while preserving the newest generation.</summary>
public sealed class LauncherLayoutScheduleState
{
    private bool _scheduled;
    private bool _animateRequested;

    public int Generation { get; private set; }

    public bool Request(bool animate)
    {
        Generation++;
        _animateRequested |= animate;
        if (_scheduled)
        {
            return false;
        }

        _scheduled = true;
        return true;
    }

    public bool TryConsume(out LauncherLayoutRequest request)
    {
        if (!_scheduled)
        {
            request = default;
            return false;
        }

        request = new LauncherLayoutRequest(Generation, _animateRequested);
        _scheduled = false;
        _animateRequested = false;
        return true;
    }

    public bool IsCurrent(int generation) => generation == Generation;

    public void Cancel()
    {
        _scheduled = false;
        _animateRequested = false;
    }
}
