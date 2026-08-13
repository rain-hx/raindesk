namespace rainspace.Features.Tray.Contracts;

public sealed record HotkeyRegistrationSnapshot(
    bool MainRegistered,
    bool TodoRegistered,
    string? MainError,
    string? TodoError);

public interface ITrayController : IDisposable
{
    Action? OnShowRequested { get; set; }

    Action? OnOpenPanel { get; set; }

    Func<global::System.Threading.Tasks.Task>? OnExportBackup { get; set; }

    Func<global::System.Threading.Tasks.Task>? OnImportBackup { get; set; }

    Action? OnExitRequested { get; set; }

    void Initialize(bool enabled);

    void Refresh(bool enabled);
}

public interface IHotkeyRegistrar
{
    bool IsCapturing { get; }

    HotkeyRegistrationSnapshot CurrentStatus { get; }

    bool IsValid(int modifiers, int key);

    void NormalizeDefaults();

    void RegisterAll(bool showErrors = false);

    void UnregisterAll();

    void EnterCaptureMode();

    void ExitCaptureMode(bool showErrors = false);
}
