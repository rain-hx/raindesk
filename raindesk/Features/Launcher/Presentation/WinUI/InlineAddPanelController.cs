using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Controls;
using raindesk.Control.Other;
using raindesk.Control.Windows;
using raindesk.Features.Launcher.Presentation;
using raindesk.Services;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// View references needed by InlineAddPanelController methods.
/// The page populates these before each call because the XAML elements
/// use x:Load="False" and may not be materialized yet.
/// </summary>
internal sealed class InlineAddPanelViews
{
    public TextBlock? SectionTitle { get; init; }
    public RowDefinition? TitleRow { get; init; }
    public FrameworkElement? TitleBar { get; init; }
    public FrameworkElement? BackInlineButton { get; init; }
    public FrameworkElement? GroupBarHost { get; init; }
    public Grid? WrapUFG { get; init; }
    public FrameworkElement? InlineUrlPanel { get; init; }
    public ContentControl? InlineUrlHost { get; init; }
    public FrameworkElement? InlineSystemPanel { get; init; }
    public TabView? InlineSystemTabs { get; init; }
    public TextBox? InlineSystemSearchBox { get; init; }
    public FrameworkElement? InlineSystemSearchPlaceholder { get; init; }
    public FrameworkElement? InlineSystemLoading { get; init; }
    public FrameworkElement? InlineSystemItems { get; init; }
}

/// <summary>
/// Manages the inline add panel lifecycle: URL panel, system items panel,
/// and the shortcut panel restoration. Extracted from RightCardControl.
/// </summary>
internal sealed class InlineAddPanelController
{
    private readonly LauncherInlineAddPanelState _panelState;
    private readonly LauncherInlineSystemItemsCoordinator<IconInfo> _systemItems;
    private readonly SystemItemWindow.SystemItemViewModel _systemVm;
    private readonly Action _ensureWritableGroupSelected;
    private readonly Action _showShortcutPanel;

    public InlineAddPanelController(
        LauncherInlineAddPanelState panelState,
        LauncherInlineSystemItemsCoordinator<IconInfo> systemItems,
        SystemItemWindow.SystemItemViewModel systemVm,
        Action ensureWritableGroupSelected,
        Action showShortcutPanel)
    {
        _panelState = panelState;
        _systemItems = systemItems;
        _systemVm = systemVm;
        _ensureWritableGroupSelected = ensureWritableGroupSelected;
        _showShortcutPanel = showShortcutPanel;
    }

    public LauncherInlineSystemItemsCoordinator<IconInfo> SystemItems => _systemItems;

    // ── Panel switching ───────────────────────────────────────────────

    public void ShowAddUrlPanel(InlineAddPanelViews views)
    {
        if (!_panelState.TryOpen(LauncherInlinePanelMode.Url)) return;
        try
        {
            _ensureWritableGroupSelected();
            ApplyPanelVisibility(views, PanelMode.Url);

            var urlDialog = new IconInfoUrlDialog();
            urlDialog.Saved += (_, _) => _showShortcutPanel();
            urlDialog.Canceled += (_, _) => _showShortcutPanel();
            if (views.InlineUrlHost != null) views.InlineUrlHost.Content = urlDialog;
        }
        catch (Exception ex)
        {
            _panelState.Close();
            LogUtil.WriteErrorLog(ex, "打开URL面板失败");
            GrowlService.Warning("无法打开 URL 添加面板，请重试。");
            _showShortcutPanel();
        }
    }

    public void ShowAddSystemPanel(InlineAddPanelViews views)
    {
        if (!_panelState.TryOpen(LauncherInlinePanelMode.SystemItems)) return;
        _ensureWritableGroupSelected();
        EnsureDataContext(views);
        ApplyPanelVisibility(views, PanelMode.System);

        if (views.InlineSystemTabs != null && views.InlineSystemTabs.SelectedIndex < 0)
            views.InlineSystemTabs.SelectedIndex = 0;
        LoadInlineSystemItems(views);
    }

    public void ShowShortcutPanel(InlineAddPanelViews views)
    {
        _panelState.Close();
        _systemItems.CancelPending();
        ApplyPanelVisibility(views, PanelMode.Shortcut);
    }

    // ── XAML event handlers ───────────────────────────────────────────

    public void HandleBackInlinePanel(object sender, RoutedEventArgs e, InlineAddPanelViews views)
        => ShowShortcutPanel(views);

    public void HandleInlineSystemTabsSelectionChanged(object sender, SelectionChangedEventArgs e, InlineAddPanelViews views)
    {
        if (views.InlineSystemPanel == null || views.InlineSystemPanel.Visibility != Visibility.Visible) return;
        LoadInlineSystemItems(views);
    }

