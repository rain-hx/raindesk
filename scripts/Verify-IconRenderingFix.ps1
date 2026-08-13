param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\raindesk')).Path
)

$ErrorActionPreference = 'Stop'

function Assert-FileContains {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-FileDoesNotContain {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match $Pattern) {
        throw $Message
    }
}

$iconInfo = Join-Path $ProjectRoot 'ViewModel\IconInfo.cs'
$imageUtil = Join-Path $ProjectRoot 'Util\ImageUtil.cs'
$commonCode = Join-Path $ProjectRoot 'Util\CommonCode.cs'
$appXaml = Join-Path $ProjectRoot 'App.xaml'
$leftCard = Join-Path $ProjectRoot 'Control\UserControls\PannelCard\LeftCardControl.xaml'
$leftCardCode = Join-Path $ProjectRoot 'Control\UserControls\PannelCard\LeftCardControl.xaml.cs'
$rightCardCode = Join-Path $ProjectRoot 'Control\UserControls\PannelCard\RightCardControl.xaml.cs'

Assert-FileContains $iconInfo 'if\s*\(\s*bitmapImage\s*!=\s*null\s*\)\s*return\s+bitmapImage;' `
    'IconInfo BitmapImage getters must return the cached BitmapImage when raw icon bytes are unavailable.'

Assert-FileContains $imageUtil 'GetImageBytesByPath' `
    'ImageUtil must expose a single helper that returns persistent icon bytes for image files, shell icons, and folders.'

Assert-FileContains $commonCode 'ImageUtil\.GetImageBytesByPath\(iconPath\)' `
    'CommonCode.GetIconInfoByPath must store image/icon bytes before binding the icon into the UI.'

Assert-FileContains $leftCardCode 'MenuGeometryHelper\.ApplyToPath\(' `
    'Menu icon Path.Data must convert persisted path strings to WinUI Geometry objects.'

Assert-FileContains $leftCard 'Converter=\{StaticResource TextToColorConverter\}' `
    'Menu icon Fill must convert persisted color text to a WinUI Brush.'

Assert-FileContains (Join-Path $ProjectRoot 'Control\UserControls\PannelCard\RightCardControl.xaml') '(<Image\s+Source="\{(Binding BitmapImage|x:Bind BitmapImage[^}]*)\}"|<ImageBrush ImageSource="\{Binding BitmapImage\}"[\s\S]*?Stretch="UniformToFill")' `
    'Main app tiles must bind icon pixels directly on the Image control or ImageBrush, not through a Style Setter.'

Assert-FileContains (Join-Path $ProjectRoot 'Control\UserControls\PannelCard\RightCardControl.xaml') '<Setter\s+Property="Stretch"\s+Value="Uniform"\s*/>' `
    'ImageStyle should only contain static visual values that WinUI can apply reliably.'

Assert-FileContains (Join-Path $ProjectRoot 'Control\UserControls\PannelCard\RightCardControl.xaml') 'Foreground="\{Binding DataContext\.AppConfig\.TextColor, ElementName=RootGrid, Converter=\{StaticResource TextToColorConverter\}\}"' `
    'Icon title Foreground must convert persisted color text to a WinUI Brush.'

Assert-FileContains $appXaml 'StringToGeometryConverter' `
    'App.xaml must register the geometry converter used by menu icon Path.Data bindings.'

Assert-FileDoesNotContain $rightCardCode '\(sender\s+as\s+StackPanel\)|\(\(StackPanel\)sender\)' `
    'RightCardControl pointer handlers must not assume shortcut item templates use StackPanel.'

Write-Host 'Icon rendering regression checks passed.'
