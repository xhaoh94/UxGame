@echo off
set a=%cd%

set b=%a%\client
set c=%a%\server\

for /f "delims=" %%i in ('dir /ad/b/s "%b%"') do (
	echo %%~ni
	mklink /J "%c%"%%~ni "%%i"	
)
pause