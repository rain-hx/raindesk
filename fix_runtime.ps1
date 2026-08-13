# Fix Windows App Runtime 2.1.3 installation
# The Main/DDLM/Singleton framework packages need special installation

$msixDir = "C:\Users\123\.nuget\packages\microsoft.windowsappsdk.runtime\2.1.3\tools\MSIX\win10-x64"

Write-Host "Step 1: Install licenses..."
& "C:\Users\123\Downloads\WindowsAppRuntimeInstall-x64.exe" --license MSIX_MAINPACKAGE_LICENSE 2>&1
& "C:\Users\123\Downloads\WindowsAppRuntimeInstall-x64.exe" --license MSIX_SINGLETONPACKAGE_LICENSE 2>&1

Write-Host "`nStep 2: Install framework packages using Add-AppxPackage with dependency..."
$mainPath = Join-Path $msixDir "Microsoft.WindowsAppRuntime.Main.2.msix"
$ddlmPath = Join-Path $msixDir "Microsoft.WindowsAppRuntime.DDLM.2.msix"
$singletonPath = Join-Path $msixDir "Microsoft.WindowsAppRuntime.Singleton.2.msix"
$runtimePath = Join-Path $msixDir "Microsoft.WindowsAppRuntime.2.msix"

# Install in dependency order
Write-Host "Installing Main..."
Add-AppxPackage -Path $mainPath -DependencyPath $ddlmPath,$singletonPath -ForceApplicationShutdown 2>&1

Write-Host "Installing DDLM..."
Add-AppxPackage -Path $ddlmPath -ForceApplicationShutdown 2>&1

Write-Host "Installing Singleton..."
Add-AppxPackage -Path $singletonPath -ForceApplicationShutdown 2>&1

Write-Host "`nStep 3: Verify..."
$main = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.Main*"
$ddlm = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.DDLM*"
$sing = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.Singleton*"

if ($main) { Write-Host "Main: $($main.Name) $($main.Version)" } else { Write-Host "Main: NOT INSTALLED" }
if ($ddlm) { Write-Host "DDLM: $($ddlm.Name) $($ddlm.Version)" } else { Write-Host "DDLM: NOT INSTALLED" }
if ($sing) { Write-Host "Singleton: $($sing.Name) $($sing.Version)" } else { Write-Host "Singleton: NOT INSTALLED" }

Write-Host "`nStep 4: Check file system..."
$mainDir = Get-ChildItem "C:\Program Files\WindowsApps" -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*WindowsAppRuntime.Main*" }
if ($mainDir) { Write-Host "Main dir: $($mainDir.Name)" } else { Write-Host "Main dir: NOT FOUND" }
