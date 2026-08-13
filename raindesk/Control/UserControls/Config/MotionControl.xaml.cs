using raindesk.Constant;
using raindesk.Controls;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Features.Tray.Contracts;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using VirtualKey = Windows.System.VirtualKey;
// Removed 'using static raindesk.Util.GlobalHotKey;' to resolve HotkeyModifiers ambiguity.
// Use raindesk.Util.HotkeyModifiers (from AppConfig.cs) and GlobalHotKey.Dispose/Register directly.

namespace raindesk.Control.UserControls.Config
{
    /// <summary>
    /// 动作设置
    /// </summary>
    public partial class MotionControl : UserControl
    {
        public static bool hotkeyFinished = true; //热键设置结束
        private VirtualKey prevKeyTemp = VirtualKey.None; //上一个按键
        private readonly List<KeyRoutedEventArgs> keysTemp = new List<KeyRoutedEventArgs>();//存储一次快捷键集合
        private readonly AppConfig appConfig;
        private readonly ISaveService saveService;
        private readonly IHotkeyRegistrar hotkeyRegistrar;
        private TextBox? _activeHotkeyBox;
        private HotkeySnapshot? _captureSnapshot;

        private sealed class HotkeySnapshot
        {
            public string Display { get; init; } = "";
            public HotkeyModifiers Modifiers { get; init; }
            public raindesk.Util.VirtualKey Key { get; init; }
        }

        public MotionControl()
        {
            appConfig = AppServices.Get<IAppDataProvider>().Current.AppConfig;
            saveService = AppServices.Get<ISaveService>();
            hotkeyRegistrar = AppServices.Get<IHotkeyRegistrar>();
            this.InitializeComponent();
            this.DataContext = appConfig;
            Unloaded += (_, _) =>
            {
                if (hotkeyRegistrar.IsCapturing)
                {
                    hotkeyRegistrar.ExitCaptureMode();
                }
            };
        }


        /// <summary>
        /// 注册热键按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HotKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox hotkeyBox) return;
            if (!TryParseHotKeyType(hotkeyBox, out HotKeyType hkType)) return;

            lock (this)
            {
                e.Handled = true;

                VirtualKey downKey = e.Key;

                if (prevKeyTemp == VirtualKey.None || prevKeyTemp != downKey)
                {
                    if (hotkeyFinished)
                    {
                        switch (hkType)
                        {
                            case HotKeyType.Main:
                                appConfig.HotkeyStr = "";
                                appConfig.HotkeyModifiers_NoWrite = HotkeyModifiers.None;
                                appConfig.Hotkey_NoWrite = raindesk.Util.VirtualKey.None;
                                break;
                            case HotKeyType.ToDo:
                                appConfig.ToDoHotkeyStr = "";
                                appConfig.ToDoHotkeyModifiers_NoWrite = HotkeyModifiers.None;
                                appConfig.ToDoHotkey_NoWrite = raindesk.Util.VirtualKey.None;
                                break;
                        }
                        hotkeyFinished = false;
                    }

                    //首次按下按键
                    if ((HotKeyType.Main == hkType && (appConfig.HotkeyStr == null || appConfig.HotkeyStr.Length == 0))
                        || (HotKeyType.ToDo == hkType && (appConfig.ToDoHotkeyStr == null || appConfig.ToDoHotkeyStr.Length == 0))
                        )
                    {
                        if (HotkeyInput.IsModifierKey(downKey))
                        {
                            switch (hkType)
                            {
                                case HotKeyType.Main:
                                    appConfig.HotkeyStr = HotkeyInput.GetModifierName(downKey);
                                    appConfig.HotkeyModifiers_NoWrite = HotkeyInput.GetModifier(downKey);
                                    break;
                                case HotKeyType.ToDo:
                                    appConfig.ToDoHotkeyStr = HotkeyInput.GetModifierName(downKey);
                                    appConfig.ToDoHotkeyModifiers_NoWrite = HotkeyInput.GetModifier(downKey);
                                    break;
                            }
                            prevKeyTemp = downKey;
                            keysTemp.Add(e);
                        }
                    }
                    else
                    {
                        if (HotkeyInput.IsModifierKey(prevKeyTemp)
                            && ((downKey >= VirtualKey.A && downKey <= VirtualKey.Z)
                            || (downKey >= VirtualKey.F1 && downKey <= VirtualKey.F12)
                            || (downKey >= VirtualKey.Number0 && downKey <= VirtualKey.Number9)
                            ))
                        {
                            string downKeyStr = downKey.ToString();
                            switch (hkType)
                            {
                                case HotKeyType.Main:
                                    appConfig.HotkeyStr += downKeyStr;
                                    appConfig.Hotkey_NoWrite = ToConfigVirtualKey(downKey);
                                    break;
                                case HotKeyType.ToDo:
                                    appConfig.ToDoHotkeyStr += downKeyStr;
                                    appConfig.ToDoHotkey_NoWrite = ToConfigVirtualKey(downKey);
                                    break;
                            }
                            prevKeyTemp = downKey;
                            keysTemp.Add(e);
                        }
                        else if (HotkeyInput.IsModifierKey(downKey))
                        {
                            switch (hkType)
                            {
                                case HotKeyType.Main:
                                    appConfig.HotkeyStr += HotkeyInput.GetModifierName(downKey);
                                    appConfig.HotkeyModifiers_NoWrite |= HotkeyInput.GetModifier(downKey);
                                    break;
                                case HotKeyType.ToDo:
                                    appConfig.ToDoHotkeyStr += HotkeyInput.GetModifierName(downKey);
                                    appConfig.ToDoHotkeyModifiers_NoWrite |= HotkeyInput.GetModifier(downKey);
                                    break;
                            }

                            prevKeyTemp = downKey;
                            keysTemp.Add(e);
                        }
                    }
                }
            }
        }

