param(
    [string[]]$Views = @('Home', 'Settings', 'Todo'),
    [string]$Root = '',
    [int]$TimeoutSeconds = 25,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'lib\rainspace-dev.psm1') -Force

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Get-RainspaceRoot
}

$artifactRoot = Join-Path $Root 'artifacts\ui-state'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class rainspaceUiSmokeNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@

function Get-rainspaceProcess {
    Get-RainspaceProcess
}

function Start-rainspaceIfNeeded {
    $process = Get-RainspaceProcess
    if ($process -or $NoLaunch) {
        return $process
    }

    $launch = Start-RainspaceApp -Root $Root -TimeoutSeconds $TimeoutSeconds
    return $launch.Process
}

function Find-AppWindow([System.Diagnostics.Process]$Process) {
    $rootElement = [System.Windows.Automation.AutomationElement]::RootElement
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $Process.Id
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $window = $rootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children,
            $condition
        )
        if ($window) {
            return $window
        }

        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $deadline)

    return $null
}

function Find-DescendantByName(
    [System.Windows.Automation.AutomationElement]$RootElement,
    [string]$Name,
    [System.Windows.Automation.ControlType]$ControlType = $null
) {
    $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )

    $condition = $nameCondition
    if ($ControlType) {
        $typeCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            $ControlType
        )
        $condition = New-Object System.Windows.Automation.AndCondition($nameCondition, $typeCondition)
    }

    return $RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Invoke-Element([System.Windows.Automation.AutomationElement]$Element) {
    if (-not $Element) {
        return $false
    }

    $pattern = $null
    if ($Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
        $pattern.Invoke()
        Start-Sleep -Milliseconds 500
        return $true
    }

    return $false
}

function Convert-ElementTree(
    [System.Windows.Automation.AutomationElement]$Element,
    [int]$Depth = 0,
    [int]$MaxDepth = 4
) {
    if (-not $Element) {
        return $null
    }

    $node = [ordered]@{
        Name = $Element.Current.Name
        AutomationId = $Element.Current.AutomationId
        ControlType = $Element.Current.ControlType.ProgrammaticName
        IsEnabled = $Element.Current.IsEnabled
        Children = @()
    }

    if ($Depth -ge $MaxDepth) {
        return $node
    }

    $children = $Element.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition
    )

    for ($i = 0; $i -lt $children.Count; $i++) {
        $node.Children += Convert-ElementTree -Element $children.Item($i) -Depth ($Depth + 1) -MaxDepth $MaxDepth
    }

    return $node
}

function Save-WindowScreenshot(
    [System.Windows.Automation.AutomationElement]$Window,
    [System.Diagnostics.Process]$Process,
    [string]$Path
) {
    $nativeHandle = [IntPtr]$Window.Current.NativeWindowHandle
    if ($nativeHandle -eq [IntPtr]::Zero) {
        $nativeHandle = $Process.MainWindowHandle
    }

    if ($nativeHandle -eq [IntPtr]::Zero) {
        return $false
    }

    [rainspaceUiSmokeNative]::SetForegroundWindow($nativeHandle) | Out-Null
    Start-Sleep -Milliseconds 250

    $rect = New-Object rainspaceUiSmokeNative+RECT
    if (-not [rainspaceUiSmokeNative]::GetWindowRect($nativeHandle, [ref]$rect)) {
        return $false
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) {
        return $false
    }

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        return $true
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Invoke-View([System.Windows.Automation.AutomationElement]$Window, [string]$View) {
    $buttonName = switch ($View) {
        'Home' { '首页'; break }
        'Settings' { '设置'; break }
        'Todo' { '待办'; break }
        default { $View; break }
    }

    $button = Find-DescendantByName -RootElement $Window -Name $buttonName -ControlType ([System.Windows.Automation.ControlType]::Button)
    return Invoke-Element -Element $button
}

$process = Start-rainspaceIfNeeded
if (-not $process) {
    throw 'rainspace process was not found and could not be launched.'
}

$window = Find-AppWindow -Process $process
if (-not $window) {
    throw "rainspace window was not found for process id $($process.Id)."
}

$states = New-Object System.Collections.Generic.List[object]

foreach ($view in $Views) {
    $clicked = Invoke-View -Window $window -View $view
    Start-Sleep -Milliseconds 500

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $safeView = ($view -replace '[^A-Za-z0-9_-]', '_')
    $screenshotPath = Join-Path $artifactRoot "$stamp-$safeView.png"
    $screenshotSaved = Save-WindowScreenshot -Window $window -Process $process -Path $screenshotPath

    $tree = Convert-ElementTree -Element $window -MaxDepth 5
    $state = [ordered]@{
        View = $view
        Clicked = $clicked
        ProcessId = $process.Id
        WindowName = $window.Current.Name
        Screenshot = if ($screenshotSaved) { $screenshotPath } else { $null }
        HasHomeButton = [bool](Find-DescendantByName -RootElement $window -Name '首页')
        HasTodoButton = [bool](Find-DescendantByName -RootElement $window -Name '待办')
        HasSettingsButton = [bool](Find-DescendantByName -RootElement $window -Name '设置')
        HasSettingsContent = [bool](Find-DescendantByName -RootElement $window -Name '显示设置')
        HasTodoContent = [bool](Find-DescendantByName -RootElement $window -Name '待办列表')
        Tree = $tree
    }
    $states.Add([pscustomobject]$state) | Out-Null
}

$jsonPath = Join-Path $artifactRoot 'ui-state.json'
$states | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($state in $states) {
    if (-not $state.Clicked) {
        $failures.Add("Failed to click navigation for view '$($state.View)'.") | Out-Null
    }

    switch ($state.View) {
        'Home' {
            if (-not $state.HasHomeButton) { $failures.Add('Home view missing 首页 button.') | Out-Null }
        }
        'Settings' {
            if (-not $state.HasSettingsContent) { $failures.Add('Settings view missing 显示设置 content.') | Out-Null }
        }
        'Todo' {
            if (-not $state.HasTodoContent) { $failures.Add('Todo view missing 待办列表 content.') | Out-Null }
        }
    }
}

Write-Host "UI smoke state written to $jsonPath"
foreach ($state in $states) {
    Write-Host ("{0}: clicked={1}, screenshot={2}" -f $state.View, $state.Clicked, $state.Screenshot)
}

if ($failures.Count -gt 0) {
    Write-Host 'UI smoke verification failed:'
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}
