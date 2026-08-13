namespace rainspace.Features.Launcher.Presentation;

internal enum LauncherInlinePanelMode
{
    Shortcuts,
    Url,
    SystemItems
}

internal sealed class LauncherInlineAddPanelState
{
    public LauncherInlinePanelMode Mode { get; private set; } = LauncherInlinePanelMode.Shortcuts;

    public bool IsOpen => Mode != LauncherInlinePanelMode.Shortcuts;

    public bool TryOpen(LauncherInlinePanelMode mode)
    {
        if (mode == LauncherInlinePanelMode.Shortcuts || IsOpen)
        {
            return false;
        }
        Mode = mode;
        return true;
    }

    public void Close()
    {
        Mode = LauncherInlinePanelMode.Shortcuts;
    }
}
