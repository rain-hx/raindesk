$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'raindesk'
$failed = @()

function Assert-Pattern {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Pattern,
        [switch]$MustNotMatch
    )

    $content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $matched = $content -match $Pattern
    if ($MustNotMatch) {
        if ($matched) { $script:failed += $Name }
    }
    elseif (-not $matched) {
        $script:failed += $Name
    }
}

Assert-Pattern 'package display name uses raindesk' (Join-Path $project 'Package.appxmanifest') '<DisplayName>raindesk</DisplayName>'
Assert-Pattern 'visual elements use raindesk' (Join-Path $project 'Package.appxmanifest') 'DisplayName="raindesk"'
Assert-Pattern 'main window shows raindesk brand' (Join-Path $project 'MainWindow.xaml') 'raindesk'
Assert-Pattern 'tray icon is created instead of skipped' (Join-Path $project 'MainWindow.xaml.cs') 'new TrayIcon\('
Assert-Pattern 'tray disabled diagnostic is removed' (Join-Path $project 'MainWindow.xaml.cs') 'TrayIcon: Skipped' -MustNotMatch
Assert-Pattern 'modern colorful tile style exists' (Join-Path $project 'Resource\Dictionary\CommonStyle.xaml') 'raindeskIconTileStyle'
Assert-Pattern 'modern colorful sidebar style exists' (Join-Path $project 'Resource\Dictionary\CommonStyle.xaml') 'raindeskSidebarItemStyle'
$myShortcutsText = -join ([char]0x6211, [char]0x7684, [char]0x5FEB, [char]0x6377)
$oldFeatureText = -join ([char]0x5FEB, [char]0x901F, [char]0x542F, [char]0x52A8)
$addShortcutText = -join ([char]0x6DFB, [char]0x52A0, [char]0x5FEB, [char]0x6377, [char]0x65B9, [char]0x5F0F)
$tipTitleText = -join ([char]0x63D0, [char]0x793A)
$rightClickTipText = -join ([char]0x53F3, [char]0x952E, [char]0x7A7A, [char]0x767D, [char]0x533A, [char]0x57DF)

