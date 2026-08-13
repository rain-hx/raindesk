using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using rainspace.Plugins.EveryThing.Constant;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Plugins.EveryThing;

/// <summary>
/// Everything search integration (requires Everything running with IPC enabled).
/// </summary>
public static class EveryThingUtil
{
    private static bool _enabled;
    private static bool _available;
    private static bool _availabilityChecked;

    public static bool IsAvailable
    {
        get
        {
            EnsureAvailabilityChecked();
            return _available;
        }
    }

    public static bool IsEnabled => _enabled && IsAvailable;

    public static void EnableEveryThing(int mode)
    {
        EnsureAvailabilityChecked();
        if (!_available)
        {
            LogUtil.WriteLog("EveryThingUtil: Everything SDK unavailable");
            return;
        }

        _enabled = true;
        LogUtil.WriteLog($"EveryThingUtil: enabled (mode={mode})");
    }

    public static void DisableEveryThing(bool full = false)
    {
        _enabled = false;
        if (!full || !_available)
        {
            return;
        }

        try
        {
            EveryThing64.Everything_Reset();
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "EveryThingUtil disable failed");
        }
    }

    public static void Search(string keyword)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        EnsureAvailabilityChecked();
        if (!_available)
        {
            return;
        }

        try
        {
            EveryThing64.Everything_SetSearchW(keyword.Trim());
            EveryThing64.Everything_SetRequestFlags(EveryThingConst.EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
            if (!EveryThing64.Everything_Query(true))
            {
                uint error = EveryThing64.Everything_GetLastError();
                LogUtil.WriteLog($"EveryThingUtil: query failed, error={error}");
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "EveryThingUtil search failed");
        }
    }

    public static IReadOnlyList<IconInfo> SearchIcons(string keyword, int maxResults = 40)
    {
        var results = new List<IconInfo>();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return results;
        }

        Search(keyword);
        if (!IsAvailable)
        {
            return results;
        }

        try
        {
            uint count = Math.Min(EveryThing64.Everything_GetNumResults(), (uint)maxResults);
            var pathBuilder = new StringBuilder(512);
            for (uint i = 0; i < count; i++)
            {
                pathBuilder.Clear();
                EveryThing64.Everything_GetResultFullPathName(i, pathBuilder, (uint)pathBuilder.Capacity);
                string path = pathBuilder.ToString();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                results.Add(CommonCode.GetIconInfoByPath(path));
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "EveryThingUtil SearchIcons failed");
        }

        return results;
    }

    private static void EnsureAvailabilityChecked()
    {
        if (_availabilityChecked)
        {
            return;
        }

        _availabilityChecked = true;
        try
        {
            EveryThing64.Everything_Reset();
            _available = true;
        }
        catch (DllNotFoundException ex)
        {
            LogUtil.WriteLog("EveryThingUtil: Everything64.dll not found: " + ex.Message);
            _available = false;
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "EveryThingUtil availability check failed");
            _available = false;
        }
    }
}