    public void HandleInlineSystemSearchBoxTextChanged(object sender, TextChangedEventArgs e, InlineAddPanelViews views)
    {
        UpdateSearchPlaceholderVisibility(views);

        if ((views.InlineSystemTabs?.SelectedItem as TabViewItem)?.Tag?.ToString() == "StartMenu")
        {
            _ = _systemItems.FilterAsync(views.InlineSystemSearchBox?.Text);
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────

    private enum PanelMode { Url, System, Shortcut }

    private void ApplyPanelVisibility(InlineAddPanelViews views, PanelMode mode)
    {
        switch (mode)
        {
            case PanelMode.Url:
                if (views.SectionTitle != null) views.SectionTitle.Text = "URL 快捷方式";
                if (views.TitleRow != null) views.TitleRow.Height = GridLength.Auto;
                SetVisible(views.TitleBar, true);
                SetVisible(views.BackInlineButton, true);
                SetVisible(views.GroupBarHost, false);
                if (views.WrapUFG != null) views.WrapUFG.Visibility = Visibility.Collapsed;
                SetVisible(views.InlineSystemPanel, false);
                SetVisible(views.InlineUrlPanel, true);
                if (views.InlineUrlHost != null) views.InlineUrlHost.Content = null;
                break;

            case PanelMode.System:
                if (views.SectionTitle != null) views.SectionTitle.Text = "系统项目";
                if (views.TitleRow != null) views.TitleRow.Height = GridLength.Auto;
                SetVisible(views.TitleBar, true);
                SetVisible(views.BackInlineButton, true);
                SetVisible(views.GroupBarHost, false);
                if (views.WrapUFG != null) views.WrapUFG.Visibility = Visibility.Collapsed;
                SetVisible(views.InlineUrlPanel, false);
                if (views.InlineUrlHost != null) views.InlineUrlHost.Content = null;
                SetVisible(views.InlineSystemPanel, true);
                break;

            case PanelMode.Shortcut:
                if (views.SectionTitle != null) views.SectionTitle.Text = "我的快捷";
                if (views.TitleRow != null) views.TitleRow.Height = new GridLength(0);
                SetVisible(views.TitleBar, false);
                SetVisible(views.BackInlineButton, false);
                SetVisible(views.GroupBarHost, true);
                if (views.WrapUFG != null) views.WrapUFG.Visibility = Visibility.Visible;
                SetVisible(views.InlineUrlPanel, false);
                SetVisible(views.InlineSystemPanel, false);
                if (views.InlineUrlHost != null) views.InlineUrlHost.Content = null;
                break;
        }
    }

    private void EnsureDataContext(InlineAddPanelViews views)
    {
        if (views.InlineSystemItems != null && views.InlineSystemItems.DataContext != _systemVm)
        {
            views.InlineSystemItems.DataContext = _systemVm;
        }
    }

    private void LoadInlineSystemItems(InlineAddPanelViews views)
    {
        LauncherInlineSystemItemsKind kind =
            (views.InlineSystemTabs?.SelectedItem as TabViewItem)?.Tag?.ToString() == "StartMenu"
                ? LauncherInlineSystemItemsKind.StartMenu
                : LauncherInlineSystemItemsKind.BuiltIn;
        if (kind == LauncherInlineSystemItemsKind.BuiltIn && views.InlineSystemSearchBox != null)
        {
            views.InlineSystemSearchBox.Text = "";
        }
        _ = _systemItems.SelectAsync(kind, views.InlineSystemSearchBox?.Text);
    }

    private void UpdateSearchPlaceholderVisibility(InlineAddPanelViews views)
    {
        if (views.InlineSystemSearchPlaceholder == null || views.InlineSystemSearchBox == null) return;

        views.InlineSystemSearchPlaceholder.Visibility = string.IsNullOrEmpty(views.InlineSystemSearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void ApplySystemItemsUpdate(LauncherInlineSystemItemsUpdate<IconInfo> update, FrameworkElement? inlineSystemLoading)
    {
        if (inlineSystemLoading != null)
        {
            inlineSystemLoading.Visibility = update.IsLoading
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        _systemVm.IconInfos = new ObservableCollection<IconInfo>(update.Items);
        if (!string.IsNullOrWhiteSpace(update.Error))
        {
            LogUtil.WriteErrorLog(
                new InvalidOperationException(update.Error),
                "加载内嵌 Launcher 候选项失败");
        }
    }

    private static void SetVisible(FrameworkElement? element, bool visible)
    {
        if (element != null)
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