Assert-Pattern 'main workspace title uses my shortcuts' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') $myShortcutsText
Assert-Pattern 'right panel removes old feature banner copy' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') $oldFeatureText -MustNotMatch
Assert-Pattern 'shortcut tiles use compact card size' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Width=.{1}96.{1}[\s\S]*Height=.{1}108'
Assert-Pattern 'shortcut tile has more actions affordance' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'E712'
Assert-Pattern 'add shortcut card is present' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') $addShortcutText
Assert-Pattern 'add shortcut card uses dashed border' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'StrokeDashArray=.{1}4,4'
Assert-Pattern 'sidebar uses light reference spacing' (Join-Path $project 'MainWindow.xaml') 'Width=.{1}220'
Assert-Pattern 'sidebar tip region is removed' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') $tipTitleText -MustNotMatch
Assert-Pattern 'sidebar right-click helper text is removed' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') $rightClickTipText -MustNotMatch
Assert-Pattern 'sidebar uses tighter padding' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'Padding=.{1}18,16,16,12'
Assert-Pattern 'sidebar item content stretches instead of centering' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'HorizontalContentAlignment.{1,80}Stretch'
Assert-Pattern 'sidebar selected unfocused state keeps selected effect' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'SelectedUnfocused'
Assert-Pattern 'sidebar pointer over selected state keeps selected effect' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'PointerOverSelected'
Assert-Pattern 'sidebar scrollbar is hidden' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'ScrollViewer.VerticalScrollBarVisibility="Hidden"'
Assert-Pattern 'sidebar selected effect is data driven' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'IsSelected_NoWrite'
Assert-Pattern 'menu model exposes non-persistent selected state' (Join-Path $project 'ViewModel\MenuInfo.cs') 'IsSelected_NoWrite'
Assert-Pattern 'menu selection refresh updates selected state' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml.cs') 'RefreshMenuSelectionState'
Assert-Pattern 'old pointer background event wiring is removed' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml.cs') 'SetMenuListBoxItemEvent' -MustNotMatch
Assert-Pattern 'sidebar selected style removes right dot' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'SelectedDot' -MustNotMatch
Assert-Pattern 'sidebar selected style removes left accent' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'SelectedAccent' -MustNotMatch
Assert-Pattern 'sidebar selected style uses compact pill background' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') '#EEF5FF'
Assert-Pattern 'sidebar menu rows are compact' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'MinHeight=.{1}28'
Assert-Pattern 'sidebar selected text turns blue' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'SelectedTextBlock'
Assert-Pattern 'sidebar has divider between counters and menu' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'x:Name="MenuDivider"'
Assert-Pattern 'sidebar divider uses thin line' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'Height=.{1}1'
Assert-Pattern 'sidebar divider uses subtle color' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'Background=.{1}\{ThemeResource DividerBrush\}'
Assert-Pattern 'sidebar rows are extra compact' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'MinHeight=.{1}28'
Assert-Pattern 'sidebar item margin is zero' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'Property=.{1}Margin.{1}\s+Value=.{1}0'
Assert-Pattern 'each sidebar item has a divider line' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'x:Name="MenuItemDivider"'
Assert-Pattern 'sidebar item divider is thin' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'x:Name="MenuItemDivider"[\s\S]*Height=.{1}1'
Assert-Pattern 'sidebar item divider uses subtle color' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'x:Name="MenuItemDivider"[\s\S]*Background=.{1}\{ThemeResource DividerBrush\}'
Assert-Pattern 'sidebar menu item visual width is balanced' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'Width=.{1}164'
Assert-Pattern 'sidebar menu item visual is centered' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'HorizontalAlignment=.{1}Center'
Assert-Pattern 'sidebar item divider has balanced inset' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'x:Name="MenuItemDivider"[\s\S]*Margin=.{1}14,0,14,0'
Assert-Pattern 'sidebar menu text is readable' (Join-Path $project 'Control\UserControls\PannelCard\LeftCardControl.xaml') 'FontSize=.{1}14'
Assert-Pattern 'shortcut tile item margin is compact' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Property=.{1}Margin.{1}\s+Value=.{1}7,8'
Assert-Pattern 'shortcut tile text max width is compact' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'MaxWidth=.{1}82'
Assert-Pattern 'add shortcut card matches compact width' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Width=.{1}96.{1}[\s\S]*IsAddShortcut_NoWrite'
Assert-Pattern 'shortcut tile icon background is larger' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Border Width=.{1}44.{1}[\s\S]*Height=.{1}44'
Assert-Pattern 'shortcut tile icon image is larger' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Image Source=.{1}\{Binding BitmapImage\}.{1}[\s\S]*Width=.{1}28[\s\S]*Height=.{1}28'
Assert-Pattern 'shortcut list wrapper is a layout panel' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') '<Grid x:Name="WrapUFG"'
Assert-Pattern 'shortcut list is not wrapped by ItemsControl' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') '<ItemsControl x:Name="WrapUFG"' -MustNotMatch
Assert-Pattern 'shortcut list keeps vertical scrollbar hidden' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'ScrollViewer.VerticalScrollBarVisibility="Hidden"'
Assert-Pattern 'add shortcut is no longer a list footer' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') '<ListView.Footer>' -MustNotMatch
Assert-Pattern 'shortcut list binds to combined tiles' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'ItemsSource="\{Binding ShortcutTiles_NoWrite, ElementName=RootControl'
Assert-Pattern 'shortcut list is assigned in code for reliable runtime binding' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml.cs') 'IconListView\.ItemsSource\s*=\s*shortcutTiles'
Assert-Pattern 'add shortcut tile uses data flag' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml') 'IsAddShortcut_NoWrite'
Assert-Pattern 'icon model exposes add shortcut flag' (Join-Path $project 'ViewModel\IconInfo.cs') 'IsAddShortcut_NoWrite'
Assert-Pattern 'right card builds combined shortcut tiles' (Join-Path $project 'Control\UserControls\PannelCard\RightCardControl.xaml.cs') 'RebuildShortcutTiles'
Assert-Pattern 'tray right click uses native menu' (Join-Path $project 'MainWindow.xaml.cs') 'TrackPopupMenu'
Assert-Pattern 'tray right click no longer uses page flyout' (Join-Path $project 'MainWindow.xaml.cs') 'new MenuFlyout\(' -MustNotMatch
Assert-Pattern 'top search uses stretch alignment' (Join-Path $project 'MainWindow.xaml') 'TextBox x:Name="SearchBox"[\s\S]*HorizontalAlignment="Stretch"'
Assert-Pattern 'top search has minimum width' (Join-Path $project 'MainWindow.xaml') 'SearchBoxHost"[\s\S]*MinWidth="260"'
Assert-Pattern 'top search has maximum width' (Join-Path $project 'MainWindow.xaml') 'SearchBoxHost"[\s\S]*MaxWidth="760"'
Assert-Pattern 'top search no longer uses fixed width' (Join-Path $project 'MainWindow.xaml') 'Width="540"' -MustNotMatch
Assert-Pattern 'top actions column uses auto width' (Join-Path $project 'MainWindow.xaml') '<ColumnDefinition Width="Auto"/>'
Assert-Pattern 'top right keeps settings button' (Join-Path $project 'MainWindow.xaml') 'x:Name="SettingButton"'
Assert-Pattern 'settings icon uses fluent icon font' (Join-Path $project 'MainWindow.xaml') 'FontIcon[\s\S]*Glyph="&#xE713;"[\s\S]*FontFamily="Segoe Fluent Icons"'
Assert-Pattern 'top right close icon is removed' (Join-Path $project 'MainWindow.xaml') 'Content="&#xE8BB;"' -MustNotMatch
Assert-Pattern 'tray menu no longer moves window near cursor' (Join-Path $project 'MainWindow.xaml.cs') 'screenPos\.X - 100' -MustNotMatch
Assert-Pattern 'tray menu no longer restores with hardcoded size' (Join-Path $project 'MainWindow.xaml.cs') 'GetSystemMetrics\(0\) - 850' -MustNotMatch
Assert-Pattern 'tray menu no longer forces visible window before flyout' (Join-Path $project 'MainWindow.xaml.cs') '确保 XamlRoot 有效' -MustNotMatch
Assert-Pattern 'main window registers hotkey on startup' (Join-Path $project 'MainWindow.xaml.cs') 'SetupTrayIcon\(\);[\s\S]*RegisterHotKey\(false\);'
Assert-Pattern 'alt w uses non-consuming keyboard hook' (Join-Path $project 'MainWindow.xaml.cs') 'Alt\+W 使用键盘 Hook 旁路监听'
Assert-Pattern 'alt w does not register a system hotkey in main window' (Join-Path $project 'MainWindow.xaml.cs') 'GlobalHotKey\.RegisterHotKey' -MustNotMatch
Assert-Pattern 'alt w key repeat is latched until key up' (Join-Path $project 'MainWindow.xaml.cs') 'altWPressed[\s\S]*AltWKeyboardHook_KeyUp'
Assert-Pattern 'alt w suppresses only matching keydown' (Join-Path $project 'MainWindow.xaml.cs') 'ShouldSuppressAltWKeyDown'
Assert-Pattern 'keyboard hook supports selective suppression' (Join-Path $project 'Util\UserActivityHook.cs') 'ShouldSuppressKeyDown'
Assert-Pattern 'alt w keyboard hook checks global alt state' (Join-Path $project 'MainWindow.xaml.cs') 'GetAsyncKeyState'
Assert-Pattern 'show app makes window topmost' (Join-Path $project 'MainWindow.xaml.cs') 'SetWindowPos\(hwnd,\s*HWND_TOPMOST'
Assert-Pattern 'show app releases permanent topmost' (Join-Path $project 'MainWindow.xaml.cs') 'SetWindowPos\(hwnd,\s*HWND_NOTOPMOST'
Assert-Pattern 'topmost stays active until focus changes' (Join-Path $project 'MainWindow.xaml.cs') '_isTopMostActive\s*=\s*true'
Assert-Pattern 'topmost opening guard protects reopen sequence' (Join-Path $project 'MainWindow.xaml.cs') '_isTopMostOpening'
Assert-Pattern 'topmost is reconfirmed after open' (Join-Path $project 'MainWindow.xaml.cs') 'ConfirmTopMostAfterOpen'
Assert-Pattern 'topmost reopen session ignores stale confirmations' (Join-Path $project 'MainWindow.xaml.cs') '_topMostSessionId'
Assert-Pattern 'show app forcefully reapplies topmost' (Join-Path $project 'MainWindow.xaml.cs') 'ForceTopMost'
Assert-Pattern 'topmost foreground session is confirmed before blur release' (Join-Path $project 'MainWindow.xaml.cs') '_topMostSawForeground'
Assert-Pattern 'foreground watcher releases topmost on blur' (Join-Path $project 'MainWindow.xaml.cs') '_topMostSawForeground[\s\S]*ReleaseTopMost\(hwnd,\s*false\)'
Assert-Pattern 'foreground child windows are treated as app foreground' (Join-Path $project 'MainWindow.xaml.cs') 'GetAncestor\(foreground,\s*GA_ROOT\)\s*==\s*hwnd'
Assert-Pattern 'foreground watcher detects outside mouse click' (Join-Path $project 'MainWindow.xaml.cs') 'IsLeftMouseDownOutside'
Assert-Pattern 'topmost mouse watcher starts while window is topmost' (Join-Path $project 'MainWindow.xaml.cs') 'StartTopMostMouseWatch'
Assert-Pattern 'clicking outside releases topmost' (Join-Path $project 'MainWindow.xaml.cs') 'TopMostMouseHook_OnMouseLeftDown[\s\S]*ReleaseTopMost\(hwnd,\s*true\)'
Assert-Pattern 'topmost is released when deactivated' (Join-Path $project 'MainWindow.xaml.cs') 'WindowActivationState\.Deactivated[\s\S]*ReleaseTopMost'
Assert-Pattern 'topmost is released from win32 deactivate messages' (Join-Path $project 'MainWindow.xaml.cs') 'WM_ACTIVATEAPP[\s\S]*WM_KILLFOCUS[\s\S]*ReleaseTopMost\(hwnd,\s*false\)'
Assert-Pattern 'topmost release preserves owner z-order' (Join-Path $project 'MainWindow.xaml.cs') 'SWP_NOOWNERZORDER'
Assert-Pattern 'show app plays scale animation' (Join-Path $project 'MainWindow.xaml.cs') 'PlayShowAnimation'
Assert-Pattern 'show animation uses back ease' (Join-Path $project 'MainWindow.xaml.cs') 'BackEase'
Assert-Pattern 'main window listens for minimize messages' (Join-Path $project 'MainWindow.xaml.cs') 'WM_SYSCOMMAND[\s\S]*SC_MINIMIZE'
Assert-Pattern 'minimize redirects to tray' (Join-Path $project 'MainWindow.xaml.cs') 'MainWindow minimize redirected to tray'

if ($failed.Count -gt 0) {
    Write-Host 'raindesk UI verification failed:'
    foreach ($name in $failed) {
        Write-Host " - $name"
    }
    exit 1
}

Write-Host 'raindesk UI verification passed.'
