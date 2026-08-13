using rainspace.Constant;
using rainspace.Controls;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;
using Microsoft.UI.Dispatching;
using System;

namespace rainspace.Services
{
    /// <summary>
    /// 全局热键注册服务（低级别键盘 Hook，避免 RegisterHotKey 冲突）。
    /// </summary>
    internal static class HotkeyService
    {
        private sealed class HotkeyBinding
        {
            public HotkeyModifiers Modifiers { get; init; }
            public VirtualKey Key { get; init; }
        }

        private static UserActivityHook? _keyboardHook;
        private static volatile HotkeyBinding? _mainBinding;
        private static volatile HotkeyBinding? _todoBinding;
        private static volatile bool _mainPressed;
        private static volatile bool _todoPressed;

        private static DispatcherQueue? _dispatcherQueue;
        private static Action? _showAppAction;
        private static Action? _showToDoAction;

        private static string? _mainHotKeyError;
        private static string? _todoHotKeyError;
        private static volatile int _captureDepth;

        private static AppConfig? CurrentConfig => AppServices.IsConfigured
            ? AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig
            : null;

        public static bool IsCapturingHotkey => _captureDepth > 0;

        public static HotkeyRegistrationStatus CurrentStatus => new HotkeyRegistrationStatus
        {
            MainRegistered = _mainBinding != null && string.IsNullOrWhiteSpace(_mainHotKeyError),
            TodoRegistered = _todoBinding != null && string.IsNullOrWhiteSpace(_todoHotKeyError),
            MainUsesAltWHook = UsesAltHook(_mainBinding),
            TodoUsesAltHook = UsesAltHook(_todoBinding),
            MainError = _mainHotKeyError,
            TodoError = _todoHotKeyError,
        };

        public static void Initialize(
            DispatcherQueue dispatcherQueue,
            Action showApp,
            Action showToDo)
        {
            _dispatcherQueue = dispatcherQueue;
            _showAppAction = showApp;
            _showToDoAction = showToDo;
        }

        public static void RegisterAll(bool showMsg)
        {
            RegisterMainHotKey(showMsg);
            RegisterToDoHotKey(showMsg);
        }

        public static void UnregisterAll()
        {
            _mainBinding = null;
            _todoBinding = null;
            _mainPressed = false;
            _todoPressed = false;
            _mainHotKeyError = null;
            _todoHotKeyError = null;
            StopKeyboardHookIfUnused();
        }

        public static void EnterCaptureMode()
        {
            if (Interlocked.Increment(ref _captureDepth) == 1)
            {
                UnregisterAll();
                LogUtil.WriteLog("快捷键录入模式：已暂停全局热键");
            }
        }

        public static void ExitCaptureMode(bool showMsg = false)
        {
            int prev = Interlocked.Decrement(ref _captureDepth);
            if (prev < 0)
            {
                Interlocked.CompareExchange(ref _captureDepth, 0, prev);
                return;
            }
            if (prev == 0)
            {
                RegisterAll(showMsg);
                LogUtil.WriteLog("快捷键录入模式：已恢复全局热键");
            }
        }

        public static void RegisterMainHotKey(bool showMsg)
        {
            _mainBinding = null;
            _mainHotKeyError = null;
            _mainPressed = false;

            AppConfig? config = CurrentConfig;
            if (config?.EnableAppHotKey != true
                || !IsValidHotKey(config.HotkeyModifiers_NoWrite, config.Hotkey_NoWrite))
            {
                StopKeyboardHookIfUnused();
                return;
            }

            if (IsSameHotKey(
                    config.HotkeyModifiers_NoWrite,
                    config.Hotkey_NoWrite,
                    config.ToDoHotkeyModifiers_NoWrite,
                    config.ToDoHotkey_NoWrite))
            {
                _mainHotKeyError = "主面板快捷键与待办快捷键重复";
                if (showMsg)
                {
                    GrowlService.WarningGlobal(_mainHotKeyError);
                }
                return;
            }

            try
            {
                _mainBinding = CreateBinding(config.HotkeyModifiers_NoWrite, config.Hotkey_NoWrite);
                EnsureKeyboardHook();
                LogUtil.WriteLog($"主面板快捷键 {config.HotkeyStr} 已注册（键盘 Hook）");
            }
            catch (Exception ex)
            {
                _mainBinding = null;
                _mainHotKeyError = ex.Message;
                LogUtil.WriteErrorLog(ex, "注册主面板快捷键失败");
                if (showMsg)
                {
                    GrowlService.WarningGlobal("注册主面板快捷键失败，请更换组合键。");
                }
            }
        }

