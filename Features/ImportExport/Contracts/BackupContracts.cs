namespace raindesk.Features.ImportExport.Contracts;

public enum BackupErrorCode
{
    None,
    Cancelled,
    InvalidPath,
    ParseFailed,
    UnsupportedVersion,
    ValidationFailed,
    WriteFailed,
    ServiceUnavailable
}

public sealed record BackupOperationResult(
    bool Success,
    BackupErrorCode ErrorCode = BackupErrorCode.None,
    string? Error = null)
{
    public static BackupOperationResult Succeeded { get; } = new(Success: true);

    public static BackupOperationResult Failed(BackupErrorCode code, string error) =>
        new(Success: false, ErrorCode: code, Error: error);
}

/// <summary>备份导入导出的应用模块门面。</summary>
public interface IBackupService
{
    global::System.Threading.Tasks.Task<BackupOperationResult> ExportAsync(
        string destinationPath,
        CancellationToken cancellationToken = default);

    global::System.Threading.Tasks.Task<BackupOperationResult> ImportAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}

/// <summary>文件选择属于 Presentation，通过 Port 与备份用例分离。</summary>
public interface IBackupFilePicker
{
    ValueTask<string?> PickImportPathAsync();

    ValueTask<string?> PickExportPathAsync(string suggestedFileName);
}

public sealed record PluginImporterDescriptor(
    string PluginId,
    string ContributionId,
    string DisplayName,
    IReadOnlyList<string> FileExtensions);

public enum PluginImportErrorCode
{
    None,
    InvalidRequest,
    NotFound,
    Disabled,
    Cancelled,
    Timeout,
    ImportFailed
}

public sealed record PluginImportResult(
    bool Success,
    PluginImportErrorCode ErrorCode = PluginImportErrorCode.None,
    string? Error = null)
{
    public static PluginImportResult Succeeded { get; } = new(true);

    public static PluginImportResult Failed(PluginImportErrorCode code, string error) =>
        new(false, code, error);
}

/// <summary>由 composition root 显式注册的导入器贡献；输入路径必须来自应用受控选择流程。</summary>
public interface IPluginImporterContribution
{
    PluginImporterDescriptor Descriptor { get; }

    Task<PluginImportResult> ImportAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}

public interface IPluginImporterRouter
{
    IReadOnlyList<PluginImporterDescriptor> GetAvailableImporters();

    Task<PluginImportResult> ImportAsync(
        string pluginId,
        string contributionId,
        string sourcePath,
        CancellationToken cancellationToken = default);
}
