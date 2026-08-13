using raindesk.Features.ImportExport.Contracts;

namespace raindesk.Features.ImportExport.Presentation;

public sealed class WinUiBackupFilePicker : IBackupFilePicker
{
    public async ValueTask<string?> PickImportPathAsync()
    {
        var picker = new global::Windows.Storage.Pickers.FileOpenPicker
        {
            SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.Desktop,
            ViewMode = global::Windows.Storage.Pickers.PickerViewMode.List
        };
        InitializeWithMainWindow(picker);
        picker.FileTypeFilter.Add(".bak");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add("*");

        global::Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async ValueTask<string?> PickExportPathAsync(string suggestedFileName)
    {
        var picker = new global::Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.Desktop,
            SuggestedFileName = suggestedFileName
        };
        InitializeWithMainWindow(picker);
        picker.FileTypeChoices.Add("备份文件", new List<string> { ".bak" });

        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private static void InitializeWithMainWindow(object picker)
    {
        MainWindow? window = MainWindow.mainWindow;
        if (window == null)
        {
            throw new InvalidOperationException("主窗口尚未初始化，无法打开文件选择器。");
        }

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }
}
