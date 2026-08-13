using rainspace.Constant;
using rainspace.Controls;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Features.Workspace.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading;

namespace rainspace.Control.Other
{
    /// <summary>
    /// WinUI 3 PasswordDialog - inline StackPanel used in RightCardControl.
    /// Implements 4-digit PIN entry with create/verify/alter flows.
    /// </summary>
    public class PasswordDialog : StackPanel
    {
        // UI elements (created programmatically, exposed as public fields for code access)
        public TextBlock Title = null!;
        public TextBlock ErrorMsg = null!;
        public TextBlock HintMsg = null!;
        public PasswordBox P1 = null!, P2 = null!, P3 = null!, P4 = null!;
        public TextBox HintBox = null!;
        public Grid PasswordGrid = null!;
        public Grid HintGrid = null!;

        // State
        public PasswordType type;
        public MenuInfo? menuInfo;
        public int count = 0;
        public int _pendingEncryptedGroupIndex = -1;
        private string? tempPassword = null;
        private PasswordType tempType;
        private readonly IAppDataProvider _dataProvider;
        private readonly ISaveService _saveService;
        private readonly IWorkspaceCommandHandler _workspaceCommandHandler;
        private AppData appData => _dataProvider.Current;

        public PasswordDialog()
        {
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _saveService = AppServices.Get<ISaveService>();
            _workspaceCommandHandler = AppServices.Get<IWorkspaceCommandHandler>();
            BuildUI();
        }

        private void BuildUI()
        {
            // Root styling
            this.Width = 300;
            this.Height = 150;
            this.CornerRadius = new CornerRadius(6);
            this.Background = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.7 };
            this.VerticalAlignment = VerticalAlignment.Center;

            // Title
            Title = new TextBlock
            {
                Text = "请输入密码",
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // PasswordGrid
            PasswordGrid = new Grid { Height = 65, Visibility = Visibility.Visible };

            // HintMsg
            HintMsg = new TextBlock
            {
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 250,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0),
                Text = "提示:",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            // Password boxes row
            var pwRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            P1 = CreatePasswordBox("P1");
            P2 = CreatePasswordBox("P2");
            P3 = CreatePasswordBox("P3");
            P4 = CreatePasswordBox("P4");

            P1.Margin = new Thickness(0, 0, 5, 0);
            P2.Margin = new Thickness(0, 0, 5, 0);
            P3.Margin = new Thickness(0, 0, 5, 0);

            pwRow.Children.Add(P1);
            pwRow.Children.Add(P2);
            pwRow.Children.Add(P3);
            pwRow.Children.Add(P4);

            // ErrorMsg
            ErrorMsg = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 65, 37, -65),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                Text = "密码输入错误",
                Visibility = Visibility.Visible
            };

            PasswordGrid.Children.Add(HintMsg);
            PasswordGrid.Children.Add(pwRow);
            PasswordGrid.Children.Add(ErrorMsg);

            // HintGrid
            HintGrid = new Grid
            {
                Height = 65,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var hintRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            HintBox = new TextBox
            {
                TextAlignment = TextAlignment.Left,
                Width = 220,
                FontSize = 14
            };
            // Try to apply style from resources
            try
            {
                var style = Application.Current.Resources["MyTextBoxStyle"] as Style;
                if (style != null) HintBox.Style = style;
            }
            catch { }

            hintRow.Children.Add(HintBox);

            var hintButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(202, 35, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var skipText = new TextBlock
            {
                Text = "跳过",
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 64, 140, 203)),
                Margin = new Thickness(0, 0, 5, 0)
            };
            skipText.PointerPressed += NextTB_MouseLeftButtonDown;

