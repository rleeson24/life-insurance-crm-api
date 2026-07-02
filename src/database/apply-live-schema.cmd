@echo off
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0live
set SERVER=%1
if "%SERVER%"=="" set SERVER=localhost,1433
set DB=%2
if "%DB%"=="" set DB=LifeInsuranceCRM

echo Applying live schema to %SERVER% / %DB% ...
for %%F in (
  001_Tenants.sql
  002_Clients.sql
  003_ClientInteractions.sql
  004_MedicareEnrollments.sql
  005_SupplementalEnrollments.sql
  006_OrganizationUsers.sql
  007_AuthSecurityEvents.sql
  008_RLS.sql
  seed\001_DevelopmentTenant.sql
) do (
  echo --- %%F ---
  sqlcmd -S %SERVER% -d %DB% -E -i "%SCRIPT_DIR%\%%F" -b
  if errorlevel 1 exit /b 1
)
echo Done.
