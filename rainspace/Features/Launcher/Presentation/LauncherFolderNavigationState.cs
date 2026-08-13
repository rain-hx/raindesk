using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation;

/// <summary>
/// Launcher 文件夹浮层的纯导航路径状态。
/// 不负责窗口、动画、持久化或拖放。
/// </summary>
internal sealed class LauncherFolderNavigationState
{
    private readonly List<IconInfo> _path = new();

    public IconInfo? Folder => _path.Count == 0 ? null : _path[^1];

    public IReadOnlyList<IconInfo> Path => _path;

    public bool IsOpen => Folder?.IsFolder == true;

    public bool CanGoBack => _path.Count > 1;

    public void OpenRoot(IconInfo folder)
    {
        _path.Clear();
        if (folder?.IsFolder == true)
        {
            _path.Add(folder);
        }
    }

    public void OpenChild(IconInfo folder)
    {
        if (folder?.IsFolder == true)
        {
            _path.Add(folder);
        }
    }

    public void Back()
    {
        if (_path.Count > 1)
        {
            _path.RemoveAt(_path.Count - 1);
        }
    }

    public void Close() => _path.Clear();
}
