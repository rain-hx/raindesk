using rainspace.Features.Launcher.Application;

namespace rainspace.Features.Launcher.Presentation;

internal sealed class LauncherPointerCacheState
{
    public bool HasValue { get; private set; }

    public LauncherPoint Value { get; private set; }

    public void Store(LauncherPoint value)
    {
        Value = value;
        HasValue = true;
    }

    public bool TryGet(out LauncherPoint value)
    {
        value = Value;
        return HasValue;
    }

    public void Clear()
    {
        HasValue = false;
        Value = default;
    }
}
