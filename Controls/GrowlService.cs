using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Controls;

/// <summary>
/// In-app toast and confirmation notifications.
/// </summary>
public static class GrowlService
{
    public static void Success(string message, string? token = null)
    {
        ToastHelper.Success(message);
    }

    public static void Warning(string message, string? token = null)
    {
        ToastHelper.Warning(message);
    }

    public static void WarningGlobal(string message)
    {
        ToastHelper.Warning(message);
    }

    public static void SuccessGlobal(string message)
    {
        ToastHelper.Success(message);
    }

    /// <summary>
    /// 显示确认对话框并返回用户选择。无法展示对话框时返回 <see langword="false"/>。
    /// 事件处理器可安全地 await 此方法；不再使用 async void 非事件 API。
    /// </summary>
    public static async Task<bool> AskAsync(string message, string? token = null)
    {
        try
        {
            var root = (App.MainWindow as MainWindow)?.Content?.XamlRoot;
            if (root == null)
            {
                return false;
            }

            var dialog = new ContentDialog
            {
                Title = "确认",
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = root,
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "GrowlService Ask failed");
            return false;
        }
    }
}
