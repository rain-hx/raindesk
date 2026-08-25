$msixDir = "C:\Users\123\.nuget\packages\microsoft.windowsappsdk.runtime\2.1.3\tools\MSIX\win10-x64"

Write-Host "=== Installing WinAppRuntime 2.1.3 framework packages ==="

# These are framework packages that need admin/system deployment
$packages = @(
    "Microsoft.WindowsAppRuntime.Main.2.msix",
    "Microsoft.WindowsAppRuntime.DDLM.2.msix",
    "Microsoft.WindowsAppRuntime.Singleton.2.msix"
)

foreach ($pkg in $packages) {
    $path = Join-Path $msixDir $pkg
    Write-Host "`nInstalling $pkg..."
    try {
        # Try provisioned package (system-wide)
        $result = Add-AppxProvisionedPackage -Online -PackagePath $path -SkipLicense 2>&1
        Write-Host "  Provision result: $result"
    } catch {
        Write-Host "  Provision error: $_"
    }
}

Write-Host "`n=== Checking installed packages ==="
Get-AppxPackage | Where-Object { $_.Name -like "*WindowsAppRuntime*" } | ForEach-Object {
    Write-Host "  $($_.Name) $($_.Version) $($_.Architecture)"
}

Write-Host "`n=== Checking provisioned packages ==="
Get-AppxProvisionedPackage -Online | Where-Object { $_.PackageName -like "*WindowsAppRuntime*" } | ForEach-Object {
    Write-Host "  $($_.PackageName)"
}

Write-Host "`n=== Checking file system ==="
Get-ChildItem "C:\Program Files\WindowsApps" -Directory | Where-Object { $_.Name -like "*WindowsAppRuntime*" } | ForEach-Object {
    Write-Host "  $($_.Name)"
}
