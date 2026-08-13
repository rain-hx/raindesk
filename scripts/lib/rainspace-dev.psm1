Set-StrictMode -Version Latest

$script:RainspaceDevModuleRoot = $PSScriptRoot

function Get-RainspaceRoot {
    param([string]$Start = $script:RainspaceDevModuleRoot)
    return (Resolve-Path (Join-Path $Start '..\..')).Path
}

function Get-RainspaceProjectPath {
    param([string]$Root = (Get-RainspaceRoot))
    return Join-Path $Root 'rainspace\rainspace.csproj'
}

function Get-RainspaceRuntimePaths {
    param(
        [string]$Root = (Get-RainspaceRoot),
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug',
        [ValidateSet('x64', 'x86', 'ARM64')]
        [string]$Platform = 'x64'
    )

    $output = Join-Path $Root "rainspace\bin\$Platform\$Configuration\net8.0-windows10.0.26100.0\win-$Platform"
    $appx = Join-Path $output 'AppX'

    [ordered]@{
        Root = $Root
        Configuration = $Configuration
        Platform = $Platform
        OutputPath = $output
        AppXPath = $appx
        Manifest = Join-Path $output 'AppxManifest.xml'
        ExePath = Join-Path $output 'rainspace.exe'
        DataPath = Join-Path $appx 'Data'
        BackupPath = Join-Path $appx 'bak'
        AssetsPath = Join-Path $appx 'Assets'
        RuntimeLogsPath = Join-Path $appx 'logs'
    }
}

function Get-RainspaceDevPaths {
    param([string]$Root = (Get-RainspaceRoot))

    $localDev = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'rainspace\dev'
    $localLogs = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'rainspace\logs'
    $runtime = Get-RainspaceRuntimePaths -Root $Root

    [ordered]@{
        DevDir = $localDev
        RequestPath = Join-Path $localDev 'request.json'
        SnapshotPath = Join-Path $localDev 'snapshot.json'
        LocalLogsPath = $localLogs
        RuntimeLogsPath = $runtime.RuntimeLogsPath
        PackageLogPaths = @(Get-RainspacePackageLogPaths)
    }
}

function Get-RainspacePackageLogPaths {
    $packagesRoot = Join-Path $env:LOCALAPPDATA 'Packages'
    if (-not (Test-Path $packagesRoot -PathType Container)) {
        return @()
    }

    return @(
        Get-ChildItem -LiteralPath $packagesRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like 'E66FE79E*' -or $_.Name -like '*rainspace*' } |
            ForEach-Object {
                @(
                    Join-Path $_.FullName 'LocalCache\Local\rainspace\logs'
                    Join-Path $_.FullName 'LocalState\rainspace\logs'
                )
            } |
            ForEach-Object { $_ } |
            Where-Object { Test-Path $_ -PathType Container }
    )
}

function Get-RainspaceWinAppPath {
    $candidates = @(
        Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp') -Recurse -Filter 'winapp.exe' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\tools\\win-x64\\winapp\.exe$' } |
            Sort-Object FullName -Descending
    )

    if ($candidates.Count -gt 0) {
        return $candidates[0].FullName
    }

    return $null
}

function Get-RainspaceProcess {
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessName -eq 'rainspace' } |
        Sort-Object StartTime -Descending |
        Select-Object -First 1
}

function Start-RainspaceApp {
    param(
        [string]$Root = (Get-RainspaceRoot),
        [switch]$BuildFirst,
        [int]$TimeoutSeconds = 25,
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug',
        [ValidateSet('x64', 'x86', 'ARM64')]
        [string]$Platform = 'x64'
    )

    $process = Get-RainspaceProcess
    if ($process) {
        return [ordered]@{
            Process = $process
            Launched = $false
            Message = 'Already running'
        }
    }

    if ($BuildFirst) {
        $project = Get-RainspaceProjectPath -Root $Root
        & dotnet build $project -c $Configuration -p:Platform=$Platform | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE"
        }
    }

    $runtime = Get-RainspaceRuntimePaths -Root $Root -Configuration $Configuration -Platform $Platform
    $winapp = Get-RainspaceWinAppPath

    if ($winapp -and (Test-Path $runtime.Manifest -PathType Leaf)) {
        Start-Process -FilePath $winapp `
            -ArgumentList @(
                'run',
                $runtime.OutputPath,
                '--manifest', $runtime.Manifest,
                '--output-appx-directory', $runtime.AppXPath,
                '--caller', 'rainspace-dev'
            ) `
            -WorkingDirectory $Root `
            -WindowStyle Hidden | Out-Null
    }
    else {
        Start-Process -FilePath 'dotnet' `
            -ArgumentList @(
                'msbuild',
                (Get-RainspaceProjectPath -Root $Root),
                '-t:RunPackagedApp',
                "-p:Configuration=$Configuration",
                "-p:Platform=$Platform"
            ) `
            -WorkingDirectory $Root `
            -WindowStyle Hidden | Out-Null
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 500
        $process = Get-RainspaceProcess
        if ($process) {
            return [ordered]@{
                Process = $process
                Launched = $true
                Message = 'Started'
            }
        }
    } while ((Get-Date) -lt $deadline)

    return [ordered]@{
        Process = $null
        Launched = $false
        Message = "Process not found within ${TimeoutSeconds}s"
    }
}

