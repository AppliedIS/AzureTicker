@echo off
set VALUE1="%~dp0%InstallScheduledTask.ps1"
set p1=%1
set scriptPath=%VALUE1% %p1%
echo %scriptPath%
powershell -command "& {Set-ExecutionPolicy Unrestricted}"
powershell -ExecutionPolicy Bypass -File %scriptPath%
powershell -command "& {Set-ExecutionPolicy AllSigned}"