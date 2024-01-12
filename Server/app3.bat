
@echo off
set a=%cd%
set d=%a%\sv.exe

%d% -appConf="./app_3.yaml"
pause