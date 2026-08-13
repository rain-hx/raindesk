using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.Launcher.Contracts;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>将纯 Launcher 菜单计划投影到 WinUI 菜单项，并统一维护菜单目标。</summary>
public sealed class LauncherContextMenuViewController<TItem> where TItem : class
{
    private readonly IReadOnlyDictionary<LauncherContextMenuAction, MenuFlyoutItemBase> _items;
    private readonly MenuFlyoutSeparator _primarySeparator;
    private readonly MenuFlyoutSeparator _editSeparator;

    public LauncherContextMenuViewController(
        IReadOnlyDictionary<LauncherContextMenuAction, MenuFlyoutItemBase> items,
        MenuFlyoutSeparator primarySeparator,
        MenuFlyoutSeparator editSeparator)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _primarySeparator = primarySeparator ?? throw new ArgumentNullException(nameof(primarySeparator));
        _editSeparator = editSeparator ?? throw new ArgumentNullException(nameof(editSeparator));
    }

    public void Apply(LauncherContextMenuPlan? plan, TItem? target)
    {
        foreach ((LauncherContextMenuAction action, MenuFlyoutItemBase item) in _items)
        {
            item.Visibility = ToVisibility(plan?.IsVisible(action) == true);
            AssignTarget(item, target);
        }

        _primarySeparator.Visibility = ToVisibility(plan?.IsAvailable == true);
        _editSeparator.Visibility = ToVisibility(
            plan?.IsVisible(LauncherContextMenuAction.RunAsAdministrator) == true
            || plan?.IsVisible(LauncherContextMenuAction.ShowInFileExplorer) == true
            || plan?.IsVisible(LauncherContextMenuAction.EditProperties) == true);
    }

    private static void AssignTarget(MenuFlyoutItemBase item, TItem? target)
    {
        if (item is MenuFlyoutItem menuItem)
        {
            menuItem.Tag = target;
            return;
        }

        if (item is MenuFlyoutSubItem subItem)
        {
            foreach (MenuFlyoutItemBase child in subItem.Items)
            {
                AssignTarget(child, target);
            }
        }
    }

    private static Visibility ToVisibility(bool visible) =>
        visible ? Visibility.Visible : Visibility.Collapsed;
}
