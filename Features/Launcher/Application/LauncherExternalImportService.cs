using raindesk.Features.Launcher.Contracts;

namespace raindesk.Features.Launcher.Application;

/// <summary>协调外部路径解析与单次批量 Launcher 写入。</summary>
public sealed class LauncherExternalImportService : ILauncherExternalImportService
{
    private readonly ILauncherShortcutDraftSource _draftSource;
    private readonly ILauncherCommandHandler _commandHandler;

    public LauncherExternalImportService(
        ILauncherShortcutDraftSource draftSource,
        ILauncherCommandHandler commandHandler)
    {
        _draftSource = draftSource ?? throw new ArgumentNullException(nameof(draftSource));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    public async global::System.Threading.Tasks.Task<LauncherExternalImportResult> ImportAsync(
        string targetGroupId,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetGroupId))
        {
            return Failed("目标分组不能为空。");
        }
        if (paths == null || paths.Count == 0)
        {
            return Failed("没有可导入的外部文件。");
        }

        var drafts = new List<LauncherShortcutDraft>(paths.Count);
        var errors = new List<LauncherExternalImportError>();
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LauncherShortcutDraftResolution resolution;
            try
            {
                resolution = await _draftSource.ResolveAsync(path, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                resolution = LauncherShortcutDraftResolution.Failed(
                    path,
                    "解析外部文件失败: " + ex.Message);
            }

            if (resolution.Success)
            {
                drafts.Add(resolution.Draft!);
            }
            else
            {
                errors.Add(new LauncherExternalImportError(
                    resolution.SourcePath,
                    resolution.Error ?? "无法解析外部文件。"));
            }
        }

        if (drafts.Count == 0)
        {
            return new LauncherExternalImportResult(false, 0, errors, "没有成功解析的快捷方式。");
        }

        LauncherCommandResult commandResult = _commandHandler.AddMany(
            new AddLauncherShortcutsCommand(
                targetGroupId,
                drafts,
                ApplyConfiguredSort: true));
        if (!commandResult.Success)
        {
            return new LauncherExternalImportResult(
                false,
                0,
                errors,
                commandResult.Error ?? "批量添加快捷方式失败。");
        }

        return new LauncherExternalImportResult(true, drafts.Count, errors);
    }

    private static LauncherExternalImportResult Failed(string error) =>
        new(false, 0, Array.Empty<LauncherExternalImportError>(), error);
}
