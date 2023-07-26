
@echo off
set a=%cd%
set d=%a%\PbTool\pbtool.exe

%d% -type=csharp_pbnet
pause