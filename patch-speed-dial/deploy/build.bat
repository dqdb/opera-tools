@ECHO OFF
MD temp 2>nul
MD temp\sdpatch 2>nul
DEL temp\* /Q
DEL temp\sdpatch\* /Q
COPY ..\readme.md temp >nul
COPY ..\sdpatch\* temp\sdpatch >nul

%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc /out:temp\SpeedDialPatch.exe /win32icon:..\resources\SpeedDialPatch.ico /nologo ..\*.cs
IF ERRORLEVEL 1 GOTO end

pkzip25 -add -level=9 -rec -dir=relative SpeedDialPatch.zip temp\*
IF ERRORLEVEL 1 GOTO end

RD /S /Q temp

:end
