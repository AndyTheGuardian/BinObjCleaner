@echo off
REM Startet BinObj Cleaner immer mit dem aktuellen Stand (siehe start.ps1).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start.ps1" %*
if errorlevel 1 pause
