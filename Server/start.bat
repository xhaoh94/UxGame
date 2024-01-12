
@echo off
set a=%cd%
set d=%a%\sv.exe

start %d% -appConf="./app_1.yaml"
start %d% -appConf="./app_2.yaml"
start %d% -appConf="./app_3.yaml"