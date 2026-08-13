using raindesk.ViewModel;

namespace raindesk.Services;

public static class ShortcutIdentityService
{
    public static bool EnsureIconIds(AppData? data)
    {
        if (data?.MenuList == null)
        {
            return false;
        }

        bool changed = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (IconInfo icon in ShortcutFolderTreeService.EnumerateIcons(data.MenuList))
        {
            if (icon == null)
            {
                continue;
            }

            string id = icon.IconId;
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
            {
                string newId;
                do
                {
                    newId = Guid.NewGuid().ToString("N");
                }
                while (!seen.Add(newId));

                icon.IconId = newId;
                changed = true;
            }
        }

        return changed;
    }
}
