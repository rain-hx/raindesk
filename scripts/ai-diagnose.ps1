param(
    [string]$Root = '',
    [string]$OutputDir = '',
    [int]$LogTailLines = 260,
    [int]$UiTreeDepth = 4,
    [int]$TimeoutSeconds = 20,
    [switch]$Launch,
    [switch]$NoScreenshot,
    [switch]$RunBuild,
    [switch]$RunVerify,
    [switch]$RunTests,
    [switch]$RequestSnapshot,
    [switch]$PrintJson
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'lib\raindesk-dev.psm1') -Force

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Get-raindeskRoot
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDir = Join-Path $Root "artifacts\ai-diagnostics\$stamp"
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class raindeskAiDiagNative
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

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
'@

function Find-AppWindow([System.Diagnostics.Process]$Process) {
    if (-not $Process) { return $null }

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
        if ($window) { return $window }

        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $deadline)

    return $null
}

function Convert-Rect($Rect) {
    if (-not $Rect) { return $null }
    if ([double]::IsInfinity([double]$Rect.Left) -or
        [double]::IsInfinity([double]$Rect.Top) -or
        [double]::IsInfinity([double]$Rect.Right) -or
        [double]::IsInfinity([double]$Rect.Bottom) -or
        [double]::IsNaN([double]$Rect.Left) -or
        [double]::IsNaN([double]$Rect.Top) -or
        [double]::IsNaN([double]$Rect.Right) -or
        [double]::IsNaN([double]$Rect.Bottom)) {
        return $null
    }

    [ordered]@{
        Left = [int]$Rect.Left
        Top = [int]$Rect.Top
        Right = [int]$Rect.Right
        Bottom = [int]$Rect.Bottom
        Width = [int]($Rect.Right - $Rect.Left)
        Height = [int]($Rect.Bottom - $Rect.Top)
    }
}

function Get-WindowRectInfo([System.Windows.Automation.AutomationElement]$Window, [System.Diagnostics.Process]$Process) {
    if (-not $Window -and -not $Process) { return $null }

    $nativeHandle = [IntPtr]::Zero
    if ($Window) { $nativeHandle = [IntPtr]$Window.Current.NativeWindowHandle }
    if ($nativeHandle -eq [IntPtr]::Zero -and $Process) { $nativeHandle = $Process.MainWindowHandle }
    if ($nativeHandle -eq [IntPtr]::Zero) { return $null }

    $rect = New-Object raindeskAiDiagNative+RECT
    if (-not [raindeskAiDiagNative]::GetWindowRect($nativeHandle, [ref]$rect)) { return $null }

    [ordered]@{
        Hwnd = ("0x{0:X}" -f $nativeHandle.ToInt64())
        IsVisible = [raindeskAiDiagNative]::IsWindowVisible($nativeHandle)
        Rect = Convert-Rect $rect
    }
}

function Convert-ElementTree(
    [System.Windows.Automation.AutomationElement]$Element,
    [int]$Depth = 0,
    [int]$MaxDepth = 4,
    [int]$MaxChildren = 60
) {
    if (-not $Element) { return $null }

    $node = [ordered]@{
        Name = $Element.Current.Name
        AutomationId = $Element.Current.AutomationId
        ControlType = $Element.Current.ControlType.ProgrammaticName
        IsEnabled = $Element.Current.IsEnabled
        BoundingRectangle = Convert-Rect $Element.Current.BoundingRectangle
        Children = @()
    }

    if ($Depth -ge $MaxDepth) { return $node }

    $children = $Element.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition
    )

    $count = [Math]::Min($children.Count, $MaxChildren)
    for ($i = 0; $i -lt $count; $i++) {
        $node.Children += Convert-ElementTree -Element $children.Item($i) -Depth ($Depth + 1) -MaxDepth $MaxDepth -MaxChildren $MaxChildren
    }

    if ($children.Count -gt $MaxChildren) {
        $node.Children += [ordered]@{
            Name = '[truncated]'
            AutomationId = ''
            ControlType = ''
            IsEnabled = $true
            BoundingRectangle = $null
            Children = @()
            OmittedChildren = $children.Count - $MaxChildren
        }
    }

    return $node
}

