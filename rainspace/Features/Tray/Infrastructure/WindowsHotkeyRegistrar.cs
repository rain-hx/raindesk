using rainspace.Features.Tray.Contracts;
using rainspace.Services;
using rainspace.Util;

namespace rainspace.Features.Tray.Infrastructure;

public sealed class WindowsHotkeyRegistrar : IHotkeyRegistrar
{
    public bool IsCapturing => HotkeyService.IsCapturingHotkey;

    public HotkeyRegistrationSnapshot CurrentStatus
    {
        get
        {
            HotkeyRegistrationStatus status = HotkeyService.CurrentStatus;
            return new HotkeyRegistrationSnapshot(
                status.MainRegistered,
                status.TodoRegistered,
                status.MainError,
                status.TodoError);
        }
    }

    public bool IsValid(int modifiers, int key) => HotkeyService.IsValidHotKey(
        (HotkeyModifiers)modifiers,
        (VirtualKey)key);

    public void NormalizeDefaults() => HotkeyService.NormalizeHotKeyDefaults();

    public void RegisterAll(bool showErrors = false) => HotkeyService.RegisterAll(showErrors);

    public void UnregisterAll() => HotkeyService.UnregisterAll();

    public void EnterCaptureMode() => HotkeyService.EnterCaptureMode();

    public void ExitCaptureMode(bool showErrors = false) => HotkeyService.ExitCaptureMode(showErrors);
}
