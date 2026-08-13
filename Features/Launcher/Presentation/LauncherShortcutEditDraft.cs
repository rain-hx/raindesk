using raindesk.Features.Launcher.Contracts;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>快捷方式属性对话框的隔离编辑草稿；只有显式构造命令后才会写入 Store。</summary>
public sealed class LauncherShortcutEditDraft
{
    private byte[]? _imageBytes;
    private readonly byte[]? _defaultImageBytes;

    public LauncherShortcutEditDraft(
        string shortcutId,
        string name,
        string? path,
        string? relativePath,
        string? startArgument,
        bool adminStartUp,
        byte[]? imageBytes,
        byte[]? defaultImageBytes)
    {
        ShortcutId = shortcutId ?? string.Empty;
        Name = name ?? string.Empty;
        Path = path ?? string.Empty;
        RelativePath = relativePath ?? string.Empty;
        StartArgument = startArgument ?? string.Empty;
        AdminStartUp = adminStartUp;
        _imageBytes = imageBytes?.ToArray();
        _defaultImageBytes = defaultImageBytes?.ToArray();
    }

    public string ShortcutId { get; }
    public string Name { get; }
    public string Path { get; }
    public string RelativePath { get; }
    public string StartArgument { get; }
    public bool AdminStartUp { get; }
    public byte[]? ImageBytes => _imageBytes?.ToArray();
    public byte[]? DefaultImageBytes => _defaultImageBytes?.ToArray();
    public bool ImageChanged { get; private set; }

    public void ReplaceImage(byte[]? imageBytes)
    {
        _imageBytes = imageBytes?.ToArray();
        ImageChanged = true;
    }

    public bool RestoreDefaultImage()
    {
        if (_defaultImageBytes is not { Length: > 0 } bytes)
        {
            return false;
        }

        ReplaceImage(bytes);
        return true;
    }

    public UpdateLauncherShortcutCommand CreateUpdateCommand(
        string name,
        string? path,
        string? startArgument,
        bool adminStartUp) =>
        new(
            ShortcutId,
            new LauncherShortcutPropertiesUpdate(
                name,
                path,
                startArgument,
                adminStartUp,
                ImageBytes,
                ImageChanged));
}
