using raindesk.Constant;
using raindesk.Controls;
using raindesk.Control.Other;
using raindesk.Control.Windows;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Features.Appearance.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace raindesk.Control.UserControls.Config
{
    /// <summary>
    /// ThemeControl.xaml 的交互逻辑
    /// </summary>
    public partial class ThemeControl : UserControl
    {
        private readonly AppConfig appConfig;
        private readonly ISaveService saveService;
        private readonly IAppearanceCommandHandler appearanceCommands;
        private bool _isLoaded;

        private static readonly HashSet<string> ThemeAutoSaveProperties = new(StringComparer.Ordinal)
        {
            nameof(AppConfig.BGStyle),
            nameof(AppConfig.ImageByteArr),
            nameof(AppConfig.BacImgName),
            nameof(AppConfig.BgOpacity),
            nameof(AppConfig.BlurEffect),
            nameof(AppConfig.BlurValue),
            nameof(AppConfig.HideSearchBox),
            nameof(AppConfig.AppThemeMode),
            nameof(AppConfig.GradientBGParam),
            nameof(AppConfig.CustomBGParams),
        };

        public ThemeControl()
        {
            appConfig = AppServices.Get<IAppDataProvider>().Current.AppConfig;
            saveService = AppServices.Get<ISaveService>();
            appearanceCommands = AppServices.Get<IAppearanceCommandHandler>();
            InitializeComponent();
            DataContext = appConfig;
            if (appConfig != null)
            {
                SyncThemeModeRadios(ThemeService.NormalizeMode(appConfig.AppThemeMode));
            }

            ApplyBackgroundConfigVisibility();
            Loaded += ThemeControl_Loaded;
            Unloaded += ThemeControl_Unloaded;
        }

        private void ThemeControl_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            if (appConfig != null)
            {
                appConfig.PropertyChanged += AppConfig_PropertyChanged;
            }
        }

        private void ThemeControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            if (appConfig != null)
            {
                appConfig.PropertyChanged -= AppConfig_PropertyChanged;
            }
        }

        private void AppConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isLoaded || e.PropertyName == null) return;
            if (!ThemeAutoSaveProperties.Contains(e.PropertyName)) return;

            AutoSaveThemeSettings();
        }

        private void AutoSaveThemeSettings()
        {
            saveService.SaveDebounced();
        }

        private void ApplyBackgroundConfigVisibility()
        {
            if (appConfig == null) return;

            if (appConfig.BGStyle != BGStyle.GradientBac)
            {
                GradientBGConf.Visibility = Visibility.Collapsed;
                ImgBGConf.Visibility = Visibility.Visible;
            }
            else
            {
                ImgBGConf.Visibility = Visibility.Collapsed;
                GradientBGConf.Visibility = Visibility.Visible;
            }
        }

        private void SyncThemeModeRadios(AppThemeMode mode)
        {
            SystemThemeRadio.IsChecked = mode == AppThemeMode.System;
            LightThemeRadio.IsChecked = mode == AppThemeMode.Light;
            DarkThemeRadio.IsChecked = mode == AppThemeMode.Dark;
        }

        private async void BGButton_Click(object sender, RoutedEventArgs e)
        {
            if (appConfig == null) return;

            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WindowNative.GetWindowHandle(MainWindow.mainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".gif");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    appearanceCommands.SetBackgroundImage(new SetBackgroundImageCommand(
                        File.ReadAllBytes(file.Path),
                        file.Path));
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "修改背景失败,已重置为默认背景!");
                GrowlService.WarningGlobal("修改背景失败,已重置为默认背景!");
            }

            BGSettingUtil.BGSetting();
            AutoSaveThemeSettings();
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (appConfig == null) return;

            try
            {
                appearanceCommands.SetBackgroundImage(new SetBackgroundImageCommand(
                    Convert.FromBase64String(Constants.DEFAULT_BAC_IMAGE_BASE64),
                    "系统默认"));
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "修改背景失败2,已重置为默认背景!");
                GrowlService.WarningGlobal("修改背景失败,已重置为默认背景!");
            }

            BGSettingUtil.BGSetting();
            AutoSaveThemeSettings();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag?.ToString() ?? "";
            new MyColorPickerDialog(tag, "ConfigWindowDialog");
        }

        public void BGStyle_Changed(object sender, RoutedEventArgs e)
        {
            BGSettingUtil.BGSetting();
            ApplyBackgroundConfigVisibility();
            AutoSaveThemeSettings();
        }

        private void BlurEffect_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            MainWindow.mainWindow?.ApplyGlassBackdrop();
            AutoSaveThemeSettings();
        }

        private void HideSearchBox_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            MainWindow.mainWindow?.ApplySearchBoxVisibility();
            AutoSaveThemeSettings();
        }

        private void ThemeMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || appConfig == null)
            {
                return;
            }

            AppThemeMode mode = ReferenceEquals(sender, LightThemeRadio)
                ? AppThemeMode.Light
                : ReferenceEquals(sender, DarkThemeRadio)
                    ? AppThemeMode.Dark
                    : AppThemeMode.System;

            AppearanceThemeMode appearanceMode = mode switch
            {
                AppThemeMode.Light => AppearanceThemeMode.Light,
                AppThemeMode.Dark => AppearanceThemeMode.Dark,
                _ => AppearanceThemeMode.System
            };
            appearanceCommands.SetTheme(new SetAppearanceThemeCommand(appearanceMode));
            BGSettingUtil.BGSetting();
            AutoSaveThemeSettings();
        }

        private void BGOpacity_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded) return;

            BGSettingUtil.BGSetting();
            AutoSaveThemeSettings();
        }

        private void SysBG_Click(object sender, RoutedEventArgs e)
        {
            GradientBGDialog gbg = new GradientBGDialog();
            gbg.dialog = DialogService.Show(gbg, "ConfigWindowDialog");
        }

        private void NewBGBtn_Click(object sender, RoutedEventArgs e)
        {
            BGNmaeDialog dialog = new BGNmaeDialog();
            dialog.dialog = DialogService.Show(dialog, "ConfigWindowDialog");
        }
    }
}
