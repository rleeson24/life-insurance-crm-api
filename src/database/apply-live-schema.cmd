@echo off
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0live
set SERVER=%1
if "%SERVER%"=="" set SERVER=localhost,1433
set DB=%2
if "%DB%"=="" set DB=BrokerBook

echo Applying live schema to %SERVER% / %DB% ...
for %%F in (
  001_Tenants.sql
  002_Clients.sql
  003_ClientInteractions.sql
  004_MajorMedicalEnrollments.sql
  005_SecondaryEnrollments.sql
  006_DrugPlanEnrollments.sql
  007_OrganizationUsers.sql
  008_AuthSecurityEvents.sql
  009_RLS.sql
  010_OrganizationUserRoles.sql
  seed\001_DevelopmentTenant.sql
) do (
  echo --- %%F ---
  sqlcmd -S %SERVER% -d %DB% -E -i "%SCRIPT_DIR%\%%F" -b
  if errorlevel 1 exit /b 1
)
echo Done.
