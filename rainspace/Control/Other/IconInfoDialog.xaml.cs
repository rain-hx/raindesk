using rainspace.Constant;
using rainspace.Controls;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Services;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Presentation;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace rainspace.Control.Other
{
    /// <summary>
    /// WinUI 3 dialog for editing icon properties (name, icon, admin mode, args).
    /// </summary>
    public partial class IconInfoDialog : UserControl
    {
        private readonly ILauncherCommandHandler _commandHandler = AppServices.Get<ILauncherCommandHandler>();
        private readonly LauncherShortcutEditDraft? _draft;
        public Microsoft.UI.Xaml.Window? dialog;

        public IconInfoDialog()
        {
            this.InitializeComponent();
        }

        public IconInfoDialog(IconInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);
            _draft = LauncherShortcutEditPresentationMapper.ToDraft(info);
            this.DataContext = _draft;
            this.InitializeComponent();
            IconImg.Source = _draft.ImageBytes is { Length: > 0 } bytes
                ? ImageUtil.ByteArrToImage(bytes)
                : info.BitmapImage;
        }

        private void SaveProperty(object sender, RoutedEventArgs e)
        {
            if (_draft != null)
            {
                LauncherCommandResult result = _commandHandler.Update(_draft.CreateUpdateCommand(
                    IconName.Text,
                    _draft.Path,
                    StartArg.Text,
                    IconIsAdmin.IsChecked == true));
                if (!result.Success)
                {
                    GrowlService.Warning(result.Error ?? "保存快捷方式属性失败。");
                    return;
                }
            }
            dialog?.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            dialog?.Close();
        }

        private void ReStoreImage(object sender, RoutedEventArgs e)
        {
            if (_draft?.RestoreDefaultImage() == true
                && _draft.ImageBytes is { Length: > 0 } bytes)
            {
                IconImg.Source = ImageUtil.ByteArrToImage(bytes);
            }
        }

        private async void EditImage(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WindowNative.GetWindowHandle(MainWindow.mainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add("*");

                var file = await picker.PickSingleFileAsync();
                if (file != null && _draft != null)
                {
                    byte[] imageBytes = ImageUtil.GetImageBytesByPath(file.Path);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        _draft.ReplaceImage(imageBytes);
                        IconImg.Source = ImageUtil.ByteArrToImage(imageBytes);
                    }
                    else
                    {
                        GrowlService.Warning("无法从所选文件提取可持久化图标。");
                    }
                }
            }
            catch (Exception ex)
            {
                GrowlService.WarningGlobal("修改图标失败,已重置为默认图标!");
                LogUtil.WriteErrorLog(ex, "修改图标失败!");
            }
        }
    }
}
