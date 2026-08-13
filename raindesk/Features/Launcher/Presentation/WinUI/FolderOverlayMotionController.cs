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
        Prepare(panel, out ScaleTransform scale);
        Reset(overlay, panel, scale);
        if (!animate) return;

        overlay.Opacity = 0d;
        panel.Opacity = 0d;
        scale.ScaleX = 0.96d;
        scale.ScaleY = 0.96d;
        _storyboard = Build(overlay, panel, scale, 1d, 1d, 1d,
            MotionTokens.Standard, MotionTokens.EaseOut());
        _storyboard.Begin();
    }

    public void Exit(FrameworkElement overlay, FrameworkElement panel, bool animate, Action completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        Stop();
        Prepare(panel, out ScaleTransform scale);
        if (!animate)
        {
            Reset(overlay, panel, scale);
            completed();
            return;
        }

        _storyboard = Build(overlay, panel, scale, 0d, 0d, 0.98d,
            MotionTokens.Exit, MotionTokens.EaseIn());
        Storyboard active = _storyboard;
        active.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_storyboard, active)) return;
            _storyboard = null;
            Reset(overlay, panel, scale);
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
        ScaleTransform scale, double overlayOpacity, double panelOpacity, double panelScale,
        TimeSpan duration, EasingFunctionBase easing)
    {
        var storyboard = new Storyboard();
        storyboard.Children.Add(Animation(overlay, "Opacity", overlayOpacity, duration, easing));
        storyboard.Children.Add(Animation(panel, "Opacity", panelOpacity, duration, easing));
        storyboard.Children.Add(Animation(scale, nameof(ScaleTransform.ScaleX), panelScale, duration, easing));
        storyboard.Children.Add(Animation(scale, nameof(ScaleTransform.ScaleY), panelScale, duration, easing));
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

    private static void Prepare(FrameworkElement panel, out ScaleTransform scale)
    {
        panel.RenderTransformOrigin = new Point(0.5d, 0.5d);
        scale = panel.RenderTransform as ScaleTransform ?? new ScaleTransform();
        panel.RenderTransform = scale;
    }

    private static void Reset(FrameworkElement overlay, FrameworkElement panel, ScaleTransform scale)
    {
        overlay.Opacity = 1d;
        panel.Opacity = 1d;
        scale.ScaleX = 1d;
        scale.ScaleY = 1d;
    }
}
