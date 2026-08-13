using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using rainspace.Constant;
using rainspace.Features.Launcher.Application;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation.WinUI;

public static class LauncherFolderTileVisualAdapter
{
    private const double ShortcutIconSize = 64d;
    private const double LabelBlockHeight = 22d;
    private const double StackSpacing = 8d;
    private const double PreviewSafePadding = 6d;
    private const double PreviewIconGap = 4d;

    public static void Apply(
        ListView listView,
        ListViewItem item,
        IconInfo icon,
        int columnCount,
        double availableWidth)
    {
        if (!icon.IsFolder || item.ContentTemplateRoot is not FrameworkElement templateRoot)
        {
            return;
        }
        if (!LauncherShortcutItemSpanAdapter.SupportsVariableSize(listView.ItemsPanelRoot))
        {
            LauncherShortcutItemSpanAdapter.ResetTemplate(templateRoot);
            return;
        }

        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        int columnSpan = ShortcutLayoutCalculator.ClampFolderColumnSpan(
            icon.FolderColumnSpan_NoWrite,
            columnCount);
        int rowSpan = Math.Max(1, icon.FolderRowSpan_NoWrite);
        ShortcutTileDisplaySize tileSize = ShortcutLayoutCalculator.CalculatePanelTileDisplaySize(
            metrics,
            columnSpan,
            rowSpan,
            supportsVariableSizedItems: true);

        templateRoot.Width = tileSize.Width;
        templateRoot.Height = tileSize.Height;
        if (MenuGeometryHelper.FindDescendantByName<Grid>(
                templateRoot,
                "FolderVisualRoot") is not Grid folderVisual)
        {
            return;
        }

        double topInset = GetShortcutIconStackTopInset(metrics.SlotHeight);
        folderVisual.Width = GetBodyWidth(tileSize.Width, columnSpan);
        folderVisual.Height = GetBodyHeight(metrics.SlotHeight, rowSpan, topInset);
        folderVisual.Margin = new Thickness(0d, topInset, 0d, 0d);
        folderVisual.HorizontalAlignment = HorizontalAlignment.Center;
        folderVisual.VerticalAlignment = VerticalAlignment.Center;
        ApplySurface(folderVisual, icon.FolderSize);
        ApplyPreview(folderVisual, icon);
    }

    public static double GetPreviewIconSize(ShortcutFolderSize size) => size switch
    {
        ShortcutFolderSize.Large => 44d,
        ShortcutFolderSize.Medium => 22d,
        _ => 18d
    };

    public static double GetBodyHeight(double slotHeight, int rowSpan) =>
        GetBodyHeight(slotHeight, rowSpan, topInset: 0d);

    private static double GetBodyHeight(double slotHeight, int rowSpan, double topInset)
    {
        double bodyHeight = ShortcutLayoutCalculator.GetFolderBodyHeight(
            slotHeight,
            rowSpan,
            LabelBlockHeight,
            StackSpacing);
        return rowSpan > 1 ? Math.Max(64d, bodyHeight - topInset) : bodyHeight;
    }

    private static double GetShortcutIconStackTopInset(double slotHeight)
    {
        double regularStackHeight = ShortcutIconSize + StackSpacing + LabelBlockHeight;
        return Math.Max(0d, (slotHeight - regularStackHeight) / 2d);
    }

    private static double GetBodyWidth(double tileWidth, int columnSpan) =>
        columnSpan >= 2 ? Math.Max(64d, tileWidth - 24d) : 64d;

    private static void ApplySurface(DependencyObject folderVisual, ShortcutFolderSize size)
    {
        if (MenuGeometryHelper.FindDescendantByName<Border>(
                folderVisual,
                "FolderVisualSurface") is Border surface)
        {
            surface.CornerRadius = new CornerRadius(
                size == ShortcutFolderSize.Small ? 18d : 28d);
        }
    }

    private static void ApplyPreview(DependencyObject folderVisual, IconInfo icon)
    {
        if (MenuGeometryHelper.FindDescendantByName<Grid>(
                folderVisual,
                "FolderPreviewItems") is not Grid previewItems)
        {
            return;
        }

        double iconSize = GetPreviewIconSize(icon.FolderSize);
        int itemCount = Math.Clamp(icon.FolderPreviewItems_NoWrite.Count, 1, 6);
        int columnCount = itemCount <= 2 ? itemCount : 2;
        int rowCount = (int)Math.Ceiling(itemCount / (double)columnCount);
        double itemSize = iconSize + PreviewIconGap;

        previewItems.Margin = new Thickness(PreviewSafePadding);
        previewItems.Width = itemSize * columnCount;
        previewItems.Height = itemSize * rowCount;
        previewItems.HorizontalAlignment = HorizontalAlignment.Center;
        previewItems.VerticalAlignment = VerticalAlignment.Center;
        previewItems.ColumnSpacing = columnCount > 1 ? PreviewIconGap : 0d;
        previewItems.RowSpacing = rowCount > 1 ? PreviewIconGap : 0d;
        SetGridTracks(previewItems, columnCount, rowCount);

        ForEachDescendant<Image>(previewItems, image =>
        {
            image.Width = iconSize;
            image.Height = iconSize;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;
        });
    }

    private static void SetGridTracks(Grid grid, int columnCount, int rowCount)
    {
        for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
        {
            grid.ColumnDefinitions[i].Width = i < columnCount
                ? new GridLength(1d, GridUnitType.Star)
                : new GridLength(0d);
        }
        for (int i = 0; i < grid.RowDefinitions.Count; i++)
        {
            grid.RowDefinitions[i].Height = i < rowCount
                ? new GridLength(1d, GridUnitType.Star)
                : new GridLength(0d);
        }
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
