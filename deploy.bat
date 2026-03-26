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
set MAINTENANCE_FLAG=/var/www/maintenance_on

echo.
echo [1/8] Uploading maintenance page to server...
scp %PROJECT_DIR%\wwwroot\maintenance.html %VPS_USER%@%VPS_IP%:/var/www/maintenance.html
echo    Maintenance page uploaded.

echo.
echo [2/8] Enabling maintenance mode...
ssh %VPS_USER%@%VPS_IP% "touch %MAINTENANCE_FLAG% && systemctl reload nginx"
echo    Maintenance mode is ON. All visitors see the maintenance page.

echo.
echo [3/8] Stopping remote service %SERVICE_NAME% on %VPS_IP%...
ssh %VPS_USER%@%VPS_IP% "systemctl stop %SERVICE_NAME%; mkdir -p %REMOTE_PATH%"
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Could not stop remote service. Continuing anyway...
)

echo.
echo [4/8] Publishing .NET 8.0 app (Release)...
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
echo [5/8] Uploading published files to %VPS_IP%:%REMOTE_PATH% ...
scp -r %PROJECT_DIR%\bin\Release\net8.0\publish\* %VPS_USER%@%VPS_IP%:%REMOTE_PATH%/
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: File upload failed!
    pause
    exit /b 1
)

echo.
echo [6/8] Starting remote service %SERVICE_NAME%...
ssh %VPS_USER%@%VPS_IP% "systemctl start %SERVICE_NAME%"

echo.
echo [7/8] Waiting for app to start...
timeout /t 5 /nobreak >nul
echo    Disabling maintenance mode...
ssh %VPS_USER%@%VPS_IP% "rm -f %MAINTENANCE_FLAG% && systemctl reload nginx"
echo    Maintenance mode is OFF. App is live!

echo.
echo [8/8] Checking service status...
ssh %VPS_USER%@%VPS_IP% "systemctl status %SERVICE_NAME% --no-pager -l"

echo.
echo =============================================
echo   Deployed successfully!
echo   Open: http://%VPS_IP%:5000/
echo =============================================
pause