        public static void RegisterToDoHotKey(bool showMsg)
        {
            _todoBinding = null;
            _todoHotKeyError = null;
            _todoPressed = false;

            AppConfig? config = CurrentConfig;
            if (config?.EnableTodoHotKey != true
                || !IsValidHotKey(config.ToDoHotkeyModifiers_NoWrite, config.ToDoHotkey_NoWrite))
            {
                StopKeyboardHookIfUnused();
                return;
            }

            if (IsSameHotKey(
                    config.HotkeyModifiers_NoWrite,
                    config.Hotkey_NoWrite,
                    config.ToDoHotkeyModifiers_NoWrite,
                    config.ToDoHotkey_NoWrite))
            {
                _todoHotKeyError = "待办快捷键与主面板快捷键重复";
                if (showMsg)
                {
                    GrowlService.WarningGlobal(_todoHotKeyError);
                }
                return;
            }

            try
            {
                _todoBinding = CreateBinding(config.ToDoHotkeyModifiers_NoWrite, config.ToDoHotkey_NoWrite);
                EnsureKeyboardHook();
                LogUtil.WriteLog($"待办快捷键 {config.ToDoHotkeyStr} 已注册（键盘 Hook）");
            }
            catch (Exception ex)
            {
                _todoBinding = null;
                _todoHotKeyError = ex.Message;
                LogUtil.WriteErrorLog(ex, "注册待办快捷键失败");
                if (showMsg)
                {
                    GrowlService.WarningGlobal("注册待办快捷键失败，请更换组合键。");
                }
            }
        }

        private static HotkeyBinding CreateBinding(HotkeyModifiers modifiers, VirtualKey key)
        {
            return new HotkeyBinding
            {
                Modifiers = NormalizeModifiers(modifiers),
                Key = key
            };
        }

        private static void EnsureKeyboardHook()
        {
            if (_keyboardHook != null) return;

            _keyboardHook = new UserActivityHook(false, true);
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.KeyUp += KeyboardHook_KeyUp;
            _keyboardHook.ShouldSuppressKeyDown = ShouldSuppressRegisteredHotKey;
        }

        private static bool ShouldSuppressRegisteredHotKey(int vkCode)
        {
            if (IsCapturingHotkey) return false;

            var key = (VirtualKey)vkCode;
            var main = _mainBinding;
            if (main != null && MatchesBinding(main, key)) return true;

            var todo = _todoBinding;
            if (todo != null && MatchesBinding(todo, key)) return true;

            return false;
        }

        private static void StopKeyboardHookIfUnused()
        {
            if (_mainBinding != null || _todoBinding != null || IsCapturingHotkey)
            {
                return;
            }

            StopKeyboardHook();
        }

        private static void StopKeyboardHook()
        {
            try
            {
                // 原子获取并清除引用，防止并发调用
                var hook = Interlocked.Exchange(ref _keyboardHook, null);
                if (hook == null) return;

                hook.KeyDown -= KeyboardHook_KeyDown;
                hook.KeyUp -= KeyboardHook_KeyUp;
                hook.Stop(false, true, false);
                _mainPressed = false;
                _todoPressed = false;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "停止键盘 Hook 失败");
            }
        }

        private static void KeyboardHook_KeyDown(object sender, int vkCode)
        {
            if (IsCapturingHotkey) return;

            var key = (VirtualKey)vkCode;
            // 使用局部引用避免竞态：_mainBinding 可能在检查后被另一线程设为 null
            var main = _mainBinding;
            if (main != null && !_mainPressed && MatchesBinding(main, key))
            {
                _mainPressed = true;
                _dispatcherQueue?.TryEnqueue(() => _showAppAction?.Invoke());
            }

            var todo = _todoBinding;
            if (todo != null && !_todoPressed && MatchesBinding(todo, key))
            {
                _todoPressed = true;
                _dispatcherQueue?.TryEnqueue(() => _showToDoAction?.Invoke());
            }
        }

        private static void KeyboardHook_KeyUp(object sender, int vkCode)
        {
            var key = (VirtualKey)vkCode;
            var main = _mainBinding;
            if (main != null && key == main.Key)
            {
                _mainPressed = false;
            }

            var todo = _todoBinding;
            if (todo != null && key == todo.Key)
            {
                _todoPressed = false;
            }
        }

        private static bool MatchesBinding(HotkeyBinding binding, VirtualKey key)
        {
            if (key != binding.Key) return false;
            return ModifierStatesMatch(binding.Modifiers);
        }

