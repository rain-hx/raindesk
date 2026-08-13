using raindesk.Features.Launcher.Contracts;
using raindesk.Constant;
using raindesk.Util;

namespace raindesk.Features.Launcher.Infrastructure;

/// <summary>使用 Windows Shell/快捷方式解析能力把文件路径转换为 Launcher 草稿。</summary>
public sealed class WindowsLauncherShortcutDraftSource : ILauncherShortcutDraftSource
{
    public global::System.Threading.Tasks.Task<LauncherShortcutDraftResolution> ResolveAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.Run(
            () => Resolve(path, cancellationToken),
            cancellationToken);

    private static LauncherShortcutDraftResolution Resolve(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return LauncherShortcutDraftResolution.Failed(path ?? string.Empty, "文件不存在。");
        }

        try
        {
            string resolvedPath = path;
            string iconPath = path;
            string? startArgument = null;
            string extension = System.IO.Path.GetExtension(path) ?? string.Empty;
            bool isLink = extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase);
            if (isLink)
            {
                try
                {
                    string? targetPath = FileUtil.GetTargetPathByLnk(path);
                    iconPath = FileUtil.GetIconPathByLnk(path) ?? path;
                    startArgument = FileUtil.GetArgByLnk(path);
                    if (!string.IsNullOrWhiteSpace(targetPath))
                    {
                        resolvedPath = targetPath;
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "解析 Launcher 快捷方式失败: " + path);
                }
            }

            byte[]? imageBytes = ImageUtil.GetImageBytesByPath(iconPath);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                imageBytes = Convert.FromBase64String(Constants.DEFAULT_IMG_IMAGE_BASE64);
            }
            cancellationToken.ThrowIfCancellationRequested();
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name))
            {
                return LauncherShortcutDraftResolution.Failed(path, "无法读取快捷方式名称。");
            }

            return new LauncherShortcutDraftResolution(
                path,
                new LauncherShortcutDraft(
                    name,
                    resolvedPath,
                    startArgument,
                    isLink ? path : null,
                    imageBytes));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "解析 Launcher 外部文件失败: " + path);
            return LauncherShortcutDraftResolution.Failed(path, ex.Message);
        }
    }
}
