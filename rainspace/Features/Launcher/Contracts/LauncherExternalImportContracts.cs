namespace rainspace.Features.Launcher.Contracts;

public sealed record LauncherShortcutDraftResolution(
    string SourcePath,
    LauncherShortcutDraft? Draft,
    string? Error = null)
{
    public bool Success => Draft != null && string.IsNullOrWhiteSpace(Error);

    public static LauncherShortcutDraftResolution Failed(string path, string error) =>
        new(path, null, error);
}

/// <summary>把操作系统路径解析成不含 WinUI/Windows SDK 类型的 Launcher 草稿。</summary>
public interface ILauncherShortcutDraftSource
{
    global::System.Threading.Tasks.Task<LauncherShortcutDraftResolution> ResolveAsync(
        string path,
        CancellationToken cancellationToken = default);
}

public sealed record AddLauncherShortcutsCommand(
    string TargetGroupId,
    IReadOnlyList<LauncherShortcutDraft> Shortcuts,
    bool ApplyConfiguredSort = false);

public sealed record LauncherExternalImportError(string SourcePath, string Error);

public sealed record LauncherExternalImportResult(
    bool Success,
    int AddedCount,
    IReadOnlyList<LauncherExternalImportError> Errors,
    string? Error = null)
{
    public bool HasErrors => Errors.Count > 0 || !string.IsNullOrWhiteSpace(Error);
}

public interface ILauncherExternalImportService
{
    global::System.Threading.Tasks.Task<LauncherExternalImportResult> ImportAsync(
        string targetGroupId,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default);
}