        private raindesk.Util.VirtualKey ToConfigVirtualKey(VirtualKey key)
        {
            return (raindesk.Util.VirtualKey)(int)key;
        }

        private bool IsValidHotKey(
            HotkeyModifiers modifiers,
            raindesk.Util.VirtualKey key) =>
            hotkeyRegistrar.IsValid((int)modifiers, (int)key);

        private string GetKeyName(VirtualKey key)
        {
            if (key == VirtualKey.LeftControl || key == VirtualKey.RightControl)
            {
                return "Ctrl + ";
            }
            else if (key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            {
                return "Win + ";
            }
            else if (key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
            {
                return "Shift + ";
            }
            else
            {
                return "Alt + ";
            }
        }

        private HotkeyModifiers GetModifierKeys(VirtualKey key)
        {
            if (key == VirtualKey.LeftControl || key == VirtualKey.RightControl)
            {
                return HotkeyModifiers.MOD_CONTROL;
            }
            else if (key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            {
                return HotkeyModifiers.MOD_WIN;
            }
            else if (key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
            {
                return HotkeyModifiers.MOD_SHIFT;
            }
            else
            {
                return HotkeyModifiers.MOD_ALT;
            }
        }

        private bool CheckModifierKeys(VirtualKey key)
        {
            return key == VirtualKey.LeftControl || key == VirtualKey.RightControl
                || key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows
                || key == VirtualKey.LeftShift || key == VirtualKey.RightShift
                || key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu;
        }


        private void HotKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox hotkeyBox) return;
            if (!TryParseHotKeyType(hotkeyBox, out HotKeyType hkType)) return;

            lock (this)
            {
                e.Handled = true;

                if (!hotkeyFinished)
                {
                    keysTemp.Clear();
                    prevKeyTemp = VirtualKey.None;
                    hotkeyFinished = true;
                    ApplyCapturedHotkey(hkType);
                }
            }
        }

        private void ApplyCapturedHotkey(HotKeyType hkType)
        {
            bool valid = hkType switch
            {
                HotKeyType.Main => IsValidHotKey(appConfig.HotkeyModifiers_NoWrite, appConfig.Hotkey_NoWrite),
                HotKeyType.ToDo => IsValidHotKey(appConfig.ToDoHotkeyModifiers_NoWrite, appConfig.ToDoHotkey_NoWrite),
                _ => false
            };

            if (!valid)
            {
                RestoreCaptureSnapshot(hkType);
                if (hotkeyRegistrar.IsCapturing)
                {
                    hotkeyRegistrar.ExitCaptureMode(false);
                }

                GrowlService.WarningGlobal("请按下完整组合键，例如 Ctrl + Alt + K");
                return;
            }

            if (hkType == HotKeyType.Main)
            {
                appConfig.HotkeyModifiers = appConfig.HotkeyModifiers_NoWrite;
                appConfig.Hotkey = appConfig.Hotkey_NoWrite;
            }
            else
            {
                appConfig.ToDoHotkeyModifiers = appConfig.ToDoHotkeyModifiers_NoWrite;
                appConfig.ToDoHotkey = appConfig.ToDoHotkey_NoWrite;
            }

            if (hotkeyRegistrar.IsCapturing)
            {
                hotkeyRegistrar.ExitCaptureMode(false);
            }
            else
            {
                hotkeyRegistrar.RegisterAll(false);
            }

            HotkeyRegistrationSnapshot status = hotkeyRegistrar.CurrentStatus;
            bool enabled = hkType == HotKeyType.Main
                ? appConfig.EnableAppHotKey == true
                : appConfig.EnableTodoHotKey == true;
            bool registered = hkType == HotKeyType.Main ? status.MainRegistered : status.TodoRegistered;
            string? error = hkType == HotKeyType.Main ? status.MainError : status.TodoError;
            if (enabled && !registered)
            {
                GrowlService.WarningGlobal(error ?? "快捷键注册失败，请更换组合键。");
                return;
            }

            saveService.SaveDebounced();
            string label = hkType == HotKeyType.Main ? "主面板" : "待办任务";
            string hotkey = hkType == HotKeyType.Main ? appConfig.HotkeyStr : appConfig.ToDoHotkeyStr;
            GrowlService.SuccessGlobal(enabled
                ? $"{label}快捷键已生效：{hotkey}"
                : $"{label}快捷键已保存：{hotkey}（开关关闭时不会注册）");
            _captureSnapshot = null;
        }

        private static bool TryParseHotKeyType(TextBox hotkeyBox, out HotKeyType hkType)
        {
            hkType = HotKeyType.Main;
            string? tag = hotkeyBox.Tag?.ToString();
            return !string.IsNullOrEmpty(tag) && Enum.TryParse(tag, out hkType);
        }

        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox hotkeyBox)
            {
                _activeHotkeyBox = hotkeyBox;
            }

            if (TryParseHotKeyType(sender as TextBox ?? _activeHotkeyBox, out HotKeyType hkType))
            {
                _captureSnapshot = CreateSnapshot(hkType);
            }

            hotkeyFinished = true;
            prevKeyTemp = VirtualKey.None;
            keysTemp.Clear();
            hotkeyRegistrar.EnterCaptureMode();
        }

