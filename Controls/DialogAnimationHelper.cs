using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace raindesk.Controls
{
    internal static class DialogAnimationHelper
    {
        private const int EntranceDurationMs = 320;
        private const int ExitDurationMs = 220;

        private static bool IsEnabled => MotionPreferences.ShouldAnimate;

        public static void PlayEntrance(FrameworkElement target, Action? onComplete = null)
        {
            if (!IsEnabled)
            {
                EnsureVisible(target);
                onComplete?.Invoke();
                return;
            }

            try
            {
                target.RenderTransformOrigin = new Point(0.5, 0.5);
                var transform = new CompositeTransform();
                target.RenderTransform = transform;

                var storyboard = new Storyboard();
                var motionDuration = TimeSpan.FromMilliseconds(EntranceDurationMs);
                var fadeDuration = TimeSpan.FromMilliseconds(220);
                var motionEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
                var fadeEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };

                AddDoubleAnimation(storyboard, transform, "ScaleX", 0.96, 1, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "ScaleY", 0.96, 1, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "TranslateY", 8, 0, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, target, "Opacity", 0, 1, fadeDuration, fadeEase);

                storyboard.Completed += (_, _) =>
                {
                    EnsureVisible(target);
                    onComplete?.Invoke();
                };
                storyboard.Begin();
            }
            catch
            {
                EnsureVisible(target);
                onComplete?.Invoke();
            }
        }

        public static void PlayExit(FrameworkElement target, Action onComplete)
        {
            if (!IsEnabled)
            {
                onComplete();
                return;
            }

            try
            {
                CompositeTransform transform = EnsureTransform(target);
                var storyboard = new Storyboard();
                var motionDuration = TimeSpan.FromMilliseconds(ExitDurationMs);
                var fadeDuration = TimeSpan.FromMilliseconds(180);
                var motionEase = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                var fadeEase = new QuadraticEase { EasingMode = EasingMode.EaseIn };

                AddDoubleAnimation(storyboard, transform, "ScaleX", transform.ScaleX, 0.96, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "ScaleY", transform.ScaleY, 0.96, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "TranslateY", transform.TranslateY, 6, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, target, "Opacity", target.Opacity, 0, fadeDuration, fadeEase);

                storyboard.Completed += (_, _) => onComplete();
                storyboard.Begin();
            }
            catch
            {
                onComplete();
            }
        }

        public static FrameworkElement ResolveAnimationTarget(FrameworkElement content)
        {
            if (content.FindName("DialogAnimationRoot") is FrameworkElement root)
            {
                return root;
            }

            return content;
        }

        private static void EnsureVisible(FrameworkElement target)
        {
            target.Opacity = 1;
            ResetTransform(target);
        }

        private static CompositeTransform EnsureTransform(FrameworkElement target)
        {
            target.RenderTransformOrigin = new Point(0.5, 0.5);
            if (target.RenderTransform is CompositeTransform transform)
            {
                return transform;
            }

            transform = new CompositeTransform();
            target.RenderTransform = transform;
            return transform;
        }

        private static void ResetTransform(FrameworkElement target)
        {
            if (target.RenderTransform is CompositeTransform transform)
            {
                transform.ScaleX = 1;
                transform.ScaleY = 1;
                transform.TranslateX = 0;
                transform.TranslateY = 0;
            }
        }

        private static void AddDoubleAnimation(
            Storyboard storyboard,
            DependencyObject target,
            string property,
            double from,
            double to,
            TimeSpan duration,
            EasingFunctionBase easing)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = easing
            };

            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
        }
    }
}
