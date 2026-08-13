using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using raindesk.Controls;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal static class LauncherDropTargetVisualAdapter
{
    public static void Apply(
        ListView listView,
        IconInfo? target,
        bool active,
        bool shouldAnimate)
    {
        ArgumentNullException.ThrowIfNull(listView);
        if (target == null
            || listView.ContainerFromItem(target) is not ListViewItem item
            || item.ContentTemplateRoot is not DependencyObject templateRoot)
        {
            return;
        }

        FrameworkElement? visualRoot = target.IsFolder
            ? MenuGeometryHelper.FindDescendantByName<Grid>(templateRoot, "FolderVisualRoot")
            : MenuGeometryHelper.FindDescendantByName<Grid>(templateRoot, "ShortcutIconHost");
        if (visualRoot == null)
        {
            return;
        }

        AnimateScale(
            visualRoot,
            active ? (target.IsFolder ? 1.08d : 1.06d) : 1d,
            shouldAnimate);

        Border? surface = target.IsFolder
            ? FindDescendant<Border>(visualRoot)
            : MenuGeometryHelper.FindDescendantByName<Border>(templateRoot, "ShortcutIconSurface");
        if (surface != null)
        {
            surface.BorderThickness = active ? new Thickness(2d) : new Thickness(0d);
            surface.Opacity = active ? 0.96d : 1d;
        }
    }

    private static void AnimateScale(
        FrameworkElement element,
        double targetScale,
        bool shouldAnimate)
    {
        element.RenderTransformOrigin = new global::Windows.Foundation.Point(0.5d, 0.5d);
        if (element.RenderTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform { ScaleX = 1d, ScaleY = 1d };
            element.RenderTransform = scale;
        }

        if (!shouldAnimate)
        {
            scale.ScaleX = targetScale;
            scale.ScaleY = targetScale;
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var scaleX = new DoubleAnimation
        {
            From = scale.ScaleX,
            To = targetScale,
            Duration = MotionTokens.Fast,
            EasingFunction = easing
        };
        var scaleY = new DoubleAnimation
        {
            From = scale.ScaleY,
            To = targetScale,
            Duration = MotionTokens.Fast,
            EasingFunction = easing
        };
        Storyboard.SetTarget(scaleX, scale);
        Storyboard.SetTargetProperty(scaleX, nameof(ScaleTransform.ScaleX));
        Storyboard.SetTarget(scaleY, scale);
        Storyboard.SetTargetProperty(scaleY, nameof(ScaleTransform.ScaleY));

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Begin();
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            T? nested = FindDescendant<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
