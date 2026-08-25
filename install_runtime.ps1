$msixDir = "C:\Users\123\.nuget\packages\microsoft.windowsappsdk.runtime\2.1.3\tools\MSIX\win10-x64"
$packages = @(
    "Microsoft.WindowsAppRuntime.Main.2.msix",
    "Microsoft.WindowsAppRuntime.DDLM.2.msix",
    "Microsoft.WindowsAppRuntime.Singleton.2.msix",
    "Microsoft.WindowsAppRuntime.2.msix"
)

foreach ($pkg in $packages) {
    $path = Join-Path $msixDir $pkg
    if (Test-Path $path) {
        Write-Host "Installing $pkg..."
        try {
            Add-AppxPackage -Path $path -ForceApplicationShutdown -ErrorAction Stop
            Write-Host "  OK"
        } catch {
            Write-Host "  Error: $_"
        }
    } else {
        Write-Host "Not found: $path"
    }
}

Write-Host "`nInstalled packages:"
Get-AppxPackage | Where-Object { $_.Name -like "*WindowsAppRuntime*" } | ForEach-Object {
    Write-Host "  $($_.Name) $($_.Version) $($_.Architecture) $($_.Status)"
}
