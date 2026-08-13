using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.Util;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal sealed class LauncherDragPreviewRequest
{
    public ImageSource? IconSource { get; init; }

    public FrameworkElement? CaptureTarget { get; init; }

    public Canvas Overlay { get; init; } = null!;

    public double CaptureWidth { get; init; }

    public double CaptureHeight { get; init; }

    public bool AnimatePreview { get; init; }

    public int DragCount { get; init; }
}

/// <summary>
/// 管理 Launcher 拖拽预览的唯一宿主、异步启动竞争和结束清理。
/// </summary>
internal sealed class LauncherDragPreviewController
{
    private DragPreviewHost? _host;
    private int _generation;

    public bool HasPreview => _host != null;

    public bool IsPlacementPreviewActive => _host?.IsPlacementPreviewActive == true;

    public async System.Threading.Tasks.Task BeginAsync(
        LauncherDragPreviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        End(animate: false);

        int generation = ++_generation;
        var host = new DragPreviewHost();
        _host = host;
        if (request.CaptureTarget != null)
        {
            request.CaptureTarget.UpdateLayout();
            await host.BeginDrag(
                request.CaptureTarget,
                request.Overlay,
                request.CaptureWidth / 2d,
                request.CaptureHeight / 2d,
                request.AnimatePreview ? 1.08d : 1.0d,
                request.DragCount,
                request.CaptureWidth,
                request.CaptureHeight);
        }
        else if (request.IconSource != null)
        {
            await host.BeginDragFromImageSource(
                request.IconSource,
                request.Overlay,
                request.CaptureWidth,
                request.CaptureHeight,
                request.CaptureWidth / 2d,
                request.CaptureHeight / 2d,
                request.AnimatePreview ? 1.08d : 1.0d,
                request.DragCount);
        }
        else
        {
            _host = null;
            return;
        }

        if (generation != _generation || !ReferenceEquals(_host, host))
        {
            host.EndDrag(animate: false);
        }
    }

    public void SetSuppressed(bool suppressed) => _host?.SetSuppressed(suppressed);

    public void AnimateToPlacement(double left, double top, double targetScale = 1.0d) =>
        _host?.AnimateToPlacement(left, top, targetScale);

    public void ResumeFollowMode() => _host?.ResumeFollowMode();

    public void ExitPlacementFollowAt(double x, double y) =>
        _host?.ExitPlacementFollowAt(x, y);

    public void UpdatePosition(double x, double y) => _host?.UpdatePosition(x, y);

    public bool End(
        bool animate,
        double? targetLeft = null,
        double? targetTop = null,
        Action? completed = null)
    {
        _generation++;
        DragPreviewHost? host = _host;
        _host = null;
        if (host == null)
        {
            return false;
        }

        host.EndDrag(animate, targetLeft, targetTop, completed);
        return true;
    }
}
