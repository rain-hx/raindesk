using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace rainspace.Controls;

/// <summary>
/// Lightweight transitions for task surfaces. Animations are limited to opacity
/// and render transforms so panel changes stay on the compositor-friendly path.
/// </summary>
internal static class SurfaceMotion
{
    private sealed class MotionState
    {
        public Storyboard? Storyboard { get; set; }
        public CompositeTransform? Transform { get; set; }
    }

    private static readonly ConditionalWeakTable<FrameworkElement, MotionState> States = new();

    public static void PlayEntrance(
        FrameworkElement target,
        double translateX = 0d,
        double translateY = 8d,
        TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        MotionState state = States.GetValue(target, static _ => new MotionState());
        Stop(state);

        CompositeTransform transform = EnsureTransform(target, state);
        if (!MotionPreferences.ShouldAnimate)
        {
            Reset(target, transform);
            return;
        }

        target.Opacity = 0d;
        transform.TranslateX = translateX;
        transform.TranslateY = translateY;

        TimeSpan motionDuration = duration ?? MotionTokens.Standard;
        var storyboard = new Storyboard();
        AddAnimation(storyboard, target, nameof(UIElement.Opacity), 1d,
            TimeSpan.FromMilliseconds(Math.Min(180d, motionDuration.TotalMilliseconds)), MotionTokens.EaseOut());
        AddAnimation(storyboard, transform, nameof(CompositeTransform.TranslateX), 0d,
            motionDuration, MotionTokens.EaseOut());
        AddAnimation(storyboard, transform, nameof(CompositeTransform.TranslateY), 0d,
            motionDuration, MotionTokens.EaseOut());

        state.Storyboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(state.Storyboard, storyboard)) return;
            state.Storyboard = null;
            Reset(target, transform);
        };
        storyboard.Begin();
    }

    public static void PlayExit(
        FrameworkElement target,
        Action completed,
        double translateY = 4d,
        TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(completed);

        MotionState state = States.GetValue(target, static _ => new MotionState());
        Stop(state);
        CompositeTransform transform = EnsureTransform(target, state);
        if (!MotionPreferences.ShouldAnimate)
        {
            Reset(target, transform);
            completed();
            return;
        }

        TimeSpan motionDuration = duration ?? MotionTokens.Exit;
        var storyboard = new Storyboard();
        AddAnimation(storyboard, target, nameof(UIElement.Opacity), 0d,
            motionDuration, MotionTokens.EaseIn());
        AddAnimation(storyboard, transform, nameof(CompositeTransform.TranslateY), translateY,
            motionDuration, MotionTokens.EaseIn());

        state.Storyboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(state.Storyboard, storyboard)) return;
            state.Storyboard = null;
            Reset(target, transform);
            completed();
        };
        storyboard.Begin();
    }

    private static CompositeTransform EnsureTransform(FrameworkElement target, MotionState state)
    {
        if (state.Transform != null && ReferenceEquals(target.RenderTransform, state.Transform))
        {
            return state.Transform;
        }

        if (target.RenderTransform is CompositeTransform existing)
        {
            state.Transform = existing;
            return existing;
        }

        var transform = new CompositeTransform();
        target.RenderTransform = transform;
        state.Transform = transform;
        return transform;
    }

    private static void AddAnimation(
        Storyboard storyboard,
        DependencyObject target,
        string property,
        double to,
        TimeSpan duration,
        EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = false
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }

    private static void Stop(MotionState state)
    {
        state.Storyboard?.Stop();
        state.Storyboard = null;
    }

    private static void Reset(FrameworkElement target, CompositeTransform transform)
    {
        target.Opacity = 1d;
        transform.TranslateX = 0d;
        transform.TranslateY = 0d;
    }
}
