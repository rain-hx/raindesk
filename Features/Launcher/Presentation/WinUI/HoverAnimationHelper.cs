using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using raindesk.Controls;
using raindesk.Util;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Static helpers for icon hover scale animations.
/// Extracted from RightCardControl to reduce page code size.
/// </summary>
internal static class HoverAnimationHelper
{
    public static void ApplyHoverScale(object sender, double targetScale, int milliseconds)
    {
        try
        {
            if (sender is not FrameworkElement element) return;

            Image? img = FindDescendant<Image>(element);
            if (img == null) return;

            if (TryGetHoverStoryboard(img, out Storyboard? activeStoryboard) && activeStoryboard != null)
            {
                activeStoryboard.Stop();
                RemoveHoverStoryboard(img);
            }

            img.RenderTransformOrigin = new Point(0.5, 0.5);
            if (img.RenderTransform is not ScaleTransform scale)
            {
                scale = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
                img.RenderTransform = scale;
            }

            if (!MotionPreferences.ShouldAnimate)
            {
                scale.ScaleX = targetScale;
                scale.ScaleY = targetScale;
                return;
            }

            Storyboard myStoryboard = new Storyboard();

            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                From = scale.ScaleX,
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(Math.Max(milliseconds, 80))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                From = scale.ScaleY,
                To = targetScale,
                Duration = new Duration(TimeSpan.FromMilliseconds(Math.Max(milliseconds, 80))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(scaleXAnimation, scale);
            Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
            Storyboard.SetTarget(scaleYAnimation, scale);
            Storyboard.SetTargetProperty(scaleYAnimation, nameof(ScaleTransform.ScaleY));

            myStoryboard.Children.Add(scaleXAnimation);
            myStoryboard.Children.Add(scaleYAnimation);
            img.Resources["HoverStoryboard"] = myStoryboard;
            myStoryboard.Begin();
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "图标悬停动画失败");
        }
    }

    public static bool TryGetHoverStoryboard(Image img, out Storyboard? storyboard)
    {
        storyboard = null;
        try
        {
            if (!img.Resources.ContainsKey("HoverStoryboard"))
            {
                return false;
            }

            if (img.Resources["HoverStoryboard"] is Storyboard activeStoryboard)
            {
                storyboard = activeStoryboard;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static void RemoveHoverStoryboard(Image img)
    {
        try
        {
            if (img.Resources.ContainsKey("HoverStoryboard"))
            {
                img.Resources.Remove("HoverStoryboard");
            }
        }
        catch
        {
            // 容器回收或布局刷新期间资源可能已经被 WinUI 清理
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            T? descendant = FindDescendant<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
