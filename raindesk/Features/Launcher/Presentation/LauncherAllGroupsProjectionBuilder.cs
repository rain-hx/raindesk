using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// Builds the read-only All Groups tile projection from an optional in-session
/// order, persisted stable/legacy keys, and the current canonical model.
/// </summary>
public static class LauncherAllGroupsProjectionBuilder
{
    public static List<IconInfo> Build(
        AppData data,
        IReadOnlyList<IconInfo>? preferredOrder = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        List<IconInfo> defaultOrder = BuildDefault(data);
        IReadOnlyList<IconInfo> seed = preferredOrder is { Count: > 0 }
            ? preferredOrder
            : BuildPersistedOrder(data);
        if (seed.Count == 0)
        {
            seed = defaultOrder;
        }

        var projection = new List<IconInfo>();
        var used = new HashSet<IconInfo>();
        foreach (IconInfo icon in seed)
        {
            AddIfVisible(data, icon, projection, used);
        }
        foreach (IconInfo icon in defaultOrder)
        {
            AddIfVisible(data, icon, projection, used);
        }
        return projection;
    }

    public static List<IconInfo> BuildDefault(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return AllGroupsShortcutViewService.BuildDefaultFlatOrder(
            data.MenuList,
            data.AppConfig.AllGroupsIconList);
    }

    private static List<IconInfo> BuildPersistedOrder(AppData data)
    {
        IReadOnlyList<string>? keys = data.AppConfig.AllGroupsFlatIconOrderKeys;
        if (keys == null || keys.Count == 0)
        {
            return new List<IconInfo>();
        }

        Dictionary<string, IconInfo> byId = data.AppConfig.AllGroupsIconList
            .Concat(ShortcutFolderTreeService.EnumerateIcons(data.MenuList))
            .Where(icon => icon != null && !string.IsNullOrWhiteSpace(icon.IconId))
            .GroupBy(icon => icon.IconId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        List<IconInfo> candidates = data.AppConfig.AllGroupsIconList
            .Concat(ShortcutFolderTreeService.EnumerateIcons(data.MenuList))
            .Where(icon => icon != null)
            .ToList();

        var result = new List<IconInfo>();
        var used = new HashSet<IconInfo>();
        foreach (string key in keys)
        {
            IconInfo? icon = ResolveOrderKey(key, byId, candidates);
            if (icon != null && used.Add(icon))
            {
                result.Add(icon);
            }
        }
        return result;
    }

    private static IconInfo? ResolveOrderKey(
        string? key,
        IReadOnlyDictionary<string, IconInfo> byId,
        IReadOnlyList<IconInfo> candidates)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }
        if (key.StartsWith("id:", StringComparison.Ordinal))
        {
            return byId.GetValueOrDefault(key[3..]);
        }

        IconInfo? exact = candidates.FirstOrDefault(icon =>
            string.Equals(BuildLegacyOrderKey(icon), key, StringComparison.Ordinal));
        if (exact != null)
        {
            return exact;
        }

        int firstSeparator = key.IndexOf('\u001f');
        if (firstSeparator < 0 || firstSeparator >= key.Length - 1)
        {
            return null;
        }
        string suffix = key[(firstSeparator + 1)..];
        int secondSeparator = suffix.IndexOf('\u001f');
        if (secondSeparator <= 0)
        {
            return null;
        }

        string path = suffix[..secondSeparator];
        string name = suffix[(secondSeparator + 1)..];
        return candidates.FirstOrDefault(icon =>
            string.Equals(icon.Path_NoWrite, path, StringComparison.Ordinal)
            && string.Equals(icon.Name_NoWrite, name, StringComparison.Ordinal));
    }

    private static string BuildLegacyOrderKey(IconInfo icon) =>
        $"{icon.Path_NoWrite ?? string.Empty}\u001f{icon.Name_NoWrite ?? string.Empty}\u001f{icon.LnkPath_NoWrite ?? string.Empty}\u001f{icon.StartArg_NoWrite ?? string.Empty}";

    private static void AddIfVisible(
        AppData data,
        IconInfo? icon,
        ICollection<IconInfo> projection,
        ISet<IconInfo> used)
    {
        if (icon == null
            || icon.IsAddShortcut_NoWrite
            || !AllGroupsShortcutViewService.IsVisibleFlatItem(
                data.MenuList,
                data.AppConfig.AllGroupsIconList,
                icon)
            || !used.Add(icon))
        {
            return;
        }

        icon.IsAddShortcut_NoWrite = false;
        projection.Add(icon);
    }
}
