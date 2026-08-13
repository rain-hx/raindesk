using raindesk.Features.Launcher.Contracts;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>管理外部文件导入的 UI 生命周期，防止旧批次结果覆盖新批次。</summary>
public sealed class LauncherExternalImportCoordinator : IDisposable
{
    private readonly ILauncherExternalImportService _service;
    private CancellationTokenSource? _cancellation;
    private int _generation;

    public LauncherExternalImportCoordinator(ILauncherExternalImportService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async global::System.Threading.Tasks.Task<LauncherExternalImportResult?> ImportAsync(
        string targetGroupId,
        IReadOnlyList<string> paths)
    {
        CancellationTokenSource cancellation = ReplaceCancellation();
        int generation = ++_generation;
        try
        {
            LauncherExternalImportResult result = await _service
                .ImportAsync(targetGroupId, paths, cancellation.Token)
                .ConfigureAwait(false);
            return generation == _generation && !cancellation.IsCancellationRequested
                ? result
                : null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return null;
        }
    }

    public void CancelPending()
    {
        _generation++;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    public void Dispose() => CancelPending();

    private CancellationTokenSource ReplaceCancellation()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        return _cancellation;
    }
}