        private static bool ModifierStatesMatch(HotkeyModifiers required)
        {
            required = NormalizeModifiers(required);
            return HasModifier(required, HotkeyModifiers.MOD_CONTROL, 0x11, 0xA2, 0xA3)
                && HasModifier(required, HotkeyModifiers.MOD_ALT, 0x12, 0xA4, 0xA5)
                && HasModifier(required, HotkeyModifiers.MOD_SHIFT, 0x10, 0xA0, 0xA1)
                && HasModifier(required, HotkeyModifiers.MOD_WIN, 0x5B, 0x5C);
        }

        private static bool HasModifier(HotkeyModifiers required, HotkeyModifiers flag, params int[] vkCodes)
        {
            bool need = required.HasFlag(flag);
            bool down = false;
            foreach (int vk in vkCodes)
            {
                if (IsKeyDown(vk))
                {
                    down = true;
                    break;
                }
            }

            return need == down;
        }

        private static bool UsesAltHook(HotkeyBinding? binding)
        {
            return binding != null && NormalizeModifiers(binding.Modifiers) == HotkeyModifiers.MOD_ALT;
        }

        public static void NormalizeHotKeyDefaults()
        {
            AppConfig? config = CurrentConfig;
            if (config == null) return;

            RepairHotKeyConfig(
                config.HotkeyStr,
                config.HotkeyModifiers_NoWrite,
                config.Hotkey_NoWrite,
                VirtualKey.W,
                HotkeyModifiers.MOD_ALT,
                "Alt + W",
                treatLegacyCtrlQAsDefault: true,
                (str, mods, key) =>
                {
                    config.HotkeyStr = str;
                    config.HotkeyModifiers = mods;
                    config.Hotkey = key;
                });

            RepairHotKeyConfig(
                config.ToDoHotkeyStr,
                config.ToDoHotkeyModifiers_NoWrite,
                config.ToDoHotkey_NoWrite,
                VirtualKey.Q,
                HotkeyModifiers.MOD_CONTROL | HotkeyModifiers.MOD_SHIFT,
                "Ctrl + Shift + Q",
                treatLegacyCtrlQAsDefault: false,
                (str, mods, key) =>
                {
                    config.ToDoHotkeyStr = str;
                    config.ToDoHotkeyModifiers = mods;
                    config.ToDoHotkey = key;
                });
        }

        private static void RepairHotKeyConfig(
            string? display,
            HotkeyModifiers modifiers,
            VirtualKey key,
            VirtualKey fallbackKey,
            HotkeyModifiers fallbackModifiers,
            string fallbackDisplay,
            bool treatLegacyCtrlQAsDefault,
            Action<string, HotkeyModifiers, VirtualKey> apply)
        {
            bool isLegacyCtrlQ = treatLegacyCtrlQAsDefault
                && string.Equals(display, "Ctrl + Q", StringComparison.OrdinalIgnoreCase)
                && NormalizeModifiers(modifiers) == HotkeyModifiers.MOD_CONTROL
                && key == VirtualKey.Q;

            if (string.IsNullOrWhiteSpace(display) || isLegacyCtrlQ)
            {
                apply(fallbackDisplay, fallbackModifiers, fallbackKey);
                return;
            }

            if (IsValidHotKey(modifiers, key))
            {
                return;
            }

            if (HotkeyInput.TryParseDisplayString(display, out HotkeyModifiers parsedModifiers, out VirtualKey parsedKey))
            {
                apply(display, parsedModifiers, parsedKey);
                return;
            }

            apply(fallbackDisplay, fallbackModifiers, fallbackKey);
        }

        public static bool IsValidHotKey(HotkeyModifiers modifiers, VirtualKey key)
        {
            return key != VirtualKey.None && NormalizeModifiers(modifiers) != HotkeyModifiers.None;
        }

        public static bool IsAltWHotKey(HotkeyModifiers modifiers, VirtualKey key)
        {
            return key == VirtualKey.W && NormalizeModifiers(modifiers) == HotkeyModifiers.MOD_ALT;
        }

        public static bool IsAltOnlyHotKey(HotkeyModifiers modifiers, VirtualKey key)
        {
            return key != VirtualKey.None && NormalizeModifiers(modifiers) == HotkeyModifiers.MOD_ALT;
        }

        public static bool IsSameHotKey(
            HotkeyModifiers firstModifiers,
            VirtualKey firstKey,
            HotkeyModifiers secondModifiers,
            VirtualKey secondKey)
        {
            return firstKey != VirtualKey.None
                && firstKey == secondKey
                && NormalizeModifiers(firstModifiers) == NormalizeModifiers(secondModifiers);
        }

        public static HotkeyModifiers NormalizeModifiers(HotkeyModifiers modifiers)
        {
            return modifiers & ~HotkeyModifiers.MOD_NOREPEAT;
        }

        private static bool IsKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
