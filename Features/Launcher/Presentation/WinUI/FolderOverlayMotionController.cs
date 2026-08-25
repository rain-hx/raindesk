using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using raindesk.Controls;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal sealed class FolderOverlayMotionController
{
    private Storyboard? _storyboard;

    public void Enter(FrameworkElement overlay, FrameworkElement panel, bool animate)
    {
        Stop();
        Prepare(panel, out CompositeTransform transform);
        Reset(overlay, panel, transform);
        if (!animate) return;

        overlay.Opacity = 0d;
        panel.Opacity = 1d;
        transform.ScaleX = MotionTokens.FolderOverlayEnterScale;
        transform.ScaleY = MotionTokens.FolderOverlayEnterScale;
        transform.TranslateY = MotionTokens.DisplacementSecondary;
        _storyboard = Build(overlay, panel, transform, 1d, 1d, 0d,
            MotionTokens.Standard, MotionTokens.EaseOut());
        _storyboard.Begin();
    }

    public void Exit(FrameworkElement overlay, FrameworkElement panel, bool animate, Action completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        Stop();
        Prepare(panel, out CompositeTransform transform);
        if (!animate)
        {
            Reset(overlay, panel, transform);
            completed();
            return;
        }

        _storyboard = Build(overlay, panel, transform, 0d, 0.98d, MotionTokens.DisplacementSecondary,
            MotionTokens.Exit, MotionTokens.EaseIn());
        Storyboard active = _storyboard;
        active.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_storyboard, active)) return;
            _storyboard = null;
            Reset(overlay, panel, transform);
            completed();
        };
        active.Begin();
    }

    public void Stop()
    {
        _storyboard?.Stop();
        _storyboard = null;
    }

    private static Storyboard Build(FrameworkElement overlay, FrameworkElement panel,
        CompositeTransform transform, double overlayOpacity, double panelScale, double panelTranslateY,
        TimeSpan duration, EasingFunctionBase easing)
    {
        var storyboard = new Storyboard();
        storyboard.Children.Add(Animation(overlay, "Opacity", overlayOpacity, duration, easing));
        storyboard.Children.Add(Animation(transform, nameof(CompositeTransform.ScaleX), panelScale, duration, easing));
        storyboard.Children.Add(Animation(transform, nameof(CompositeTransform.ScaleY), panelScale, duration, easing));
        storyboard.Children.Add(Animation(transform, nameof(CompositeTransform.TranslateY), panelTranslateY, duration, easing));
        return storyboard;
    }

    private static DoubleAnimation Animation(DependencyObject target, string property,
        double to, TimeSpan duration, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation { To = to, Duration = duration, EasingFunction = easing };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        return animation;
    }

    private static void Prepare(FrameworkElement panel, out CompositeTransform transform)
    {
        panel.RenderTransformOrigin = new Point(0.5d, 0.5d);
        transform = panel.RenderTransform as CompositeTransform ?? new CompositeTransform();
        panel.RenderTransform = transform;
    }

    private static void Reset(FrameworkElement overlay, FrameworkElement panel, CompositeTransform transform)
    {
        overlay.Opacity = 1d;
        panel.Opacity = 1d;
        transform.ScaleX = 1d;
        transform.ScaleY = 1d;
        transform.TranslateY = 0d;
    }
}
