param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$GeekDeskExe = "",
    [string]$OutputJson = "",
    [switch]$InstallToRainspace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$migratorProject = Join-Path $root 'tools\LegacyBackupMigrator\LegacyBackupMigrator.csproj'
$backupPath = (Resolve-Path $BackupPath).Path

if (-not $OutputJson) {
    $OutputJson = [System.IO.Path]::ChangeExtension($backupPath, '.json')
}

if (-not $GeekDeskExe) {
    $tempGeekDesk = Join-Path $env:TEMP 'GeekDesk215\GeekDesk.exe'
    if (-not (Test-Path $tempGeekDesk)) {
        $zip = Join-Path $env:TEMP 'GeekDesk.2.5.15.zip'
        $dir = Split-Path $tempGeekDesk -Parent
        Write-Host "Downloading GeekDesk 2.5.15 for legacy backup import..."
        Invoke-WebRequest -Uri 'https://github.com/BookerLiu/GeekDesk/releases/download/2.5.15/GeekDesk.2.5.15.zip' -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath (Split-Path $dir -Parent) -Force
    }
    $GeekDeskExe = $tempGeekDesk
}

if (-not (Test-Path $GeekDeskExe)) {
    throw "GeekDesk.exe not found: $GeekDeskExe"
}

Write-Host "Migrating legacy backup: $backupPath"
dotnet run --project $migratorProject -c Release -- $backupPath $OutputJson $GeekDeskExe | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Legacy backup migration failed with exit code $LASTEXITCODE"
}

if ($InstallToRainspace) {
    $dataPath = Join-Path $root 'rainspace\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\AppX\Data'
    if (-not (Test-Path (Split-Path $dataPath -Parent))) {
        throw "Rainspace AppX output not found. Build the app first: dotnet build rainspace/rainspace.csproj -c Debug -p:Platform=x64"
    }

    Copy-Item $OutputJson $dataPath -Force
    Write-Host "Installed migrated data to: $dataPath"
}

Write-Host "Done. JSON output: $OutputJson"
