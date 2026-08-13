namespace rainspace.Services;

public sealed class HotkeyRegistrationStatus
{
    public bool MainRegistered { get; init; }
    public bool TodoRegistered { get; init; }
    public bool MainUsesAltWHook { get; init; }
    public bool TodoUsesAltHook { get; init; }
    public string? MainError { get; init; }
    public string? TodoError { get; init; }

    public bool HasAnyFailure =>
        !string.IsNullOrWhiteSpace(MainError)
        || !string.IsNullOrWhiteSpace(TodoError);
}
