using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace raindesk.Controls
{
    internal static class DialogAnimationHelper
    {
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
                CompositeTransform transform = EnsureTransform(target);

                // 首次进入（元素处于静止终态）先写入起始值；否则视为从中途续播，不重置视觉状态。
                if (target.Opacity >= 0.99d)
                {
                    transform.ScaleX = MotionTokens.DialogEnterScale;
                    transform.ScaleY = MotionTokens.DialogEnterScale;
                    transform.TranslateY = MotionTokens.DisplacementSecondary;
                    target.Opacity = 0d;
                }

                var storyboard = new Storyboard();
                var motionDuration = MotionTokens.Standard;
                var fadeDuration = TimeSpan.FromMilliseconds(Math.Min(180d, motionDuration.TotalMilliseconds));
                var motionEase = MotionTokens.EaseOut();

                AddDoubleAnimation(storyboard, transform, "ScaleX", 1, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "ScaleY", 1, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "TranslateY", 0, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, target, "Opacity", 1, fadeDuration, motionEase);

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
                var motionDuration = MotionTokens.Exit;
                var fadeDuration = TimeSpan.FromMilliseconds(Math.Min(150d, motionDuration.TotalMilliseconds));
                var motionEase = MotionTokens.EaseIn();

                // 省略 From：从当前视觉状态收敛到退出终态，中途再次打开可反向续播。
                AddDoubleAnimation(storyboard, transform, "ScaleX", MotionTokens.DialogExitScale, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "ScaleY", MotionTokens.DialogExitScale, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, transform, "TranslateY", MotionTokens.DisplacementFeedback, motionDuration, motionEase);
                AddDoubleAnimation(storyboard, target, "Opacity", 0, fadeDuration, motionEase);

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
            double to,
            TimeSpan duration,
            EasingFunctionBase easing)
        {
            var animation = new DoubleAnimation
            {
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
