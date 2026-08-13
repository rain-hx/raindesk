using System;
using raindesk.Util;
using UiVirtualKey = Windows.System.VirtualKey;

namespace raindesk.Services
{
    internal static class HotkeyInput
    {
        public static bool IsModifierKey(UiVirtualKey key)
        {
            return key == UiVirtualKey.Control
                || key == UiVirtualKey.LeftControl
                || key == UiVirtualKey.RightControl
                || key == UiVirtualKey.Shift
                || key == UiVirtualKey.LeftShift
                || key == UiVirtualKey.RightShift
                || key == UiVirtualKey.Menu
                || key == UiVirtualKey.LeftMenu
                || key == UiVirtualKey.RightMenu
                || key == UiVirtualKey.LeftWindows
                || key == UiVirtualKey.RightWindows;
        }

        public static string GetModifierName(UiVirtualKey key)
        {
            return GetModifier(key) switch
            {
                HotkeyModifiers.MOD_CONTROL => "Ctrl + ",
                HotkeyModifiers.MOD_WIN => "Win + ",
                HotkeyModifiers.MOD_SHIFT => "Shift + ",
                HotkeyModifiers.MOD_ALT => "Alt + ",
                _ => string.Empty,
            };
        }

        public static HotkeyModifiers GetModifier(UiVirtualKey key)
        {
            return key switch
            {
                UiVirtualKey.Control or UiVirtualKey.LeftControl or UiVirtualKey.RightControl => HotkeyModifiers.MOD_CONTROL,
                UiVirtualKey.LeftWindows or UiVirtualKey.RightWindows => HotkeyModifiers.MOD_WIN,
                UiVirtualKey.Shift or UiVirtualKey.LeftShift or UiVirtualKey.RightShift => HotkeyModifiers.MOD_SHIFT,
                UiVirtualKey.Menu or UiVirtualKey.LeftMenu or UiVirtualKey.RightMenu => HotkeyModifiers.MOD_ALT,
                _ => HotkeyModifiers.None,
            };
        }

        public static bool TryParseDisplayString(string? display, out HotkeyModifiers modifiers, out VirtualKey key)
        {
            modifiers = HotkeyModifiers.None;
            key = VirtualKey.None;
            if (string.IsNullOrWhiteSpace(display))
            {
                return false;
            }

            string[] parts = display.Split([" + "], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            string keyPart = parts[^1].Trim();
            if (!Enum.TryParse(keyPart, true, out key) || key == VirtualKey.None)
            {
                return false;
            }

            for (int i = 0; i < parts.Length - 1; i++)
            {
                HotkeyModifiers mod = parts[i].Trim() switch
                {
                    "Ctrl" => HotkeyModifiers.MOD_CONTROL,
                    "Alt" => HotkeyModifiers.MOD_ALT,
                    "Shift" => HotkeyModifiers.MOD_SHIFT,
                    "Win" => HotkeyModifiers.MOD_WIN,
                    _ => HotkeyModifiers.None,
                };
                if (mod == HotkeyModifiers.None)
                {
                    return false;
                }

                modifiers |= mod;
            }

            return modifiers != HotkeyModifiers.None;
        }
    }
}