function Save-WindowScreenshot(
    [System.Windows.Automation.AutomationElement]$Window,
    [System.Diagnostics.Process]$Process,
    [string]$Path
) {
    if ($NoScreenshot) { return $null }
    if (-not $Window -and -not $Process) { return $null }

    $nativeHandle = [IntPtr]::Zero
    if ($Window) { $nativeHandle = [IntPtr]$Window.Current.NativeWindowHandle }
    if ($nativeHandle -eq [IntPtr]::Zero -and $Process) { $nativeHandle = $Process.MainWindowHandle }
    if ($nativeHandle -eq [IntPtr]::Zero) { return $null }

    [raindeskAiDiagNative]::SetForegroundWindow($nativeHandle) | Out-Null
    Start-Sleep -Milliseconds 250

    $rect = New-Object raindeskAiDiagNative+RECT
    if (-not [raindeskAiDiagNative]::GetWindowRect($nativeHandle, [ref]$rect)) { return $null }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) { return $null }

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        return $Path
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-BackupSummary([string]$BakDir) {
    if (-not (Test-Path $BakDir -PathType Container)) {
        return [ordered]@{ Path = $BakDir; Exists = $false; Files = @() }
    }

    $files = Get-ChildItem -LiteralPath $BakDir -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.json', '.bak') } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 20

    [ordered]@{
        Path = (Resolve-Path $BakDir).Path
        Exists = $true
        Files = @($files | ForEach-Object {
            [ordered]@{
                Name = $_.Name
                Length = $_.Length
                LastWriteTime = $_.LastWriteTime.ToString('o')
            }
        })
    }
}

$runtime = Get-raindeskRuntimePaths -Root $Root
$devPaths = Get-raindeskDevPaths -Root $Root
$commands = @()

if ($RunBuild) {
    $commands += Invoke-raindeskDevCommand -Name 'build' -Command 'dotnet build raindesk\raindesk.csproj -c Debug -p:Platform=x64' -WorkingDirectory $Root -OutputPath (Join-Path $OutputDir 'build.txt')
}
if ($RunTests) {
    $commands += Invoke-raindeskDevCommand -Name 'unit-tests' -Command 'dotnet test raindesk.Tests\raindesk.Tests.csproj -c Debug --logger "console;verbosity=minimal"' -WorkingDirectory $Root -OutputPath (Join-Path $OutputDir 'unit-tests.txt')
}
if ($RunVerify) {
    $commands += Invoke-raindeskDevCommand -Name 'verify-icon-rendering' -Command '.\scripts\Verify-IconRenderingFix.ps1' -WorkingDirectory $Root -OutputPath (Join-Path $OutputDir 'verify-icon-rendering.txt')
}

$launchResult = $null
if ($Launch) {
    $launchResult = Start-raindeskApp -Root $Root -TimeoutSeconds $TimeoutSeconds
    if (-not $launchResult.Process) {
        Write-Warning $launchResult.Message
    }
    else {
        Start-Sleep -Seconds 2
    }
}

$process = Get-raindeskProcess
$window = Find-AppWindow -Process $process
$screenshotPath = Save-WindowScreenshot -Window $window -Process $process -Path (Join-Path $OutputDir 'current-window.png')

$runtimeSnapshot = $null
if ($RequestSnapshot) {
    if (-not $process) {
        Write-Warning 'Skipping runtime snapshot because raindesk is not running.'
    }
    else {
        $runtimeSnapshot = Request-raindeskDevSnapshot -Reason 'ai-diagnose' -TimeoutSeconds $TimeoutSeconds
    }
}

