using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.ImportExport.Contracts;
using raindesk.Services;
using raindesk.Util;

namespace raindesk;

public sealed partial class MainWindow
{
    /// <summary>
    /// 从备份文件导入数据，成功后重启应用以加载新数据。
    /// </summary>
    private async global::System.Threading.Tasks.Task ImportBackupAsync()
    {
        // 先显示窗口，否则文件选择器无法弹出
        ShowApp();

        IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
        IBackupService? backupService = AppServices.TryGet<IBackupService>();
        if (picker == null || backupService == null)
        {
            var unavailableDialog = new ContentDialog
            {
                Title = "导入失败",
                Content = "备份服务尚未初始化。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await unavailableDialog.ShowAsync();
            return;
        }

        string? sourcePath = await picker.PickImportPathAsync();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        BackupOperationResult result = await backupService.ImportAsync(sourcePath);
        if (!result.Success)
        {
            var failedDialog = new ContentDialog
            {
                Title = "导入失败",
                Content = result.Error ?? "无法导入备份文件。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await failedDialog.ShowAsync();
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "导入成功",
            Content = "备份导入成功！应用将重新启动以加载新数据。",
            CloseButtonText = "确定",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
        ProcessUtil.ReStartApp();
    }

    /// <summary>
    /// 将当前数据导出为备份文件。
    /// </summary>
    private async global::System.Threading.Tasks.Task ExportBackupAsync()
    {
        IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
        IBackupService? backupService = AppServices.TryGet<IBackupService>();
        if (picker == null || backupService == null)
        {
            return;
        }

        string? destinationPath = await picker.PickExportPathAsync(
            "Data-GD-" + System.DateTime.Now.ToString("yyMMdd"));
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        BackupOperationResult result = await backupService.ExportAsync(destinationPath);
        if (result.Success)
        {
            var dialog = new ContentDialog
            {
                Title = "备份成功",
                Content = "数据已导出到 .bak 文件。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "备份失败",
                Content = result.Error ?? "无法写入备份文件。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
