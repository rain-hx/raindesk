using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace raindesk.Controls;

/// <summary>
/// Lightweight transitions for task surfaces. Animations are limited to opacity
/// and render transforms so panel changes stay on the compositor-friendly path.
///
/// Continuous-operation safe: starting a new animation reads the current visual
/// values, stale completions are invalidated via a per-target generation, and
/// cancelled or unloaded targets keep their current visual state.
/// </summary>
internal static class SurfaceMotion
{
    private sealed class MotionState
    {
        public Storyboard? Storyboard { get; set; }
        public CompositeTransform? Transform { get; set; }
        public long Generation { get; set; }
        public bool UnloadedHookAttached { get; set; }
    }

    private static readonly ConditionalWeakTable<FrameworkElement, MotionState> States = new();

    /// <summary>Play a primary entrance transition (default vertical displacement 6px).</summary>
    public static void PlayEntrance(
        FrameworkElement target,
        double translateX = 0d,
        double translateY = MotionTokens.DisplacementPrimary,
        TimeSpan? duration = null)
        => PlayEntranceCore(target, translateX, translateY, duration);

    /// <summary>
    /// Play a secondary entrance for nested content (settings panels, editors).
    /// Displacement is capped at <see cref="MotionTokens.DisplacementSecondary"/>.
    /// </summary>
    public static void PlaySecondaryEntrance(
        FrameworkElement target,
        double translateX = 0d,
        double translateY = MotionTokens.DisplacementSecondary,
        TimeSpan? duration = null)
    {
        double y = Math.Abs(translateY) > MotionTokens.DisplacementSecondary
            ? Math.Sign(translateY) * MotionTokens.DisplacementSecondary
            : translateY;
        PlayEntranceCore(target, translateX, y, duration);
    }

    /// <summary>Play a subtle feedback entrance (3px displacement).</summary>
    public static void PlayFeedbackEntrance(
        FrameworkElement target,
        double translateX = 0d,
        double translateY = MotionTokens.DisplacementFeedback,
        TimeSpan? duration = null)
    {
        double y = Math.Abs(translateY) > MotionTokens.DisplacementFeedback
            ? Math.Sign(translateY) * MotionTokens.DisplacementFeedback
            : translateY;
        PlayEntranceCore(target, translateX, y, duration);
    }

    private static void PlayEntranceCore(
        FrameworkElement target,
        double translateX,
        double translateY,
        TimeSpan? duration)
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

        // Storyboard 省略 From，从当前视觉状态继续，避免快速连续操作闪回初始值。
        TimeSpan motionDuration = duration ?? MotionTokens.Standard;
        long generation = ++state.Generation;
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
            if (generation != state.Generation || !ReferenceEquals(state.Storyboard, storyboard)) return;
            state.Storyboard = null;
            Reset(target, transform);
        };
        storyboard.Begin();
    }

    public static void PlayExit(
        FrameworkElement target,
        Action completed,
        double translateY = MotionTokens.DisplacementFeedback,
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
        long generation = ++state.Generation;
        var storyboard = new Storyboard();
        AddAnimation(storyboard, target, nameof(UIElement.Opacity), 0d,
            motionDuration, MotionTokens.EaseIn());
        AddAnimation(storyboard, transform, nameof(CompositeTransform.TranslateY), translateY,
            motionDuration, MotionTokens.EaseIn());

        state.Storyboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            if (generation != state.Generation || !ReferenceEquals(state.Storyboard, storyboard)) return;
            state.Storyboard = null;
            Reset(target, transform);
            completed();
        };
        storyboard.Begin();
    }

    /// <summary>
    /// Cancel a running animation. When <paramref name="resetToFinalState"/> is true
    /// the target is normalized to its final state; otherwise it keeps its current
    /// visual values and pending completion callbacks are invalidated.
    /// </summary>
    public static void Cancel(FrameworkElement target, bool resetToFinalState)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!States.TryGetValue(target, out MotionState? state)) return;

        Stop(state);
        if (state.Transform != null)
        {
            if (resetToFinalState)
            {
                Reset(target, state.Transform);
            }
            else
            {
                // 保持当前视觉值，仅使过期完成回调失效。
                state.Generation++;
            }
        }
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

        if (!state.UnloadedHookAttached)
        {
            state.UnloadedHookAttached = true;
            target.Unloaded += OnTargetUnloaded;
        }

        return transform;
    }

    private static void OnTargetUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement target) return;
        if (!States.TryGetValue(target, out MotionState? state)) return;

        Stop(state);
        // 卸载后旧回调不得再更改目标视觉状态。
        state.Generation++;
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
