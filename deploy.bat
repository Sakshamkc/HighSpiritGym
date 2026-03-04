@echo off
REM ==========================================
REM  High Spirit Gym - One-Click Deploy (Windows)
REM  Just double-click this file or run: deploy.bat
REM ==========================================

set VPS_IP=161.97.93.213
set VPS_USER=root
set REMOTE_PATH=/var/www/highspirit/publish
set SERVICE_NAME=highspirit
set PROJECT_DIR=HighSpiritApp

echo.
echo [1/7] Uploading maintenance page...
scp %PROJECT_DIR%\wwwroot\maintenance.html %VPS_USER%@%VPS_IP%:%REMOTE_PATH%/wwwroot/maintenance_on.html
ssh %VPS_USER%@%VPS_IP% "cp %REMOTE_PATH%/wwwroot/maintenance.html %REMOTE_PATH%/wwwroot/maintenance_backup.html 2>/dev/null; cp %REMOTE_PATH%/wwwroot/maintenance_on.html %REMOTE_PATH%/wwwroot/maintenance.html 2>/dev/null"
echo    Maintenance page is now live.

echo.
echo [2/7] Stopping remote service %SERVICE_NAME% on %VPS_IP%...
ssh %VPS_USER%@%VPS_IP% "systemctl stop %SERVICE_NAME%; mkdir -p %REMOTE_PATH%"
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Could not stop remote service. Continuing anyway...
)

echo.
echo [3/7] Publishing .NET 8.0 app (Release)...
cd %PROJECT_DIR%
dotnet publish -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet publish failed!
    cd ..
    pause
    exit /b 1
)
cd ..

echo.
echo [4/7] Uploading published files to %VPS_IP%:%REMOTE_PATH% ...
scp -r %PROJECT_DIR%\bin\Release\net8.0\publish\* %VPS_USER%@%VPS_IP%:%REMOTE_PATH%/
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: File upload failed!
    pause
    exit /b 1
)

echo.
echo [5/7] Starting remote service %SERVICE_NAME%...
ssh %VPS_USER%@%VPS_IP% "systemctl start %SERVICE_NAME%"

echo.
echo [6/7] Removing maintenance page...
ssh %VPS_USER%@%VPS_IP% "rm -f %REMOTE_PATH%/wwwroot/maintenance_on.html %REMOTE_PATH%/wwwroot/maintenance_backup.html 2>/dev/null"
echo    Maintenance page removed. App is live!

echo.
echo [7/7] Checking service status...
ssh %VPS_USER%@%VPS_IP% "systemctl status %SERVICE_NAME% --no-pager -l"

echo.
echo =============================================
echo   Deployed successfully!
echo   Open: http://%VPS_IP%:5000/
echo =============================================
pause
