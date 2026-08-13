namespace raindesk.Services.Interfaces;

/// <summary>
/// 文件对话框抽象，便于单元测试时替换。
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 打开文件选择器，返回选中的文件路径；取消则返回 null。
    /// </summary>
    Task<string?> OpenFilePickerAsync(string[] fileTypes, string? suggestedStartLocation = null);

    /// <summary>
    /// 打开文件保存器，返回保存路径；取消则返回 null。
    /// </summary>
    Task<string?> SaveFilePickerAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices);
}
