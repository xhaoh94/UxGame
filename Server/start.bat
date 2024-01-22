
@echo off
set a=%cd%
set d=%a%\uxgame.exe
set e=%a%\etcd\etcd.exe

tasklist|findstr /i %e%>nul &&echo "" || start %e%

start %d% -appConf="./app_1.yaml"
start %d% -appConf="./app_2.yaml"
start %d% -appConf="./app_3.yaml"