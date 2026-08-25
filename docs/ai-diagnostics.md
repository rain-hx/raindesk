# AI Diagnostics and Dev Loop

rainspace ships a small dev toolchain for AI-assisted debugging and local co-development with the running app.

## Quick Start

Full local loop: build, unit tests, launch, UI smoke, diagnostics, runtime snapshot.

```powershell
.\scripts\dev.ps1 loop -Launch -RunSmoke
```

Collect diagnostics from a running app and ask the app for a runtime snapshot:

```powershell
.\scripts\dev.ps1 diagnose -Launch -RequestSnapshot
```

Request a live snapshot from the running app:

```powershell
.\scripts\dev.ps1 snapshot -Launch
```

## Commands

| Command | Purpose |
|---------|---------|
| `.\scripts\dev.ps1 loop` | Build, test, diagnose, snapshot |
| `.\scripts\dev.ps1 build` | Build rainspace only |
| `.\scripts\dev.ps1 test` | Build + run headless unit tests (`VersionComparisonTests`) |
| `.\scripts\dev.ps1 verify` | Icon rendering checks + asset validation |
| `.\scripts\dev.ps1 smoke` | UI Automation smoke across Home/Settings/Todo |
| `.\scripts\dev.ps1 diagnose` | Wrapper around `ai-diagnose.ps1` |
| `.\scripts\dev.ps1 snapshot` | Request `%LOCALAPPDATA%\rainspace\dev\snapshot.json` from the app |

Useful flags:

- `-Launch` start the packaged app when it is not running
- `-RunSmoke` include UI smoke in `loop`
- `-RunVerify` include static verification scripts
- `-PrintJson` print JSON to stdout

## AI Diagnostics

`scripts/ai-diagnose.ps1` collects a stable diagnostic bundle for AI-assisted debugging.

Collect current state without launching the app:

```powershell
.\scripts\ai-diagnose.ps1
```

Launch the app if it is not running, then collect UI state, screenshot, and runtime snapshot:

```powershell
.\scripts\ai-diagnose.ps1 -Launch -RequestSnapshot
```

Collect diagnostics and run build / verify / unit test checks:

```powershell
.\scripts\ai-diagnose.ps1 -Launch -RunBuild -RunVerify -RunTests -RequestSnapshot
```

Print the generated JSON to stdout as well:

```powershell
.\scripts\ai-diagnose.ps1 -PrintJson
```

## App Integration

While rainspace is running, the app watches:

```text
%LOCALAPPDATA%\rainspace\dev\request.json
```

When that file appears, the app writes:

```text
%LOCALAPPDATA%\rainspace\dev\snapshot.json
```

The snapshot includes window visibility, selected menu, icon counts, tray/config flags, and asset presence inside the packaged layout.

You can also opt into a startup snapshot with:

```powershell
$env:RAINSPACE_DEV = '1'
dotnet msbuild .\rainspace\rainspace.csproj -t:RunPackagedApp -c Debug -p:Platform=x64
```

Shared helpers live in `scripts/lib/rainspace-dev.psm1`:

- resolve runtime/AppX paths
- launch via `winapp.exe` when available
- discover packaged log directories under `%LOCALAPPDATA%\Packages`
- validate icon assets
- request runtime snapshots from the app

## Output

Diagnostic runs write to:

```text
artifacts\ai-diagnostics\<timestamp>\
```

Dev loop runs write to:

```text
artifacts\dev-loop\<timestamp>\
```

Important files:

- `diagnostics.json`: machine-readable report for AI
- `summary.txt`: short human-readable summary
- `current-window.png`: screenshot when a window is found
- `build.txt`, `unit-tests.txt`, `verify-icon-rendering.txt`: optional command output
- `logs.json`: log tails extracted separately for stable JSON serialization
- `%LOCALAPPDATA%\rainspace\dev\snapshot.json`: live app state written by `DevDiagnosticService`

The JSON includes process/window state, UI Automation tree, window bounds, screenshot path, runtime `Data` summary, asset checks, backup file list, packaged log tails, and optional app snapshot data.
