@echo off
setlocal
REM Local Windows-auth wrapper. For Azure SQL use apply-live-schema.ps1 (Entra token).
set SERVER=%1
if "%SERVER%"=="" set SERVER=localhost,1433
set DB=%2
if "%DB%"=="" set DB=BrokerBook
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0apply-live-schema.ps1" -Server "%SERVER%" -Database "%DB%" -UseIntegratedSecurity -IncludeSeed
exit /b %ERRORLEVEL%