function Get-RainspaceLogSources {
    param(
        [string]$Root = (Get-RainspaceRoot),
        [int]$TailLines = 260
    )

    $runtime = Get-RainspaceRuntimePaths -Root $Root
    $dev = Get-RainspaceDevPaths -Root $Root
    $sources = [ordered]@{}
    $paths = [ordered]@{
        LocalLog = Join-Path $dev.LocalLogsPath 'log.log'
        LocalError = Join-Path $dev.LocalLogsPath 'error.log'
        RuntimeLog = Join-Path $runtime.RuntimeLogsPath 'log.log'
        RuntimeError = Join-Path $runtime.RuntimeLogsPath 'error.log'
    }

    foreach ($entry in $paths.GetEnumerator()) {
        $sources[$entry.Key] = Read-RainspaceTextTail -Path $entry.Value -Lines $TailLines
    }

    $index = 0
    foreach ($packageLog in $dev.PackageLogPaths) {
        $index++
        $sources["Package${index}Log"] = Read-RainspaceTextTail -Path (Join-Path $packageLog 'log.log') -Lines $TailLines
        $sources["Package${index}Error"] = Read-RainspaceTextTail -Path (Join-Path $packageLog 'error.log') -Lines $TailLines
    }

    return $sources
}

function Read-RainspaceTextTail {
    param(
        [string]$Path,
        [int]$Lines = 260
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        return [ordered]@{
            Path = $Path
            Exists = $false
            Length = 0
            LastWriteTime = $null
            Tail = @()
        }
    }

    $item = Get-Item -LiteralPath $Path
    [ordered]@{
        Path = $item.FullName
        Exists = $true
        Length = $item.Length
        LastWriteTime = $item.LastWriteTime.ToString('o')
        Tail = @(Get-Content -LiteralPath $Path -Tail $Lines -ErrorAction SilentlyContinue)
    }
}

function Get-RainspaceDataSummary {
    param([string]$Path)

    if (-not (Test-Path $Path -PathType Leaf)) {
        return [ordered]@{ Path = $Path; Exists = $false }
    }

    try {
        $item = Get-Item -LiteralPath $Path
        $data = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
        $menus = @($data.MenuList)
        $iconCount = ($menus | ForEach-Object { @($_.IconList).Count } | Measure-Object -Sum).Sum
        $idx = [int]$data.AppConfig.SelectedMenuIndex
        $selected = if ($idx -ge 0 -and $idx -lt $menus.Count) { $menus[$idx] } else { $null }
        $selectedName = if ($idx -eq -1) { '全部' } elseif ($selected) { $selected.MenuName } else { $null }
        $selectedIconCount = if ($idx -eq -1) { [int]$iconCount } elseif ($selected) { @($selected.IconList).Count } else { 0 }

        [ordered]@{
            Path = $item.FullName
            Exists = $true
            Length = $item.Length
            LastWriteTime = $item.LastWriteTime.ToString('o')
            MenuCount = $menus.Count
            IconCount = [int]$iconCount
            SelectedMenuIndex = $idx
            SelectedMenuName = $selectedName
            SelectedMenuIconCount = $selectedIconCount
            Menus = @($menus | Select-Object -First 30 MenuName, @{ n = 'IconCount'; e = { @($_.IconList).Count } })
        }
    }
    catch {
        [ordered]@{
            Path = $Path
            Exists = $true
            Error = $_.Exception.Message
        }
    }
}

