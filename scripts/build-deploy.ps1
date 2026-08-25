# =============================================================================
# raindesk 一键构建安装脚本
# 构建 Release -> 部署到 C:\soft\raindesk\ -> 签名并安装 MSIX -> 自动打开应用
# 用法：
#   powershell -ExecutionPolicy Bypass -File scripts\build-deploy.ps1
#   或双击 scripts\build-deploy.bat
#
# 说明：
#   - 使用自签名证书 CN=AppPublisher（与 AppxManifest 的 Publisher 一致）
#   - 证书自动创建并加入受信任根/发布者，首次会触发信任提示
#   - 产物：C:\soft\raindesk\app\（可运行目录）+ packages\（MSIX）+ raindesk.exe 已安装
# =============================================================================

[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64', 'x86', 'ARM64')]
    [string]$Platform = 'x64',

    [string]$DeployRoot = 'C:\soft\raindesk',

    [string]$PackageName = 'E66FE79E-F1D2-4A70-9CF3-B2B19981F85D',

    [switch]$SkipMsix,
    [switch]$SkipInstall,
    [switch]$SkipLaunch,
    [switch]$SkipClean
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$msixDir = Join-Path $ProjectRoot "bin\$Platform\$Configuration\net8.0-windows10.0.26100.0\win-$Platform"
$signtool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$CertSubject = 'CN=AppPublisher'
$CertName = 'apppublisher'

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

# ── 工具检测 ───────────────────────────────────────────────────────────
if (-not (Test-Path $signtool)) {
    $found = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match 'x64' } | Sort-Object FullName -Descending | Select-Object -First 1
    if ($found) { $signtool = $found.FullName } else { throw '未找到 signtool.exe，请安装 Windows SDK。' }
}

# ── 1. 准备/复用签名证书 ───────────────────────────────────────────────
function Ensure-SigningCertificate {
    $certDir = Join-Path $DeployRoot 'cert'
    New-Item -ItemType Directory -Force -Path $certDir | Out-Null
    $pfxPath = Join-Path $certDir "$CertName.pfx"
    $certPassword = 'raindesk-soft'

    $cert = Get-ChildItem 'Cert:\CurrentUser\My' -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -eq $CertSubject -and $_.HasPrivateKey } |
        Sort-Object NotBefore -Descending | Select-Object -First 1

    if (-not $cert) {
        Write-Step "创建签名证书 $CertSubject"
        $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $CertSubject `
            -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy Exportable `
            -NotAfter (Get-Date).AddYears(3)
    }

    # 确保证书在受信任根和受信任发布者中（MSIX 安装需要）
    foreach ($storeName in @('Root', 'TrustedPublisher')) {
        $store = [System.Security.Cryptography.X509Certificates.X509Store]::new($storeName, 'CurrentUser')
        $store.Open('ReadWrite')
        $exists = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
        if (-not $exists) { $store.Add($cert) }
        $store.Close()
    }

    # 导出 PFX 供 signtool 使用
    if (-not (Test-Path $pfxPath)) {
        $pwd = ConvertTo-SecureString $certPassword -AsPlainText -Force
        Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pwd | Out-Null
    }

    Write-Host "  证书: $($cert.Subject)  指纹: $($cert.Thumbprint)" -ForegroundColor Green
    return @{ Cert = $cert; PfxPath = $pfxPath; Password = $certPassword }
}

# ── 2. 关闭运行实例 ────────────────────────────────────────────────────
Write-Step "关闭运行中的 raindesk"
Get-Process -Name 'raindesk' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

# ── 3. 清理旧发布（可选） ───────────────────────────────────────────────
$publishDir = Join-Path $msixDir 'publish'
if (-not $SkipClean -and (Test-Path $publishDir)) {
    Remove-Item -Recurse -Force $publishDir
}

# ── 4. Release 发布构建 ────────────────────────────────────────────────
Write-Step "执行 dotnet publish (Release, 自包含)"
dotnet publish (Join-Path $ProjectRoot 'raindesk.csproj') `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:RuntimeIdentifier="win-$Platform" `
    -p:SelfContained=true `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishReadyToRun=false `
    -p:PublishTrimmed=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败，退出码 $LASTEXITCODE" }