        private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot);
            if (ReferenceEquals(focused, MainHotkeyBox))
            {
                return;
            }

            if (sender is TextBox hotkeyBox && TryParseHotKeyType(hotkeyBox, out HotKeyType hkType))
            {
                bool valid = hkType switch
                {
                    HotKeyType.Main => IsValidHotKey(appConfig.HotkeyModifiers_NoWrite, appConfig.Hotkey_NoWrite),
                    HotKeyType.ToDo => IsValidHotKey(appConfig.ToDoHotkeyModifiers_NoWrite, appConfig.ToDoHotkey_NoWrite),
                    _ => false
                };

                if (!valid)
                {
                    RestoreCaptureSnapshot(hkType);
                }
                else if (hkType == HotKeyType.Main)
                {
                    appConfig.HotkeyModifiers = appConfig.HotkeyModifiers_NoWrite;
                    appConfig.Hotkey = appConfig.Hotkey_NoWrite;
                }
                else
                {
                    appConfig.ToDoHotkeyModifiers = appConfig.ToDoHotkeyModifiers_NoWrite;
                    appConfig.ToDoHotkey = appConfig.ToDoHotkey_NoWrite;
                }
            }

            _activeHotkeyBox = null;
            _captureSnapshot = null;
            hotkeyFinished = true;
            prevKeyTemp = VirtualKey.None;
            keysTemp.Clear();
            hotkeyRegistrar.ExitCaptureMode();
        }

        private HotkeySnapshot CreateSnapshot(HotKeyType hkType)
        {
            return hkType switch
            {
                HotKeyType.Main => new HotkeySnapshot
                {
                    Display = appConfig.HotkeyStr ?? "",
                    Modifiers = appConfig.HotkeyModifiers_NoWrite,
                    Key = appConfig.Hotkey_NoWrite,
                },
                _ => new HotkeySnapshot
                {
                    Display = appConfig.ToDoHotkeyStr ?? "",
                    Modifiers = appConfig.ToDoHotkeyModifiers_NoWrite,
                    Key = appConfig.ToDoHotkey_NoWrite,
                },
            };
        }

        private void RestoreCaptureSnapshot(HotKeyType hkType)
        {
            if (_captureSnapshot == null)
            {
                return;
            }

            if (hkType == HotKeyType.Main)
            {
                appConfig.HotkeyStr = _captureSnapshot.Display;
                appConfig.HotkeyModifiers = _captureSnapshot.Modifiers;
                appConfig.Hotkey = _captureSnapshot.Key;
            }
            else
            {
                appConfig.ToDoHotkeyStr = _captureSnapshot.Display;
                appConfig.ToDoHotkeyModifiers = _captureSnapshot.Modifiers;
                appConfig.ToDoHotkey = _captureSnapshot.Key;
            }
        }

        private void HotkeyBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is TextBox hotkeyBox)
            {
                hotkeyBox.Focus(FocusState.Pointer);
            }
        }

        /// <summary>
        /// 启用热键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableHotKey_Click(object sender, RoutedEventArgs e)
        {
            if (hotkeyRegistrar.IsCapturing)
            {
                return;
            }

            hotkeyRegistrar.RegisterAll(false);
        }
    }
}
