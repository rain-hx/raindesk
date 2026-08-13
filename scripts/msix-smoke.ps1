param(
    [string]$MsixPath = '',
    [string]$PublisherSubject = 'CN=AppPublisher',
    [string]$PackageName = 'E66FE79E-F1D2-4A70-9CF3-B2B19981F85D',
    [int]$LaunchTimeoutSeconds = 30
)

# Installs a signed MSIX package, launches the app, verifies the process stays
# alive (diagnostic handshake substitute), then uninstalls the package.
# Exit code 0 = package install/launch/uninstall round-trip succeeded.
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($MsixPath)) {
    $msix = Get-ChildItem -Recurse -Filter '*.msix' -Path (Join-Path $PSScriptRoot '..\rainspace\AppPackages') -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $msix) {
        Write-Error 'No .msix artifact found. Build the package first (dotnet publish ... -p:GenerateAppxPackageOnBuild=true).'
    }
    $MsixPath = $msix.FullName
}

if (-not (Test-Path -LiteralPath $MsixPath)) {
    Write-Error "MSIX not found: $MsixPath"
}

Write-Host "MSIX: $MsixPath"

$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $PublisherSubject -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy Exportable -NotAfter (Get-Date).AddDays(30)
$certPath = Join-Path $env:TEMP 'rainspace-test.pfx'
$certPassword = 'rainspace-ci-test'
$securePassword = ConvertTo-SecureString $certPassword -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePassword | Out-Null

# Trust the test certificate so Add-AppxPackage accepts the signature.
$trustedRoot = [System.Security.Cryptography.X509Certificates.X509Store]::new('Root', 'CurrentUser')
$trustedRoot.Open('ReadWrite')
$trustedRoot.Add($cert)
$trustedRoot.Close()

# Locate signtool (Windows SDK build tools ship it; search common install roots).
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match 'x64' } | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $signtool) {
    Write-Error 'signtool.exe not found under Windows Kits.'
}
Write-Host "signtool: $($signtool.FullName)"

& $signtool.FullName sign /fd SHA256 /f $certPath /p $certPassword $MsixPath
if ($LASTEXITCODE -ne 0) {
    throw "MSIX signing failed with exit code $LASTEXITCODE"
}

# Remove any prior install of this package to guarantee a clean round-trip.
Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue |
    Remove-AppxPackage -ErrorAction SilentlyContinue

Add-AppxPackage -Path $MsixPath -ErrorAction Stop
Write-Host 'Package installed.'

$installed = Get-AppxPackage -Name $PackageName -ErrorAction Stop
Write-Host "Installed package: $($installed.Name) version $($installed.Version) at $($installed.InstallLocation)"

$exePath = Join-Path $installed.InstallLocation 'rainspace.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "rainspace.exe not found in package install location: $exePath"
}

# Launch the packaged exe and wait for a live process (surrogate for a
# diagnostic handshake; the app must stay up for the whole observation window).
$process = Start-Process -FilePath $exePath -PassThru
$deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
$alive = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if ($process.HasExited) {
        break
    }
    $alive = $true
    Start-Sleep -Milliseconds 500
    if ((Get-Date) -ge $deadline) {
        break
    }
}

if (-not $alive) {
    if ($process.HasExited) {
        Write-Host "App exited with code $($process.ExitCode) during launch window."
    }
    Remove-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue
    throw 'App did not stay alive during the launch smoke window.'
}

Write-Host "App stayed alive for $LaunchTimeoutSeconds s (PID $($process.Id)). Launch smoke passed."

if (-not $process.HasExited) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}

Remove-AppxPackage -Name $PackageName -ErrorAction Stop
Write-Host 'Package uninstalled. MSIX install/launch/uninstall smoke passed.'
