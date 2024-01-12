
@echo off
set a=%cd%
set d=%a%\sv.exe

%d% -appConf="./app_2.yaml"
pause