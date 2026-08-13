using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal sealed class LauncherShortcutContainerStateController
{
    private readonly ListView _listView;
    private readonly LauncherItemAnimationCoordinator _animations;
    private readonly Func<bool> _isCustomDragActive;
    private readonly Predicate<IconInfo> _isDraggedIcon;
    private readonly Action<ListViewItem, IconInfo?> _applyDraggedState;
    private readonly Action _refreshItemSpans;

    public LauncherShortcutContainerStateController(
        ListView listView,
        LauncherItemAnimationCoordinator animations,
        Func<bool> isCustomDragActive,
        Predicate<IconInfo> isDraggedIcon,
        Action<ListViewItem, IconInfo?> applyDraggedState,
        Action refreshItemSpans)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        _animations = animations ?? throw new ArgumentNullException(nameof(animations));
        _isCustomDragActive = isCustomDragActive
            ?? throw new ArgumentNullException(nameof(isCustomDragActive));
        _isDraggedIcon = isDraggedIcon ?? throw new ArgumentNullException(nameof(isDraggedIcon));
        _applyDraggedState = applyDraggedState
            ?? throw new ArgumentNullException(nameof(applyDraggedState));
        _refreshItemSpans = refreshItemSpans
            ?? throw new ArgumentNullException(nameof(refreshItemSpans));
    }

    public void Restore(IReadOnlyCollection<IconInfo> icons)
    {
        ArgumentNullException.ThrowIfNull(icons);

        int itemCount = icons.Count;
        for (int index = 0; index < itemCount; index++)
        {
            try
            {
                RestoreContainer(index);
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }
        }

        _refreshItemSpans();
    }

    private void RestoreContainer(int index)
    {
        if (_listView.ContainerFromIndex(index) is not ListViewItem item)
        {
            return;
        }

        if (_isCustomDragActive()
            && item.Content is IconInfo icon
            && _isDraggedIcon(icon))
        {
            _applyDraggedState(item, icon);
            return;
        }

        item.Opacity = 1d;
        item.Visibility = Visibility.Visible;
        item.IsHitTestVisible = true;
        if (item.ContentTemplateRoot is not FrameworkElement root)
        {
            return;
        }

        root.Opacity = 1d;
        root.Visibility = Visibility.Visible;
        root.IsHitTestVisible = true;
        if (!_animations.Contains(item))
        {
            root.RenderTransform = null;
        }
    }
}
