using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Static visual-tree helpers for Launcher icon and group resolution.
/// Extracted from RightCardControl to reduce page code size.
/// </summary>
internal static class LauncherVisualTreeHelper
{
    public static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
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

    public static IconInfo? ResolveIconFromFlyoutTarget(DependencyObject? target)
    {
        while (target != null)
        {
            switch (target)
            {
                case ListViewItem { Content: IconInfo listIcon }:
                    return listIcon;
                case FrameworkElement { Tag: IconInfo tagIcon }:
                    return tagIcon;
            }

            target = VisualTreeHelper.GetParent(target);
        }

        return null;
    }

    public static MenuInfo? ResolveGroupFromContextTarget(DependencyObject? target)
    {
        while (target != null)
        {
            switch (target)
            {
                case ListViewItem { Content: MenuInfo menu }:
                    return menu;
                case FrameworkElement { Tag: MenuInfo tagMenu }:
                    return tagMenu;
            }

            target = VisualTreeHelper.GetParent(target);
        }

        return null;
    }

    public static IconInfo? GetIconInfoFromSender(object sender)
    {
        return (sender as FrameworkElement)?.Tag as IconInfo;
    }

    public static Border? FindShortcutIconBorder(DependencyObject root)
    {
        return MenuGeometryHelper.FindDescendantByName<Border>(root, "ShortcutIconFrame");
    }

    public static FrameworkElement? FindShortcutDragCaptureTarget(ListViewItem container)
    {
        if (container.ContentTemplateRoot is DependencyObject root)
        {
            if (MenuGeometryHelper.FindDescendantByName<Grid>(root, "ShortcutIconHost") is Grid iconHost)
            {
                return iconHost;
            }

            if (MenuGeometryHelper.FindDescendantByName<Grid>(root, "FolderVisualRoot") is Grid folderVisual)
            {
                return folderVisual;
            }

            if (MenuGeometryHelper.FindDescendantByName<Border>(root, "ShortcutIconFrame") is Border iconFrame
                && iconFrame.Parent is Grid frameHost)
            {
                return frameHost;
            }
        }

        return container.ContentTemplateRoot as FrameworkElement;
    }

    public static void AnimateContainerFadeIn(ListViewItem container)
    {
        container.Opacity = 0;
        var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, container);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");
        var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        sb.Children.Add(fadeIn);
        sb.Begin();
    }
}
