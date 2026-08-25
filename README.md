# GeekDesk-WinUI3 (rainspace)

Windows desktop productivity app built with **WinUI 3**, **.NET 8**, and **MSIX** packaging. Successor to GeekDesk: app launcher, todo management, global search, tray icon, and customizable desktop panels.

## Requirements

- Windows 10 1809+ (Windows 11 recommended)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 with **.NET desktop development** and **Windows App SDK** workloads (optional, for F5 debugging)

## Build

From the repository root:

```powershell
dotnet build .\rainspace\rainspace.csproj -c Debug -p:Platform=x64
```

Or open `GeekDesk-WinUI3.sln` in Visual Studio, select **x64** and **rainspace (Package)**, then press F5.

## Run

```powershell
dotnet msbuild .\rainspace\rainspace.csproj -t:RunPackagedApp -c Debug -p:Platform=x64
```

Dev script (build, test, launch):

```powershell
.\scripts\dev.ps1 build
.\scripts\dev.ps1 diagnose -Launch
```

Full dev loop:

```powershell
.\scripts\dev.ps1 loop -Launch -RunSmoke -RunVerify
```

## Tray icon

After launch the app may appear only in the **system tray** (notification area). Look for the rainspace icon near the clock. If hidden, enable **Always show** in Windows tray settings.

## Tests and verification

```powershell
dotnet test .\rainspace.Tests\rainspace.Tests.csproj -c Debug -p:Platform=x64
.\scripts\dev.ps1 verify
.\scripts\verify-settings.ps1
```

## AI diagnostics

See [docs/ai-diagnostics.md](docs/ai-diagnostics.md) for the dev diagnostic loop and runtime snapshots.

## Project layout

| Path | Purpose |
|------|---------|
| `rainspace/` | Main WinUI 3 application |
| `rainspace.Tests/` | MSTest unit tests |
| `scripts/` | Build, verify, UI smoke, AI diagnostics |
| `docs/` | Developer documentation |
