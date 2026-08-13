$f = 'D:\otherProject\GeekDesk-WinUI3\rainspace\Control\UserControls\PannelCard\RightCardControl.xaml.cs'
$c = [IO.File]::ReadAllText($f)
$c = $c -replace '(\r?\n){3,}', "`r`n`r`n"
[IO.File]::WriteAllText($f, $c)
Write-Host "Done. Lines: $((Get-Content $f).Count)"
