namespace raindesk.Features.Launcher.Presentation;

/// <summary>Process-local Launcher multi-select mode shared by shell views.</summary>
public sealed class LauncherBatchModeState
{
    public bool IsEnabled { get; private set; }

    public event EventHandler? Changed;

    public bool SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled)
        {
            return false;
        }

        IsEnabled = enabled;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Toggle()
    {
        SetEnabled(!IsEnabled);
        return IsEnabled;
    }
}