$processInfo = $null
if ($process) {
    $processStartTime = $null
    $processResponding = $null
    try { $processStartTime = $process.StartTime.ToString('o') } catch { }
    try { $processResponding = $process.Responding } catch { }

    $processInfo = [ordered]@{
        Name = $process.ProcessName
        Id = $process.Id
        StartTime = $processStartTime
        MainWindowTitle = $process.MainWindowTitle
        MainWindowHandle = ("0x{0:X}" -f $process.MainWindowHandle.ToInt64())
        Responding = $processResponding
        LaunchedByScript = [bool]($launchResult -and $launchResult.Launched)
    }
}

$sourceAssets = Join-Path $Root 'raindesk\Assets'
$logs = Convert-raindeskLogsForJson (Get-raindeskLogSources -Root $Root -TailLines $LogTailLines)
$report = [ordered]@{
    Schema = 'raindesk.ai-diagnostics.v2'
    CreatedAt = (Get-Date).ToString('o')
    Root = $Root
    OutputDir = (Resolve-Path $OutputDir).Path
    Runtime = [ordered]@{
        OutputPath = $runtime.OutputPath
        AppXPath = $runtime.AppXPath
        Manifest = $runtime.Manifest
        Data = Get-raindeskDataSummary -Path $runtime.DataPath
        Backups = Get-BackupSummary -BakDir $runtime.BackupPath
        Assets = Get-raindeskAssetSummary -AssetsPath $runtime.AssetsPath
        SourceAssets = Get-raindeskAssetSummary -AssetsPath $sourceAssets
    }
    Dev = [ordered]@{
        DevDir = $devPaths.DevDir
        RequestPath = $devPaths.RequestPath
        SnapshotPath = $devPaths.SnapshotPath
        PackageLogPaths = $devPaths.PackageLogPaths
    }
    Process = $processInfo
    Window = [ordered]@{
        Found = [bool]$window
        Name = if ($window) { $window.Current.Name } else { $null }
        ClassName = if ($window) { $window.Current.ClassName } else { $null }
        Rect = Get-WindowRectInfo -Window $window -Process $process
        Screenshot = $screenshotPath
        UiTree = if ($window) { Convert-ElementTree -Element $window -MaxDepth $UiTreeDepth } else { $null }
    }
    AppSnapshot = if ($runtimeSnapshot) { $runtimeSnapshot.Snapshot } else { $null }
    Logs = $logs
    Commands = @($commands | ForEach-Object { [ordered]@{ Name = [string]$_.Name; Command = [string]$_.Command; OutputPath = [string]$_.OutputPath; ExitCode = $_.ExitCode; DurationMs = $_.DurationMs } })
}

$logsPath = Join-Path $OutputDir 'logs.json'
[System.IO.File]::WriteAllText($logsPath, ($logs | ConvertTo-Json -Depth 8 -Compress))

$jsonPath = Join-Path $OutputDir 'diagnostics.json'
$jsonText = $report | ConvertTo-Json -Depth 16 -Compress
[System.IO.File]::WriteAllText($jsonPath, $jsonText)

$summaryPath = Join-Path $OutputDir 'summary.txt'
@(
    'raindesk AI diagnostics'
    "CreatedAt: $($report.CreatedAt)"
    "Process: $(if ($process) { "$($process.ProcessName)#$($process.Id)" } else { 'not found' })"
    "WindowFound: $($report.Window.Found)"
    "Screenshot: $($report.Window.Screenshot)"
    "Data: menus=$($report.Runtime.Data.MenuCount), icons=$($report.Runtime.Data.IconCount), selected=$($report.Runtime.Data.SelectedMenuName)"
    "Assets missing: $($report.Runtime.Assets.MissingCount), placeholder-like: $($report.Runtime.Assets.PlaceholderCount)"
    "AppSnapshot: $(if ($runtimeSnapshot -and $runtimeSnapshot.Ok) { $devPaths.SnapshotPath } else { 'not available' })"
    "Logs: $logsPath"
    "Json: $jsonPath"
) | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "AI diagnostics written to $jsonPath"
Write-Host "Summary written to $summaryPath"
if ($screenshotPath) { Write-Host "Screenshot written to $screenshotPath" }

if ($PrintJson) {
    Get-Content -LiteralPath $jsonPath -Raw
}