# ── 5. 部署可运行目录到 C:\soft\raindesk\app ──────────────────────────
Write-Step "部署到 $DeployRoot"
$deployApp = Join-Path $DeployRoot 'app'
$deployPkg = Join-Path $DeployRoot 'packages'
New-Item -ItemType Directory -Force -Path $deployApp | Out-Null
New-Item -ItemType Directory -Force -Path $deployPkg | Out-Null
# The publish root's generic resources.pri conflicts with WinUI's raindesk.pri
# in an unpackaged deployment and causes Microsoft.UI.Xaml.dll to terminate.
$stalePri = Join-Path $deployApp 'resources.pri'
if (Test-Path -LiteralPath $stalePri) {
    Remove-Item -LiteralPath $stalePri -Force
}
Get-ChildItem -LiteralPath $publishDir -Force |
    Where-Object { $_.Name -ne 'resources.pri' } |
    Copy-Item -Destination $deployApp -Recurse -Force
# WinUI's publish folder does not contain the compiled XAML/PRI tree. Copy the
# complete build layout as well, otherwise Microsoft.UI.Xaml exits at startup.
Get-ChildItem -LiteralPath $msixDir -Force |
    Where-Object { $_.Name -ne 'publish' } |
    Copy-Item -Destination $deployApp -Recurse -Force
Write-Host "  可运行目录 -> $deployApp" -ForegroundColor Green

# ── 6. 生成并签名 MSIX ─────────────────────────────────────────────────
$msixFile = Join-Path $msixDir "raindesk_1.0.0.0_$Platform.msix"
if (-not $SkipMsix) {
    $signCert = Ensure-SigningCertificate

    Write-Step "生成 MSIX（构建工具签名）"
    dotnet publish (Join-Path $ProjectRoot 'raindesk.csproj') `
        -c $Configuration `
        -p:Platform=$Platform `
        -p:RuntimeIdentifier="win-$Platform" `
        -p:SelfContained=true `
        -p:WindowsAppSDKSelfContained=true `
        -p:EnableMsixTooling=true `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=true `
        -p:PackageCertificateThumbprint=$($signCert.Cert.Thumbprint) `
        -p:WindowsPackageType=MSIX `
        -p:EnableDefaultPriItems=false `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "MSIX 打包失败，回退到 signtool 签名已有包..."
        if (Test-Path $msixFile) {
            & $signtool sign /fd SHA256 /sha1 $($signCert.Cert.Thumbprint) $msixFile 2>&1 | Out-Null
        }
    }

    # 复制 MSIX 到部署目录
    Get-ChildItem -Path $msixDir -Filter '*.msix*' -File -ErrorAction SilentlyContinue |
        Copy-Item -Destination $deployPkg -Force
    Write-Host "  MSIX -> $deployPkg" -ForegroundColor Green

    # ── 7. 安装 MSIX ──────────────────────────────────────────────────
    if (-not $SkipInstall) {
        Write-Step "安装 MSIX 到系统"
        if (Test-Path $msixFile) {
            Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue |
                Remove-AppxPackage -ErrorAction SilentlyContinue
            Add-AppxPackage -Path $msixFile -ErrorAction Stop
            Write-Host "  MSIX 安装成功: $PackageName" -ForegroundColor Green
        } else {
            Write-Warning "未找到 MSIX，跳过安装"
        }
    }
}

# ── 8. 版本信息 ────────────────────────────────────────────────────────
$exe = Join-Path $deployApp 'raindesk.exe'
if (Test-Path $exe) {
    $ver = (Get-Item $exe).VersionInfo.FileVersion
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    @"
build_time = $stamp
file_version = $ver
configuration = $Configuration
platform = $Platform
"@ | Set-Content -Path (Join-Path $DeployRoot 'BUILD.txt') -Encoding UTF8
    Write-Host "  版本: $ver  构建时间: $stamp" -ForegroundColor Green
}

# ── 9. 自动打开应用 ────────────────────────────────────────────────────
if (-not $SkipLaunch) {
    Write-Step "启动应用"
    # 优先启动 MSIX 已安装版本；绿色版也包含 .NET 8 与 Windows App SDK，
    # 无需用户另外安装运行时。
    $inst = Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue
    if ($inst -and (Test-Path (Join-Path $inst.InstallLocation 'raindesk.exe'))) {
        Start-Process (Join-Path $inst.InstallLocation 'raindesk.exe')
        Write-Host "  已启动 MSIX 应用: $PackageName" -ForegroundColor Green
    } elseif (Test-Path $exe) {
        Start-Process $exe
        Write-Host "  已启动绿色版: $exe" -ForegroundColor Green
    } else {
        Write-Warning "未找到可启动的应用"
    }
}

Write-Step "全部完成"
Write-Host "  运行目录: $deployApp\raindesk.exe" -ForegroundColor Yellow
Write-Host "  部署目录: $DeployRoot" -ForegroundColor Yellow
