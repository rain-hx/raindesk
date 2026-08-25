@echo off
rem =============================================================================
rem  raindesk 一键构建部署（双击运行）
rem  构建 Release 并把产物放到 C:\soft\raindesk\
rem =============================================================================
setlocal

set "SCRIPT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%build-deploy.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 构建失败，请查看上方日志。
    pause
    exit /b 1
)

echo.
echo [完成] 构建成功，产物已部署到 C:\soft\raindesk\
pause