function Get-RainspaceAssetSummary {
    param([string]$AssetsPath)

    $required = @(
        'AppIcon.ico',
        'rainspaceLogo.png',
        'StoreLogo.png',
        'Square44x44Logo.png',
        'Square150x150Logo.png',
        'SplashScreen.png',
        'Wide310x150Logo.png',
        'Square44x44Logo.scale-200.png',
        'Square150x150Logo.scale-200.png'
    )

    $files = @()
    foreach ($name in $required) {
        $path = Join-Path $AssetsPath $name
        if (Test-Path $path -PathType Leaf) {
            $item = Get-Item -LiteralPath $path
            $files += [ordered]@{
                Name = $name
                Exists = $true
                Length = $item.Length
                LastWriteTime = $item.LastWriteTime.ToString('o')
                PlaceholderLikely = ($name -like '*.png' -and $item.Length -lt 2048)
            }
        }
        else {
            $files += [ordered]@{
                Name = $name
                Exists = $false
                Length = 0
                PlaceholderLikely = $true
            }
        }
    }

    [ordered]@{
        AssetsPath = $AssetsPath
        Exists = Test-Path $AssetsPath -PathType Container
        Files = $files
        MissingCount = @($files | Where-Object { -not $_.Exists }).Count
        PlaceholderCount = @($files | Where-Object { $_.PlaceholderLikely }).Count
    }
}

function Request-RainspaceDevSnapshot {
    param(
        [string]$Reason = 'manual',
        [int]$TimeoutSeconds = 8
    )

    $dev = Get-RainspaceDevPaths
    New-Item -ItemType Directory -Force -Path $dev.DevDir | Out-Null

    $request = [ordered]@{
        RequestId = [guid]::NewGuid().ToString('N')
        Reason = $Reason
        RequestedAt = (Get-Date).ToString('o')
    }

    $request | ConvertTo-Json | Set-Content -LiteralPath $dev.RequestPath -Encoding UTF8

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ((Test-Path $dev.SnapshotPath -PathType Leaf)) {
            $snapshot = Get-Content -LiteralPath $dev.SnapshotPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($snapshot.RequestId -eq $request.RequestId) {
                return [ordered]@{
                    Ok = $true
                    Request = $request
                    SnapshotPath = $dev.SnapshotPath
                    Snapshot = $snapshot
                }
            }
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    return [ordered]@{
        Ok = $false
        Request = $request
        SnapshotPath = $dev.SnapshotPath
        Message = "No matching snapshot within ${TimeoutSeconds}s. Is rainspace running?"
    }
}

function Invoke-RainspaceDevCommand {
    param(
        [string]$Name,
        [string]$Command,
        [string]$WorkingDirectory,
        [string]$OutputPath
    )

    $started = Get-Date
    $result = [ordered]@{
        Name = $Name
        Command = $Command
        OutputPath = $OutputPath
        StartedAt = $started.ToString('o')
        ExitCode = $null
        DurationMs = $null
    }

    $oldLocation = Get-Location
    try {
        Set-Location $WorkingDirectory
        $output = Invoke-Expression $Command 2>&1
        $result.ExitCode = if ($null -ne $LASTEXITCODE) { $LASTEXITCODE } else { 0 }
        $output | Out-String | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    }
    catch {
        $result.ExitCode = 1
        $_ | Out-String | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    }
    finally {
        Set-Location $oldLocation
        $result.DurationMs = [int]((Get-Date) - $started).TotalMilliseconds
    }

    return $result
}

function Convert-RainspaceLogsForJson {
    param($Logs)

    $plain = [ordered]@{}
    foreach ($key in $Logs.Keys) {
        $entry = $Logs[$key]
        $plain[$key] = [ordered]@{
            Path = [string]$entry.Path
            Exists = [bool]$entry.Exists
            Length = [int]$entry.Length
            LastWriteTime = if ($entry.LastWriteTime) { [string]$entry.LastWriteTime } else { $null }
            Tail = @($entry.Tail | ForEach-Object { [string]$_ })
        }
    }

    return $plain
}

Export-ModuleMember -Function @(
    'Get-RainspaceRoot',
    'Get-RainspaceProjectPath',
    'Get-RainspaceRuntimePaths',
    'Get-RainspaceDevPaths',
    'Get-RainspacePackageLogPaths',
    'Get-RainspaceWinAppPath',
    'Get-RainspaceProcess',
    'Start-RainspaceApp',
    'Get-RainspaceLogSources',
    'Read-RainspaceTextTail',
    'Get-RainspaceDataSummary',
    'Get-RainspaceAssetSummary',
    'Convert-RainspaceLogsForJson',
    'Request-RainspaceDevSnapshot',
    'Invoke-RainspaceDevCommand'
)
