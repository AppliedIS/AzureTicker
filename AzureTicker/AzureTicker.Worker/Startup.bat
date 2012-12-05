@echo off
SET RUNLOCATION="%~dp0%Run"
xcopy %RUNLOCATION% "D:\run" /i /c /y /q
REG ADD "HKLM\Software\Policies\Microsoft\Internet Explorer\Main" /f /v DisableFirstRunCustomize /t REG_DWORD /d 1
set VALUE1="%~dp0%InstallScheduledTask.ps1"
set p1=%1
set scriptPath=%VALUE1% %p1%
powershell -command "& {Set-ExecutionPolicy Unrestricted}"
powershell -ExecutionPolicy Bypass -File "ieScript.ps1"
powershell -ExecutionPolicy Bypass -File %scriptPath%
powershell -command "& {Set-ExecutionPolicy AllSigned}"
