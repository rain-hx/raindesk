using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.ViewModel;

namespace raindesk.Services;

/// <summary>
/// 面板导航服务，管理 Home/Todo/Settings 三个面板的切换。
/// 从 MainWindow 提取的导航逻辑。
/// </summary>
public sealed class NavigationService
{
    /// <summary>
    /// 当前活动的面板。
    /// </summary>
    public PanelType ActivePanel { get; private set; } = PanelType.Home;

    /// <summary>
    /// 面板切换时触发。
    /// </summary>
    public event Action<PanelType>? PanelChanged;

    public NavigationService()
    {
    }

    /// <summary>
    /// 切换到主面板（快捷方式列表）。
    /// </summary>
    public void ShowMainPanel(
        FrameworkElement? inlineConfigPanel,
        FrameworkElement? inlineTodoPanel,
        FrameworkElement? leftCard,
        FrameworkElement? rightCard,
        GridLength leftColumnWidth)
    {
        if (inlineConfigPanel != null) inlineConfigPanel.Visibility = Visibility.Collapsed;
        if (inlineTodoPanel != null) inlineTodoPanel.Visibility = Visibility.Collapsed;

        if (leftCard != null) leftCard.Visibility = Visibility.Collapsed;
        if (rightCard != null) rightCard.Visibility = Visibility.Visible;

        ActivePanel = PanelType.Home;
        PanelChanged?.Invoke(ActivePanel);
    }

    /// <summary>
    /// 切换到设置面板。
    /// </summary>
    public void ShowSettingsPanel(
        AppConfig appConfig,
        FrameworkElement configPanel,
        FrameworkElement inlineConfigPanel,
        FrameworkElement? inlineTodoPanel,
        FrameworkElement? leftCard,
        FrameworkElement? rightCard)
    {
        if (configPanel is FrameworkElement fe)
        {
            // DataContext 由调用者设置
        }

        if (leftCard != null) leftCard.Visibility = Visibility.Collapsed;
        if (rightCard != null) rightCard.Visibility = Visibility.Collapsed;
        if (inlineTodoPanel != null) inlineTodoPanel.Visibility = Visibility.Collapsed;
        inlineConfigPanel.Visibility = Visibility.Visible;

        ActivePanel = PanelType.Settings;
        PanelChanged?.Invoke(ActivePanel);
    }

    /// <summary>
    /// 切换到待办面板。
    /// </summary>
    public void ShowTodoPanel(
        object todoPanel,
        FrameworkElement? inlineConfigPanel,
        FrameworkElement? leftCard,
        FrameworkElement? rightCard,
        FrameworkElement inlineTodoPanel)
    {
        // TodoPanel.Refresh() 由调用者处理

        if (leftCard != null) leftCard.Visibility = Visibility.Collapsed;
        if (rightCard != null) rightCard.Visibility = Visibility.Collapsed;
        if (inlineConfigPanel != null) inlineConfigPanel.Visibility = Visibility.Collapsed;
        inlineTodoPanel.Visibility = Visibility.Visible;

        ActivePanel = PanelType.Todo;
        PanelChanged?.Invoke(ActivePanel);
    }

    /// <summary>
    /// 更新导航栏按钮的选中状态。
    /// </summary>
    public void UpdateRailSelection(
        Button? homeButton,
        Button? todoButton,
        Button? settingsButton,
        AppThemeMode mode)
    {
        if (homeButton == null || todoButton == null || settingsButton == null) return;

        var selectedBrush = new SolidColorBrush(ThemeService.GetRailSelectedBackgroundColor(mode));
        var transparentBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        var selectedForeground = new SolidColorBrush(ThemeService.GetRailSelectedForegroundColor(mode));
        var normalForeground = new SolidColorBrush(ThemeService.GetRailNormalForegroundColor(mode));

        ApplyButtonState(homeButton, ActivePanel == PanelType.Home, selectedBrush, transparentBrush, selectedForeground, normalForeground);
        ApplyButtonState(todoButton, ActivePanel == PanelType.Todo, selectedBrush, transparentBrush, selectedForeground, normalForeground);
        ApplyButtonState(settingsButton, ActivePanel == PanelType.Settings, selectedBrush, transparentBrush, selectedForeground, normalForeground);
    }

    private static void ApplyButtonState(Button button, bool isSelected, Brush selectedBrush, Brush transparentBrush, Brush selectedForeground, Brush normalForeground)
    {
        button.Background = isSelected ? selectedBrush : transparentBrush;
        button.Foreground = isSelected ? selectedForeground : normalForeground;
    }
}

/// <summary>
/// 面板类型枚举。
/// </summary>
public enum PanelType
{
    Home,
    Todo,
    Settings
}
