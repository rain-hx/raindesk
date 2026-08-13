using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace rainspace.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Workspace 分组标签拖拽占位宽度、容器可见性与重排 Transition 的唯一所有者。
/// </summary>
internal sealed class WorkspaceGroupTabDragVisualController
{
    private TransitionCollection? _savedTransitions;

    public double PlaceholderHeight { get; private set; }

    public double CapturePlaceholderHeight(ListViewItem container, double fallbackHeight = 56d)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (PlaceholderHeight <= 0d)
        {
            container.UpdateLayout();
            PlaceholderHeight = container.ActualHeight > 0d
                ? container.ActualHeight
                : fallbackHeight;
        }

        return PlaceholderHeight;
    }

    public void ApplyPlaceholderChrome(ListViewItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Visibility = Visibility.Visible;
        item.Opacity = 0d;
        item.IsHitTestVisible = false;
        if (PlaceholderHeight > 0d)
        {
            item.MinHeight = PlaceholderHeight;
        }
    }

    public void RestoreContainer(ListViewItem container, Action<ListViewItem> applyNaturalWidth)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(applyNaturalWidth);
        container.Visibility = Visibility.Visible;
        container.Opacity = 1d;
        container.IsHitTestVisible = true;
        container.ClearValue(FrameworkElement.MinHeightProperty);
        container.ClearValue(FrameworkElement.HeightProperty);
        applyNaturalWidth(container);
    }

    public void EnableRepositionTransitions(ListView listView, TimeSpan desiredDuration)
    {
        ArgumentNullException.ThrowIfNull(listView);
        _savedTransitions ??= listView.ItemContainerTransitions;
        listView.ItemContainerTransitions = new TransitionCollection
        {
            new RepositionThemeTransition()
        };
        if (desiredDuration > TimeSpan.Zero)
        {
            listView.SetDesiredContainerUpdateDuration(desiredDuration);
        }
    }

    public void EnableDeleteTransitions(ListView listView, TimeSpan desiredDuration)
    {
        ArgumentNullException.ThrowIfNull(listView);
        _savedTransitions ??= listView.ItemContainerTransitions;
        listView.ItemContainerTransitions = new TransitionCollection
        {
            new RepositionThemeTransition { IsStaggeringEnabled = false }
        };
        listView.SetDesiredContainerUpdateDuration(desiredDuration);
    }

    public void RestoreTransitions(ListView listView)
    {
        ArgumentNullException.ThrowIfNull(listView);
        listView.ItemContainerTransitions = _savedTransitions ?? new TransitionCollection();
        _savedTransitions = null;
    }

    public void EndInteraction() => PlaceholderHeight = 0d;
}
