@echo off

powershell -ExecutionPolicy Unrestricted -NoLogo -NoProfile -File %~dp0\Test.ps1 %*

exit /b %ERRORLEVEL%