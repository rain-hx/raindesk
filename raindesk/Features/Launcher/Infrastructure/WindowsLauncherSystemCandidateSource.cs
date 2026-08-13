using System.Collections;
using raindesk.Constant;
using raindesk.Features.Launcher.Contracts;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Infrastructure;

/// <summary>Windows 系统图标资源与开始菜单目录的 Launcher 候选项适配器。</summary>
public sealed class WindowsLauncherSystemCandidateSource : ILauncherSystemCandidateSource
{
    public global::System.Threading.Tasks.Task<IReadOnlyList<LauncherSystemCandidate>> GetBuiltInSystemItemsAsync(
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.Run<IReadOnlyList<LauncherSystemCandidate>>(
            () => LoadBuiltInSystemItems(cancellationToken),
            cancellationToken);

    public global::System.Threading.Tasks.Task<IReadOnlyList<LauncherSystemCandidate>> GetStartMenuItemsAsync(
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.Run<IReadOnlyList<LauncherSystemCandidate>>(
            () => LoadStartMenuItems(cancellationToken),
            cancellationToken);

    private static IReadOnlyList<LauncherSystemCandidate> LoadBuiltInSystemItems(
        CancellationToken cancellationToken)
    {
        var results = new List<LauncherSystemCandidate>();
        Hashtable systemApps = Constants.SYSTEM_APP_NAMES;
        foreach (object key in systemApps.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string keyText = key.ToString() ?? string.Empty;
                string name = systemApps[key]?.ToString() ?? keyText;
                string imagePath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Resource",
                    "Image",
                    "SystemIcon",
                    keyText + ".png");
                byte[]? imageBytes = File.Exists(imagePath)
                    ? File.ReadAllBytes(imagePath)
                    : null;
                results.Add(new LauncherSystemCandidate(
                    name,
                    StartArgument: keyText,
                    Content: name,
                    ImageBytes: imageBytes));
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "加载 Launcher 系统候选项失败: " + key);
            }
        }
        return results;
    }

    private static IReadOnlyList<LauncherSystemCandidate> LoadStartMenuItems(
        CancellationToken cancellationToken)
    {
        var paths = new List<string>();
        CollectStartMenuFiles(
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs"),
            paths,
            cancellationToken);
        CollectStartMenuFiles(
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs"),
            paths,
            cancellationToken);

        var results = new List<LauncherSystemCandidate>();
        foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                IconInfo? icon = CommonCode.GetIconInfoByPath_NoWrite(path);
                if (icon?.Path_NoWrite == null)
                {
                    continue;
                }

                results.Add(new LauncherSystemCandidate(
                    icon.Name_NoWrite ?? System.IO.Path.GetFileNameWithoutExtension(path),
                    icon.Path_NoWrite,
                    icon.StartArg_NoWrite,
                    icon.LnkPath_NoWrite,
                    icon.Path_NoWrite + "\n" + icon.Name_NoWrite,
                    icon.ImageByteArr_NoWrite));
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取 Launcher 开始菜单候选项失败: " + path);
            }
        }
        return results;
    }

    private static void CollectStartMenuFiles(
        string directoryPath,
        ICollection<string> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            foreach (string path in Directory.EnumerateFiles(directoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains("卸载", StringComparison.OrdinalIgnoreCase)
                    || fileName.Contains("uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string extension = System.IO.Path.GetExtension(path);
                if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(path);
                }
            }

            foreach (string directory in Directory.EnumerateDirectories(directoryPath))
            {
                CollectStartMenuFiles(directory, results, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "扫描 Launcher 开始菜单目录失败: " + directoryPath);
        }
    }
}
