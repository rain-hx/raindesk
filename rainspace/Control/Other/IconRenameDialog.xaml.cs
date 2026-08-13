using System;
using rainspace.Controls;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Services;
using rainspace.Features.Launcher.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace rainspace.Control.Other
{
    /// <summary>
    /// Lightweight rename dialog for a shortcut tile.
    /// </summary>
    public partial class IconRenameDialog : UserControl, IDialogPostEntrance
    {
        public Microsoft.UI.Xaml.Window? dialog;
        private readonly string _shortcutId;
        private readonly string _originalName;
        private readonly ILauncherCommandHandler _commandHandler = AppServices.Get<ILauncherCommandHandler>();
        private bool _isClosing;
        private bool _inputHovered;

        public IconRenameDialog(IconInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);
            _shortcutId = info.IconId;
            _originalName = info.Name ?? string.Empty;
            InitializeComponent();
            Loaded += IconRenameDialog_Loaded;
        }

        private void IconRenameDialog_Loaded(object sender, RoutedEventArgs e)
        {
            IconName.Text = _originalName;
            IconName.KeyDown += IconName_KeyDown;
            InputHost.PointerEntered += InputHost_PointerEntered;
            InputHost.PointerExited += InputHost_PointerExited;
            BlinkingCaretOverlay.Attach(
                IconName,
                IconNameMirror,
                IconNameCaretBlinker,
                DispatcherQueue,
                fontWeight: Microsoft.UI.Text.FontWeights.Normal);
            UpdateInputChrome();
            UpdateNamePlaceholderVisibility();
        }

        public void OnEntranceAnimationCompleted()
        {
            int textLength = IconName.Text?.Length ?? 0;
            IconName.SelectionStart = textLength;
            IconName.SelectionLength = 0;
            BlinkingCaretOverlay.PrepareEditAppearance(IconName);
            IconName.Focus(FocusState.Programmatic);
            UpdateInputChrome();
            UpdateNamePlaceholderVisibility();
        }

        private void IconName_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateInputChrome();
            UpdateNamePlaceholderVisibility();
        }

        private void IconName_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateInputChrome();
            UpdateNamePlaceholderVisibility();
        }

        private void IconName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateNamePlaceholderVisibility();
        }

        private void UpdateNamePlaceholderVisibility()
        {
            NamePlaceholder.Visibility = string.IsNullOrEmpty(IconName.Text)
                && IconName.FocusState == FocusState.Unfocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void InputHost_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _inputHovered = true;
            UpdateInputChrome();
        }

        private void InputHost_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _inputHovered = false;
            UpdateInputChrome();
        }

        private void UpdateInputChrome()
        {
            var resources = Application.Current.Resources;
            var normalBorder = resources["ControlBorderBrush"] as Brush;
            var hoverBorder = resources["AccentLightBrush"] as Brush;
            var focusBorder = resources["rainspaceBlueBrush"] as Brush;
            var background = resources["ControlFillBrush"] as Brush;

            InputHost.Background = background;
            InputHost.BorderThickness = new Thickness(1);

            if (IconName.FocusState != FocusState.Unfocused)
            {
                InputHost.BorderBrush = focusBorder ?? normalBorder;
                return;
            }

            InputHost.BorderBrush = _inputHovered
                ? hoverBorder ?? normalBorder
                : normalBorder;
        }

        private void IconName_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Enter)
            {
                Confirm_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == global::Windows.System.VirtualKey.Escape)
            {
                Cancel_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string name = IconName.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(name)
                && !string.Equals(_originalName, name, StringComparison.Ordinal))
            {
                LauncherCommandResult result = _commandHandler.Rename(
                    new RenameLauncherShortcutCommand(_shortcutId, name));
                if (!result.Success)
                {
                    GrowlService.Warning(result.Error ?? "快捷方式重命名失败。");
                    return;
                }
            }

            CloseDialog();
        }

        private void CloseDialog()
        {
            if (_isClosing) return;
            _isClosing = true;

            BlinkingCaretOverlay.Detach(IconName);
            DialogAnimationHelper.PlayExit(DialogAnimationRoot, () => dialog?.Close());
        }
    }
}
