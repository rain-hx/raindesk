using rainspace.Constant;
using rainspace.Controls;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Services;
using rainspace.Services.Interfaces;
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
    /// WinUI 3 dialog for URL-based icon properties.
    /// </summary>
    public partial class IconInfoUrlDialog : UserControl
    {
        public Microsoft.UI.Xaml.Window? dialog;
        public event EventHandler? Saved;
        public event EventHandler? Canceled;
        private readonly bool newIconInfo;
        private readonly LauncherShortcutEditDraft _draft;
        private readonly IAppDataProvider _dataProvider = AppServices.Get<IAppDataProvider>();
        private readonly ILauncherCommandHandler _commandHandler = AppServices.Get<ILauncherCommandHandler>();

        public IconInfoUrlDialog()
        {
            newIconInfo = true;
            byte[] defaultIconBytes = Convert.FromBase64String(Constants.URL_ICON_IMG_BASE64);
            _draft = new LauncherShortcutEditDraft(
                Guid.NewGuid().ToString("N"),
                string.Empty,
                string.Empty,
                null,
                null,
                adminStartUp: false,
                defaultIconBytes,
                defaultIconBytes);
            this.DataContext = _draft;
            this.InitializeComponent();
            IconImg.Source = ImageUtil.ByteArrToImage(defaultIconBytes);
        }

        public IconInfoUrlDialog(IconInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);
            _draft = LauncherShortcutEditPresentationMapper.ToDraft(info);
            this.DataContext = _draft;
            newIconInfo = false;
            this.InitializeComponent();
            IconImg.Source = _draft.ImageBytes is { Length: > 0 } bytes
                ? ImageUtil.ByteArrToImage(bytes)
                : info.BitmapImage;
        }

        private void SaveProperty(object sender, RoutedEventArgs e)
        {
            LauncherCommandResult result;
            if (newIconInfo)
            {
                AppData data = _dataProvider.Current;
                int index = data.AppConfig.SelectedMenuIndex;
                if (index < 0 || index >= data.MenuList.Count || data.MenuList[index].IsEncrypt)
                {
                    GrowlService.Warning("请先选择一个可写分组。");
                    return;
                }

                result = _commandHandler.Add(new AddLauncherShortcutCommand(
                    data.MenuList[index].MenuId,
                    new LauncherShortcutDraft(
                        IconName.Text,
                        Path: IconUrl.Text,
                        ImageBytes: _draft.ImageBytes,
                        Kind: LauncherShortcutKind.Url)));
            }
            else
            {
                result = _commandHandler.Update(_draft.CreateUpdateCommand(
                    IconName.Text,
                    IconUrl.Text,
                    _draft.StartArgument,
                    _draft.AdminStartUp));
            }

            if (!result.Success)
            {
                GrowlService.Warning(result.Error ?? "保存 URL 快捷方式失败。");
                return;
            }
            if (dialog != null)
            {
                dialog.Close();
            }
            else
            {
                Saved?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (dialog != null)
            {
                dialog.Close();
            }
            else
            {
                Canceled?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ReStoreImage(object sender, RoutedEventArgs e)
        {
            if (_draft.RestoreDefaultImage()
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
                if (file != null)
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
