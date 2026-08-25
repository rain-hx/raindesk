using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.Features.Launcher.Application;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

public static class LauncherShortcutItemSpanAdapter
{
    public static void Apply(
        ListView listView,
        ListViewItem item,
        IconInfo icon,
        int columnCount,
        double availableWidth)
    {
        if (item.ContentTemplateRoot is FrameworkElement templateRoot)
        {
            ResetTemplate(templateRoot);
        }

        int columnSpan = ShortcutLayoutCalculator.ClampFolderColumnSpan(
            GetColumnSpan(icon),
            columnCount);
        int rowSpan = GetRowSpan(icon);
        bool supportsVariableSize = SupportsVariableSize(listView.ItemsPanelRoot);
        bool supportsGridSpans = listView.ItemsPanelRoot is LauncherGridPanel or VariableSizedWrapGrid;

        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        ShortcutTileDisplaySize size = ShortcutLayoutCalculator.CalculatePanelTileDisplaySize(
            metrics,
            columnSpan,
            rowSpan,
            supportsVariableSize);

        if (listView.ItemsPanelRoot is LauncherGridPanel)
        {
            ClearContainerSizeAndSpans(item);
        }
        else if (supportsVariableSize)
        {
            item.Width = size.Width;
            item.Height = size.Height;
            if (supportsGridSpans)
            {
                item.SetValue(VariableSizedWrapGrid.ColumnSpanProperty, columnSpan);
                item.SetValue(VariableSizedWrapGrid.RowSpanProperty, rowSpan);
            }
            else
            {
                ClearGridSpans(item);
            }
        }
        else
        {
            ClearContainerSizeAndSpans(item);
        }
    }

    public static void ApplyMargin(ListViewItem item, int index, int itemCount)
    {
        item.Margin = new Thickness(0d);
    }

    public static void ResetTemplate(FrameworkElement? templateRoot)
    {
        if (templateRoot == null)
        {
            return;
        }

        templateRoot.ClearValue(FrameworkElement.WidthProperty);
        templateRoot.ClearValue(FrameworkElement.HeightProperty);
        templateRoot.RenderTransform = null;

        if (MenuGeometryHelper.FindDescendantByName<Grid>(
                templateRoot,
                "FolderVisualRoot") is Grid folderVisual)
        {
            folderVisual.ClearValue(FrameworkElement.WidthProperty);
            folderVisual.ClearValue(FrameworkElement.HeightProperty);
        }

        ForEachDescendant<Image>(templateRoot, image =>
            image.ClearValue(UIElement.RenderTransformProperty));
    }

    public static int GetColumnSpan(IconInfo icon)
    {
        if (icon.IsDragYieldPlaceholder_NoWrite)
        {
            return Math.Max(1, icon.DragPlaceholderColumnSpan_NoWrite);
        }
        return 1;
    }

    public static int GetRowSpan(IconInfo icon)
    {
        if (icon.IsDragYieldPlaceholder_NoWrite)
        {
            return Math.Max(1, icon.DragPlaceholderRowSpan_NoWrite);
        }
        return 1;
    }

    public static bool SupportsVariableSize(Panel? panel) =>
        panel is LauncherGridPanel or VariableSizedWrapGrid;

    private static void ClearContainerSizeAndSpans(ListViewItem item)
    {
        item.ClearValue(FrameworkElement.WidthProperty);
        item.ClearValue(FrameworkElement.HeightProperty);
        ClearGridSpans(item);
    }

    private static void ClearGridSpans(ListViewItem item)
    {
        item.ClearValue(VariableSizedWrapGrid.ColumnSpanProperty);
        item.ClearValue(VariableSizedWrapGrid.RowSpanProperty);
    }

    private static void ForEachDescendant<T>(DependencyObject root, Action<T> action)
        where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                action(typed);
            }
            ForEachDescendant(child, action);
        }
    }
}
