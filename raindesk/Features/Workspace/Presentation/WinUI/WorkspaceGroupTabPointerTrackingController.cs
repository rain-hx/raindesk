using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// 统一持有分组标签拖拽期间的窗口级指针事件订阅。
/// </summary>
internal sealed class WorkspaceGroupTabPointerTrackingController
{
    private UIElement? _root;
    private PointerEventHandler? _movedHandler;
    private PointerEventHandler? _releasedHandler;

    public bool IsTracking => _root != null;

    public void Begin(
        UIElement root,
        PointerEventHandler movedHandler,
        PointerEventHandler releasedHandler)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(movedHandler);
        ArgumentNullException.ThrowIfNull(releasedHandler);
        End();

        _root = root;
        _movedHandler = movedHandler;
        _releasedHandler = releasedHandler;
        root.AddHandler(UIElement.PointerMovedEvent, movedHandler, true);
        root.AddHandler(UIElement.PointerReleasedEvent, releasedHandler, true);
        root.AddHandler(UIElement.PointerCanceledEvent, releasedHandler, true);
    }

    public void End()
    {
        if (_root == null)
        {
            return;
        }

        if (_movedHandler != null)
        {
            _root.RemoveHandler(UIElement.PointerMovedEvent, _movedHandler);
        }

        if (_releasedHandler != null)
        {
            _root.RemoveHandler(UIElement.PointerReleasedEvent, _releasedHandler);
            _root.RemoveHandler(UIElement.PointerCanceledEvent, _releasedHandler);
        }

        _root = null;
        _movedHandler = null;
        _releasedHandler = null;
    }
}
