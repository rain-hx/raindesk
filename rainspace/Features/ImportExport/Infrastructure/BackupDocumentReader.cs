using Newtonsoft.Json;
using rainspace.Services;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Features.ImportExport.Infrastructure;

/// <summary>读取 JSON 或受支持的旧版备份，并执行不涉及写盘的兼容规范化。</summary>
public sealed class BackupDocumentReader
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static BackupDocumentReader Default { get; } = new();

    public AppData? Read(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                LogUtil.WriteErrorLog(new FileNotFoundException("备份文件不存在"), backupFilePath);
                return null;
            }

            LogUtil.WriteLog("开始导入备份文件: " + backupFilePath);
            string content = File.ReadAllText(backupFilePath);
            AppData? data;
            if (content.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                data = JsonConvert.DeserializeObject<AppData>(content, JsonSettings);
                if (data == null)
                {
                    return null;
                }
                LogUtil.WriteLog("成功读取 JSON 格式备份");
            }
            else
            {
                LogUtil.WriteLog("尝试读取 GeekDesk 旧版备份: " + backupFilePath);
                data = LegacyBackupImporter.TryImport(backupFilePath);
                if (data == null)
                {
                    return null;
                }
            }

            return Normalize(data);
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "读取备份文件失败: " + backupFilePath);
            return null;
        }
    }

    private static AppData Normalize(AppData data)
    {
        AllGroupsShortcutViewService.NormalizeOwnedFolderReferences(data);

        NormalizeHotkey(
            data.AppConfig.HotkeyStr,
            value => data.AppConfig.Hotkey = value,
            value => data.AppConfig.HotkeyModifiers = value);

        NormalizeHotkey(
            data.AppConfig.ToDoHotkeyStr,
            value => data.AppConfig.ToDoHotkey = value,
            value => data.AppConfig.ToDoHotkeyModifiers = value);

        return data;
    }

    private static void NormalizeHotkey(
        string? hotkeyText,
        Action<VirtualKey> setKey,
        Action<HotkeyModifiers> setModifiers)
    {
        if (string.IsNullOrWhiteSpace(hotkeyText))
        {
            return;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        string? keyToken = null;
        foreach (string rawPart in hotkeyText.Split(
                     '+',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string part = rawPart.Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
                || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.MOD_CONTROL;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.MOD_ALT;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.MOD_SHIFT;
            }
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase)
                     || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.MOD_WIN;
            }
            else
            {
                keyToken = part;
            }
        }

        if (keyToken != null && Enum.TryParse(keyToken, ignoreCase: true, out VirtualKey key))
        {
            setKey(key);
            setModifiers(modifiers);
        }
    }
}
