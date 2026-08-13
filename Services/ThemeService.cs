using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace raindesk.Services;

public static class ThemeService
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Guid, ThemeRegistration> RegisteredRoots = new();
    private static AppThemeMode currentMode = AppThemeMode.System;

    public static AppThemeMode CurrentMode
    {
        get
        {
            lock (SyncRoot)
            {
                return currentMode;
            }
        }
    }

    public static int RegisteredRootCount
    {
        get
        {
            lock (SyncRoot)
            {
                return RegisteredRoots.Count;
            }
        }
    }

    public static IDisposable RegisterRoot(FrameworkElement? root)
    {
        Guid id = Guid.NewGuid();
        AppThemeMode mode;

        lock (SyncRoot)
        {
            RegisteredRoots[id] = new ThemeRegistration(root, null);
            mode = currentMode;
        }

        ApplyRootTheme(root, ToElementTheme(mode));
        return new Registration(id);
    }

    public static IDisposable RegisterWindow(Window window, Action<AppThemeMode>? themeApplied = null)
    {
        if (window == null)
        {
            return RegisterRoot(null);
        }

        FrameworkElement? root = window.Content as FrameworkElement;
        IDisposable registration = RegisterRoot(root, themeApplied);
        window.Closed += (_, _) => registration.Dispose();
        return registration;
    }

    public static IDisposable RegisterRoot(FrameworkElement? root, Action<AppThemeMode>? themeApplied)
    {
        Guid id = Guid.NewGuid();
        ThemeRegistration registration = new(root, themeApplied);
        AppThemeMode mode;

        lock (SyncRoot)
        {
            RegisteredRoots[id] = registration;
            mode = currentMode;
        }

        ApplyRegistration(registration, mode);
        return new Registration(id);
    }

    public static void SetMode(string? persistedMode)
    {
        SetMode(NormalizeMode(persistedMode));
    }

    public static void SetMode(AppThemeMode mode)
    {
        List<ThemeRegistration> registrations;

        lock (SyncRoot)
        {
            currentMode = mode;
            registrations = new List<ThemeRegistration>(RegisteredRoots.Values);
        }

        foreach (ThemeRegistration registration in registrations)
        {
            ApplyRegistration(registration, mode);
        }
    }

    public static AppThemeMode NormalizeMode(string? persistedMode)
    {
        return Enum.TryParse(persistedMode, true, out AppThemeMode mode)
            ? mode
            : AppThemeMode.System;
    }

    public static string ToPersistedValue(AppThemeMode mode)
    {
        return mode.ToString();
    }

    public static ElementTheme ToElementTheme(AppThemeMode mode)
    {
        return mode switch
        {
            AppThemeMode.Light => ElementTheme.Light,
            AppThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public static bool IsDarkModeEffective()
    {
        return IsDarkModeEffective(CurrentMode);
    }

    public static bool IsDarkModeEffective(AppThemeMode mode)
    {
        return mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => IsSystemThemeDark()
        };
    }

    public static Color GetAppBackgroundColor(AppThemeMode mode)
    {
        return IsDarkModeEffective(mode)
            ? ColorHelper.FromArgb(255, 14, 23, 38)
            : ColorHelper.FromArgb(255, 246, 249, 254);
    }

    public static Color GetTitleBarColor(AppThemeMode mode)
    {
        return GetAppBackgroundColor(mode);
    }

    public static Color GetRailSelectedBackgroundColor(AppThemeMode mode)
    {
        return IsDarkModeEffective(mode)
            ? ColorHelper.FromArgb(255, 18, 63, 112)
            : ColorHelper.FromArgb(255, 232, 243, 255);
    }

    public static Color GetRailSelectedForegroundColor(AppThemeMode mode)
    {
        return IsDarkModeEffective(mode)
            ? ColorHelper.FromArgb(255, 174, 214, 255)
            : ColorHelper.FromArgb(255, 0, 112, 224);
    }

    public static Color GetRailNormalForegroundColor(AppThemeMode mode)
    {
        return IsDarkModeEffective(mode)
            ? ColorHelper.FromArgb(255, 180, 194, 216)
            : ColorHelper.FromArgb(255, 77, 98, 138);
    }

    internal static void ClearRegistrationsForTests()
    {
        lock (SyncRoot)
        {
            RegisteredRoots.Clear();
            currentMode = AppThemeMode.System;
        }
    }

    private static void Unregister(Guid id)
    {
        lock (SyncRoot)
        {
            RegisteredRoots.Remove(id);
        }
    }

    private static void ApplyRegistration(ThemeRegistration registration, AppThemeMode mode)
    {
        ElementTheme theme = ToElementTheme(mode);
        FrameworkElement? root = registration.Root;

        if (root == null)
        {
            registration.ThemeApplied?.Invoke(mode);
            return;
        }

        if (root.DispatcherQueue == null || root.DispatcherQueue.HasThreadAccess)
        {
            root.RequestedTheme = theme;
            registration.ThemeApplied?.Invoke(mode);
            return;
        }

        root.DispatcherQueue.TryEnqueue(() =>
        {
            root.RequestedTheme = theme;
            registration.ThemeApplied?.Invoke(mode);
        });
    }

    private static bool IsSystemThemeDark()
    {
        try
        {
            var settings = new Windows.UI.ViewManagement.UISettings();
            return settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).R < 128;
        }
        catch
        {
            return Application.Current?.RequestedTheme == ApplicationTheme.Dark;
        }
    }

    private static void ApplyRootTheme(FrameworkElement? root, ElementTheme theme)
    {
        if (root == null)
        {
            return;
        }

        if (root.DispatcherQueue == null || root.DispatcherQueue.HasThreadAccess)
        {
            root.RequestedTheme = theme;
            return;
        }

        root.DispatcherQueue.TryEnqueue(() => root.RequestedTheme = theme);
    }

    private sealed class ThemeRegistration
    {
        public ThemeRegistration(FrameworkElement? root, Action<AppThemeMode>? themeApplied)
        {
            Root = root;
            ThemeApplied = themeApplied;
        }

        public FrameworkElement? Root { get; }

        public Action<AppThemeMode>? ThemeApplied { get; }
    }

    private sealed class Registration : IDisposable
    {
        private readonly Guid id;
        private bool disposed;

        public Registration(Guid id)
        {
            this.id = id;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Unregister(id);
        }
    }
}
