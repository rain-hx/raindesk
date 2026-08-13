param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$raindeskExe = "",
    [string]$OutputJson = "",
    [switch]$InstallToraindesk
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$migratorProject = Join-Path $root 'tools\LegacyBackupMigrator\LegacyBackupMigrator.csproj'
$backupPath = (Resolve-Path $BackupPath).Path

if (-not $OutputJson) {
    $OutputJson = [System.IO.Path]::ChangeExtension($backupPath, '.json')
}

if (-not $raindeskExe) {
    $tempraindesk = Join-Path $env:TEMP 'raindesk215\raindesk.exe'
    if (-not (Test-Path $tempraindesk)) {
        $zip = Join-Path $env:TEMP 'raindesk.2.5.15.zip'
        $dir = Split-Path $tempraindesk -Parent
        Write-Host "Downloading raindesk 2.5.15 for legacy backup import..."
        Invoke-WebRequest -Uri 'https://github.com/BookerLiu/raindesk/releases/download/2.5.15/raindesk.2.5.15.zip' -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath (Split-Path $dir -Parent) -Force
    }
    $raindeskExe = $tempraindesk
}

if (-not (Test-Path $raindeskExe)) {
    throw "raindesk.exe not found: $raindeskExe"
}

Write-Host "Migrating legacy backup: $backupPath"
dotnet run --project $migratorProject -c Release -- $backupPath $OutputJson $raindeskExe | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Legacy backup migration failed with exit code $LASTEXITCODE"
}

if ($InstallToraindesk) {
    $dataPath = Join-Path $root 'raindesk\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\AppX\Data'
    if (-not (Test-Path (Split-Path $dataPath -Parent))) {
        throw "raindesk AppX output not found. Build the app first: dotnet build raindesk/raindesk.csproj -c Debug -p:Platform=x64"
    }

    Copy-Item $OutputJson $dataPath -Force
    Write-Host "Installed migrated data to: $dataPath"
}

Write-Host "Done. JSON output: $OutputJson"
