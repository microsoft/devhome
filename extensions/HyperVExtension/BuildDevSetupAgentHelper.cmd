@echo off

powershell -ExecutionPolicy Unrestricted -NoLogo -NoProfile -File %~dp0\BuildDevSetupAgentHelper.ps1 %*

exit /b %ERRORLEVEL%