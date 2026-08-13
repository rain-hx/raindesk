using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace rainspace.Util
{
    /// <summary>
    /// 对话框辅助工具类
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// 获取 XamlRoot
        /// </summary>
        private static Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
        {
            try
            {
                return App.MainWindow?.Content?.XamlRoot;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public static async System.Threading.Tasks.Task ShowErrorAsync(string title, string message, Exception ex = null)
        {
            var content = message;
            if (ex != null)
            {
                content += $"\n\n详细信息：{ex.Message}";
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示成功提示
        /// </summary>
        public static async System.Threading.Tasks.Task ShowSuccessAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "成功",
                Content = message,
                CloseButtonText = "确定"
            };

            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> ConfirmAsync(string title, string message, string confirmText = "确定", string cancelText = "取消")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = confirmText,
                CloseButtonText = cancelText,
                DefaultButton = ContentDialogButton.Close
            };

            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示删除确认对话框
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> ConfirmDeleteAsync(string itemName)
        {
            return await ConfirmAsync(
                "确认删除",
                $"确定要删除「{itemName}」吗？\n\n此操作无法撤销。",
                "删除",
                "取消"
            );
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public static async System.Threading.Tasks.Task ShowWarningAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "警告",
                Content = message,
                CloseButtonText = "确定"
            };

            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public static async System.Threading.Tasks.Task ShowInfoAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定"
            };

            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
                dialog.XamlRoot = xamlRoot;

            await dialog.ShowAsync();
        }
    }
}
