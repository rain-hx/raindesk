using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using raindesk.Controls;
using Windows.Foundation;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Workspace 分组标签删除的淡出和缩放。避免对 Width/Margin 做布局属性动画，
/// 布局收缩由容器在动画完成后一次性处理。
/// </summary>
internal sealed class WorkspaceGroupTabDeleteAnimationController
{
    public System.Threading.Tasks.Task AnimateOutAsync(
        ListViewItem container,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(container);
        var completion = new System.Threading.Tasks.TaskCompletionSource<bool>();
        var easing = MotionTokens.EaseIn();

        container.IsHitTestVisible = false;
        container.RenderTransformOrigin = new Point(0.5d, 0.5d);
        var scale = new ScaleTransform { ScaleX = 1d, ScaleY = 1d };
        container.RenderTransform = scale;

        var storyboard = new Storyboard { FillBehavior = FillBehavior.Stop };
        var fadeOut = new DoubleAnimation
        {
            From = 1d,
            To = 0d,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fadeOut, container);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");

        var scaleX = new DoubleAnimation
        {
            From = 1d,
            To = 0.72d,
            Duration = duration,
            EasingFunction = easing
        };
        var scaleY = new DoubleAnimation
        {
            From = 1d,
            To = 0.72d,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(scaleX, scale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        Storyboard.SetTarget(scaleY, scale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        storyboard.Children.Add(fadeOut);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);

        storyboard.Completed += (_, _) =>
        {
            try
            {
                container.RenderTransform = null;
            }
            catch
            {
                // 容器可能已被回收。
            }

            completion.TrySetResult(true);
        };
        storyboard.Begin();
        return completion.Task;
    }

    public void ScheduleTransitionRestore(
        DispatcherQueue dispatcherQueue,
        Action restore)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        ArgumentNullException.ThrowIfNull(restore);
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => restore());
        });
    }
}
