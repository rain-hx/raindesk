using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Util;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Workspace 分组标签拖拽预览的唯一宿主，并防止异步截图在会话结束后晚到。
/// </summary>
internal sealed class WorkspaceGroupTabDragPreviewController
{
    private DragPreviewHost? _host;
    private int _generation;

    public bool HasPreview => _host != null;

    public async System.Threading.Tasks.Task<bool> BeginAsync(
        FrameworkElement captureTarget,
        Canvas overlay,
        double width,
        double height)
    {
        ArgumentNullException.ThrowIfNull(captureTarget);
        ArgumentNullException.ThrowIfNull(overlay);
        if (_host != null)
        {
            return false;
        }

        int generation = ++_generation;
        var host = new DragPreviewHost();
        _host = host;
        try
        {
            captureTarget.UpdateLayout();
            await host.BeginDrag(
                captureTarget,
                overlay,
                width / 2d,
                height / 2d,
                1.0d);
        }
        catch
        {
            if (ReferenceEquals(_host, host))
            {
                _host = null;
            }

            host.EndDrag(animate: false);
            return false;
        }

        if (generation != _generation || !ReferenceEquals(_host, host))
        {
            host.EndDrag(animate: false);
            return false;
        }

        return true;
    }

    public void UpdatePosition(double x, double y) =>
        _host?.UpdatePosition(x, y);

    public void End()
    {
        _generation++;
        DragPreviewHost? host = _host;
        _host = null;
        host?.EndDrag(animate: false);
    }
}