            var doneText = new TextBlock
            {
                Text = "完成",
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 64, 140, 203))
            };
            doneText.PointerPressed += DoneTB_MouseLeftButtonDown;

            hintButtons.Children.Add(skipText);
            hintButtons.Children.Add(doneText);

            HintGrid.Children.Add(hintRow);
            HintGrid.Children.Add(hintButtons);

            // Assemble
            var mainPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            mainPanel.Children.Add(Title);
            mainPanel.Children.Add(PasswordGrid);
            mainPanel.Children.Add(HintGrid);

            this.Children.Add(mainPanel);
        }

        private PasswordBox CreatePasswordBox(string tag)
        {
            var pb = new PasswordBox
            {
                Tag = tag,
                Height = 40,
                Width = 40,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 18,
                MaxLength = 1,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0)
            };

            // Apply PasswordBoxBaseStyle if available
            try
            {
                var style = Application.Current.Resources["PasswordBoxBaseStyle"] as Style;
                if (style != null)
                {
                    // Keep our custom dimensions
                    pb.Height = 40;
                    pb.Width = 40;
                    pb.MaxLength = 1;
                }
            }
            catch { }

            pb.PasswordChanged += PasswordBox_PasswordChanged;
            pb.KeyDown += PasswordBox_KeyDown;
            return pb;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            if (!string.IsNullOrEmpty(pb.Password))
            {
                char c = pb.Password.ToCharArray()[0];
                if (c > '9' || c < '0')
                {
                    pb.Password = "";
                    return;
                }
            }

            string tag = pb.Tag?.ToString() ?? "";
            switch (tag)
            {
                case "P1":
                    if (!string.IsNullOrEmpty(pb.Password)) P2.Focus(FocusState.Programmatic);
                    break;
                case "P2":
                    if (!string.IsNullOrEmpty(pb.Password)) P3.Focus(FocusState.Programmatic);
                    break;
                case "P3":
                    if (!string.IsNullOrEmpty(pb.Password)) P4.Focus(FocusState.Programmatic);
                    break;
                case "P4":
                    if (string.IsNullOrEmpty(pb.Password)) P3.Focus(FocusState.Programmatic);
                    break;
            }

            if (!string.IsNullOrEmpty(P1.Password)
                    && !string.IsNullOrEmpty(P2.Password)
                    && !string.IsNullOrEmpty(P3.Password)
                    && !string.IsNullOrEmpty(P4.Password))
            {
                string pw = P1.Password + P2.Password + P3.Password + P4.Password;

                if (type == PasswordType.INPUT || type == PasswordType.CANCEL)
                {
                    if (PasswordUtil.VerifyAndMigrate(pw, appData.AppConfig.MenuPassword, out var newHash))
                    {
                        // 如果密码已迁移到 PBKDF2 格式，更新存储的哈希
                        if (newHash != null)
                        {
                            appData.AppConfig.MenuPassword = newHash;
                        }

                        var rightCard = MainWindow.mainWindow?.RightCard;
                        var passwordDialog = rightCard?.EnsurePasswordDialog();
                        if (passwordDialog != null)
                        {
                            passwordDialog.Visibility = Visibility.Collapsed;
                        }
                        int selectedIndex = _pendingEncryptedGroupIndex >= 0
                            ? _pendingEncryptedGroupIndex
                            : appData.AppConfig.SelectedMenuIndex;
                        if (selectedIndex >= 0 && selectedIndex < appData.MenuList.Count)
                        {
                            _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(selectedIndex));
                        }
                        var shortcutPanel = rightCard?.EnsureWrapUFG();
                        if (shortcutPanel != null)
                        {
                            shortcutPanel.Visibility = Visibility.Visible;
                        }
                        if (type == PasswordType.CANCEL && menuInfo != null)
                        {
                            _workspaceCommandHandler.SetEncryption(new SetGroupEncryptionCommand(menuInfo.MenuId, false));
                        }
                    }
                    else
                    {
                        ErrorMsg.Text = "密码输入错误";
                        ErrorMsg.Visibility = Visibility.Visible;
                        if (!string.IsNullOrEmpty(appData.AppConfig.PasswordHint))
                        {
                            HintMsg.Text = "提示: " + appData.AppConfig.PasswordHint;
                            HintMsg.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (type == PasswordType.CREATE)
                {
                    if (count == 0)
                    {
                        count++;
                        tempPassword = pw; // 保存原始密码用于确认比较
                        Title.Text = "再次输入密码";
                        ClearVal();
                        SetFocus(0);
                    }
                    else
                    {
                        if (tempPassword != null && tempPassword.Equals(pw, StringComparison.Ordinal))
                        {
                            Title.Text = "填写密码提示";
                            PasswordGrid.Visibility = Visibility.Collapsed;
                            HintGrid.Visibility = Visibility.Visible;
                            HintBox.Focus(FocusState.Programmatic);
                        }
                        else
                        {
                            ErrorMsg.Text = "两次密码输入不一致";
                            ErrorMsg.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (type == PasswordType.ALTER)
                {
                    if (PasswordUtil.VerifyAndMigrate(pw, appData.AppConfig.MenuPassword, out var newHash))
                    {
                        // 如果密码已迁移到 PBKDF2 格式，更新存储的哈希
                        if (newHash != null)
                        {
                            appData.AppConfig.MenuPassword = newHash;
                        }

                        tempType = type;
                        type = PasswordType.CREATE;
                        Title.Text = "设置新密码";
                        ClearVal();
                        SetFocus(0);
                    }
                    else
                    {
                        ErrorMsg.Text = "密码输入错误";
                        ErrorMsg.Visibility = Visibility.Visible;
                        HintMsg.Text = appData.AppConfig.PasswordHint;
                        HintMsg.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                if (ErrorMsg.Visibility == Visibility.Visible)
                {
                    ErrorMsg.Visibility = Visibility.Collapsed;
                    HintMsg.Visibility = Visibility.Collapsed;
                }
            }
        }

        public async System.Threading.Tasks.Task SetFocusAsync(int time = 100)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(time);
                if (string.IsNullOrEmpty(P1.Password))
                {
                    P1.Focus(FocusState.Programmatic);
                    return;
                }
                if (string.IsNullOrEmpty(P2.Password))
                {
                    P2.Focus(FocusState.Programmatic);
                    return;
                }
                if (string.IsNullOrEmpty(P3.Password))
                {
                    P3.Focus(FocusState.Programmatic);
                    return;
                }
                P4.Focus(FocusState.Programmatic);
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "SetFocus 失败"); }
        }

        /// <summary>
        /// 设置焦点（便捷方法，不等待完成）
        /// </summary>
        public void SetFocus(int time = 100)
        {
            _ = SetFocusAsync(time);
        }

        public void ClearVal()
        {
            P1.Password = "";
            P2.Password = "";
            P3.Password = "";
            P4.Password = "";
        }

        private void NextTB_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            appData.AppConfig.PasswordHint = "";
            DonePassword();
        }

        private void DoneTB_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            string hint = HintBox.Text.Trim();
            appData.AppConfig.PasswordHint = hint;
            DonePassword();
        }

        private void DonePassword()
        {
            appData.AppConfig.MenuPassword = PasswordUtil.HashPassword(tempPassword);
            _saveService.SaveDebounced();
            var passwordDialog = MainWindow.mainWindow?.RightCard?.EnsurePasswordDialog();
            if (passwordDialog != null)
            {
                passwordDialog.Visibility = Visibility.Collapsed;
            }
            PasswordGrid.Visibility = Visibility.Visible;
            HintGrid.Visibility = Visibility.Collapsed;
            if (tempType == PasswordType.ALTER)
            {
                GrowlService.Success("密码修改成功!", "MainWindowGrowl");
            }
            else if (menuInfo != null)
            {
                _workspaceCommandHandler.SetEncryption(new SetGroupEncryptionCommand(menuInfo.MenuId, true));
                GrowlService.Success(menuInfo.MenuName + " 已加密!", "MainWindowGrowl");
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Back)
            {
                if (P2.FocusState != FocusState.Unfocused)
                {
                    if (string.IsNullOrEmpty(P2.Password))
                    {
                        P1.Password = "";
                    }
                    else
                    {
                        P2.Password = "";
                    }
                }

                if (P3.FocusState != FocusState.Unfocused)
                {
                    if (string.IsNullOrEmpty(P3.Password))
                    {
                        P2.Password = "";
                    }
                    else
                    {
                        P3.Password = "";
                    }
                }

                if (P4.FocusState != FocusState.Unfocused)
                {
                    if (string.IsNullOrEmpty(P4.Password))
                    {
                        P3.Password = "";
                    }
                    else
                    {
                        P4.Password = "";
                    }
                }
            }
            SetFocus(0);
        }
    }
}
