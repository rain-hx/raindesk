param(
    [Parameter(Position = 0)]
    [ValidateSet('loop', 'diagnose', 'test', 'smoke', 'verify', 'snapshot', 'build')]
    [string]$Command = 'loop',

    [switch]$Launch,
    [switch]$BuildFirst,
    [switch]$RunTests,
    [switch]$RunSmoke,
    [switch]$RunVerify,
    [switch]$RunBuild,
    [switch]$PrintJson,
    [switch]$NoScreenshot,
    [int]$TimeoutSeconds = 25
)

$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'lib\rainspace-dev.psm1'
Import-Module $modulePath -Force

$Root = Get-RainspaceRoot -Start (Join-Path $PSScriptRoot 'lib')
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$artifactRoot = Join-Path $Root "artifacts\dev-loop\$stamp"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

function Write-DevStep([string]$Message) {
    Write-Host "[dev] $Message"
}

function Invoke-DevBuild {
    $project = Get-RainspaceProjectPath -Root $Root
    Write-DevStep "Building $project"
    & dotnet build $project -c Debug -p:Platform=x64
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DevTests {
    $testProject = Join-Path $Root 'rainspace.Tests\rainspace.Tests.csproj'
    $output = Join-Path $artifactRoot 'unit-tests.txt'
    Write-DevStep "Running unit tests"
    & dotnet test $testProject -c Debug -p:Platform=x64 --logger "console;verbosity=minimal" 2>&1 |
        Tee-Object -FilePath $output |
        Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Unit tests failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DevVerify {
    $checks = @(
        @{ Name = 'verify-icon-rendering'; Script = 'Verify-IconRenderingFix.ps1' },
        @{ Name = 'verify-settings'; Script = 'verify-settings.ps1' }
    )

    foreach ($check in $checks) {
        $output = Join-Path $artifactRoot "$($check.Name).txt"
        Write-DevStep "Running $($check.Name)"
        & (Join-Path $Root "scripts\$($check.Script)") 2>&1 |
            Tee-Object -FilePath $output |
            Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "$($check.Name) failed with exit code $LASTEXITCODE"
        }
    }

    $runtime = Get-RainspaceRuntimePaths -Root $Root
    $assets = Get-RainspaceAssetSummary -AssetsPath (Join-Path $Root 'rainspace\Assets')
    if ($assets.MissingCount -gt 0 -or $assets.PlaceholderCount -gt 0) {
        throw "App assets missing=$($assets.MissingCount), placeholder-like=$($assets.PlaceholderCount)"
    }

    Write-DevStep 'Asset checks passed'
}

function Invoke-DevSmoke {
    $output = Join-Path $artifactRoot 'ui-smoke.txt'
    Write-DevStep 'Running UI smoke'
    & (Join-Path $Root 'scripts\ui-smoke.ps1') -Root $Root -TimeoutSeconds $TimeoutSeconds 2>&1 |
        Tee-Object -FilePath $output |
        Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "UI smoke failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DevDiagnose {
    param(
        [switch]$WithLaunch,
        [switch]$WithBuild,
        [switch]$WithVerify,
        [switch]$WithScreenshot,
        [switch]$WithSnapshot,
        [switch]$EmitJson
    )

    $diagArgs = @{
        Root = $Root
        OutputDir = Join-Path $artifactRoot 'diagnostics'
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($WithLaunch) { $diagArgs.Launch = $true }
    if ($WithBuild) { $diagArgs.RunBuild = $true }
    if ($WithVerify) { $diagArgs.RunVerify = $true }
    if ($WithSnapshot) { $diagArgs.RequestSnapshot = $true }
    if (-not $WithScreenshot) { $diagArgs.NoScreenshot = $true }
    if ($EmitJson) { $diagArgs.PrintJson = $true }

    Write-DevStep 'Collecting AI diagnostics'
    & (Join-Path $Root 'scripts\ai-diagnose.ps1') @diagArgs
    if ($LASTEXITCODE -ne 0) {
        throw "AI diagnostics failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DevSnapshot {
    if (-not (Get-RainspaceProcess)) {
        if ($Launch) {
            Write-DevStep 'Launching app for snapshot'
            $launch = Start-RainspaceApp -Root $Root -BuildFirst:$BuildFirst -TimeoutSeconds $TimeoutSeconds
            if (-not $launch.Process) {
                throw $launch.Message
            }
            Start-Sleep -Seconds 2
        }
        else {
            throw 'rainspace is not running. Use -Launch or start the app first.'
        }
    }

    Write-DevStep 'Requesting runtime snapshot from app'
    $result = Request-RainspaceDevSnapshot -Reason $Command -TimeoutSeconds $TimeoutSeconds
    $resultPath = Join-Path $artifactRoot 'snapshot-result.json'
    $result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resultPath -Encoding UTF8

    if (-not $result.Ok) {
        throw $result.Message
    }

    Write-DevStep "Snapshot written to $($result.SnapshotPath)"
    if ($PrintJson) {
        Get-Content -LiteralPath $result.SnapshotPath -Raw
    }
}

switch ($Command) {
    'build' {
        Invoke-DevBuild
    }
    'test' {
        Invoke-DevBuild
        Invoke-DevTests
    }
    'verify' {
        Invoke-DevVerify
    }
    'smoke' {
        if ($BuildFirst) { Invoke-DevBuild }
        if ($Launch) {
            $launch = Start-RainspaceApp -Root $Root -TimeoutSeconds $TimeoutSeconds
            if (-not $launch.Process) { throw $launch.Message }
            Start-Sleep -Seconds 2
        }
        Invoke-DevSmoke
    }
    'diagnose' {
        Invoke-DevDiagnose -WithLaunch:$Launch -WithBuild:$RunBuild -WithVerify:$RunVerify -WithSnapshot -WithScreenshot:(-not $NoScreenshot) -EmitJson:$PrintJson
    }
    'snapshot' {
        Invoke-DevSnapshot
    }
    'loop' {
        Invoke-DevBuild
        if ($RunTests -or -not ($RunSmoke -or $RunVerify)) {
            Invoke-DevTests
        }
        if ($RunVerify) {
            Invoke-DevVerify
        }

        if ($Launch -or $RunSmoke) {
            $launch = Start-RainspaceApp -Root $Root -TimeoutSeconds $TimeoutSeconds
            if (-not $launch.Process) {
                throw $launch.Message
            }
            Start-Sleep -Seconds 2
        }

        if ($RunSmoke) {
            Invoke-DevSmoke
        }

        Invoke-DevDiagnose -WithLaunch:$false -WithVerify:$RunVerify -WithSnapshot -WithScreenshot:(-not $NoScreenshot) -EmitJson:$PrintJson
        Invoke-DevSnapshot
    }
}

Write-DevStep "Artifacts: $artifactRoot"
