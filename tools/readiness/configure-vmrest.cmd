@echo off
setlocal
if "%~1"=="" exit /b 2
if exist "%USERPROFILE%\.vmrest" (
  echo Existing vmrest configuration found. It will not be replaced.
  pause
  exit /b 2
)
"%~1" -C
set "vmrest_configuration_exit=%errorlevel%"
echo.
echo vmrest configuration exit code: %vmrest_configuration_exit%
echo Report only the success or error message, never the password.
pause
exit /b %vmrest_configuration_exit%
