using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Owns the phased materialization and recycling rules for virtualized workspace tab containers.
/// </summary>
internal sealed class WorkspaceGroupTabContainerController
{
    private readonly WorkspaceGroupTabChromeController _chrome;
    private readonly Action<ListViewItem> _applyPlaceholderChrome;
    private readonly WorkspaceGroupTabWidthMetrics _widthMetrics;
    private readonly PointerEventHandler _pointerPressed;
    private readonly PointerEventHandler _pointerMoved;
    private readonly PointerEventHandler _pointerReleased;
    private readonly PointerEventHandler _pointerCanceled;
    private readonly Func<MenuInfo?> _getDraggingMenu;
    private readonly Func<MenuFlyout?> _getContextFlyout;
    private readonly Func<MenuInfo, bool> _isSelectionMenu;
    private readonly Action _selectionEditorRealized;

    public WorkspaceGroupTabContainerController(
        WorkspaceGroupTabChromeController chrome,
        Action<ListViewItem> applyPlaceholderChrome,
        WorkspaceGroupTabWidthMetrics widthMetrics,
        PointerEventHandler pointerPressed,
        PointerEventHandler pointerMoved,
        PointerEventHandler pointerReleased,
        PointerEventHandler pointerCanceled,
        Func<MenuInfo?> getDraggingMenu,
        Func<MenuFlyout?> getContextFlyout,
        Func<MenuInfo, bool> isSelectionMenu,
        Action selectionEditorRealized)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
        _applyPlaceholderChrome = applyPlaceholderChrome
            ?? throw new ArgumentNullException(nameof(applyPlaceholderChrome));
        _widthMetrics = widthMetrics;
        _pointerPressed = pointerPressed ?? throw new ArgumentNullException(nameof(pointerPressed));
        _pointerMoved = pointerMoved ?? throw new ArgumentNullException(nameof(pointerMoved));
        _pointerReleased = pointerReleased ?? throw new ArgumentNullException(nameof(pointerReleased));
        _pointerCanceled = pointerCanceled ?? throw new ArgumentNullException(nameof(pointerCanceled));
        _getDraggingMenu = getDraggingMenu ?? throw new ArgumentNullException(nameof(getDraggingMenu));
        _getContextFlyout = getContextFlyout ?? throw new ArgumentNullException(nameof(getContextFlyout));
        _isSelectionMenu = isSelectionMenu ?? throw new ArgumentNullException(nameof(isSelectionMenu));
        _selectionEditorRealized = selectionEditorRealized
            ?? throw new ArgumentNullException(nameof(selectionEditorRealized));
    }

    public void Handle(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is not ListViewItem item)
        {
            return;
        }

        if (args.InRecycleQueue)
        {
            ResetRecycledContainer(item);
            return;
        }

        if (args.Item is not MenuInfo menu)
        {
            return;
        }

        switch (args.Phase)
        {
            case 0:
                PrepareContainer(item, menu);
                args.RegisterUpdateCallback(Handle);
                break;
            case 1:
                ApplyIconGeometry(item, menu);
                args.RegisterUpdateCallback(Handle);
                break;
            default:
                ApplyWidth(item, menu);
                if (_isSelectionMenu(menu) && menu.MenuEdit == Visibility.Visible)
                {
                    _selectionEditorRealized();
                }
                break;
        }
    }

    public void RefreshIcon(ListView? listView, MenuInfo menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (!WorkspaceGroupTabLayoutAdapter.TryGetContainer(
                listView,
                menu,
                out ListViewItem container))
        {
            return;
        }

        ApplyIconGeometry(container, menu);
    }

    private void ResetRecycledContainer(ListViewItem item)
    {
        item.Opacity = 1;
        item.ClearValue(FrameworkElement.WidthProperty);
        item.Margin = new Thickness(0, 0, 8, 0);
        DetachPointerHandlers(item);
    }

    private void PrepareContainer(ListViewItem item, MenuInfo menu)
    {
        DetachPointerHandlers(item);
        _chrome.Apply(item);
        AttachPointerHandlers(item);

        if (ReferenceEquals(menu, _getDraggingMenu()))
        {
            _applyPlaceholderChrome(item);
        }

        if (menu.IsAllGroupsTab_NoWrite)
        {
            item.ContextFlyout = null;
        }
        else if (item.ContextFlyout == null)
        {
            item.ContextFlyout = _getContextFlyout();
        }
    }

    private static void ApplyIconGeometry(ListViewItem item, MenuInfo menu)
    {
        if (item.ContentTemplateRoot is not DependencyObject root)
        {
            return;
        }

        var iconPath = MenuGeometryHelper.FindDescendantByName<Microsoft.UI.Xaml.Shapes.Path>(
            root,
            "GroupTabIconPath");
        MenuGeometryHelper.ApplyToPath(iconPath, menu.MenuGeometry);
    }

    private void ApplyWidth(ListViewItem item, MenuInfo menu)
    {
        if (menu.MenuEdit == Visibility.Visible)
        {
            WorkspaceGroupTabSizingAdapter.ApplyEditWidth(item, _widthMetrics);
        }
        else
        {
            WorkspaceGroupTabSizingAdapter.ApplyNaturalWidth(item, _widthMetrics);
        }
    }

    private void AttachPointerHandlers(ListViewItem item)
    {
        item.AddHandler(UIElement.PointerPressedEvent, _pointerPressed, true);
        item.AddHandler(UIElement.PointerMovedEvent, _pointerMoved, true);
        item.AddHandler(UIElement.PointerReleasedEvent, _pointerReleased, true);
        item.AddHandler(UIElement.PointerCanceledEvent, _pointerCanceled, true);
    }

    private void DetachPointerHandlers(ListViewItem item)
    {
        item.RemoveHandler(UIElement.PointerPressedEvent, _pointerPressed);
        item.RemoveHandler(UIElement.PointerMovedEvent, _pointerMoved);
        item.RemoveHandler(UIElement.PointerReleasedEvent, _pointerReleased);
        item.RemoveHandler(UIElement.PointerCanceledEvent, _pointerCanceled);
    }
}
