
@echo off
set a=%cd%
set d=%a%\sv.exe

%d% -appConf="./app_1.yaml"
pause