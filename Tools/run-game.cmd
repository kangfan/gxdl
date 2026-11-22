@echo off
rem Starts a play session in the already-open Unity editor.
rem Unity polls for this file; it is deleted once play mode starts.
setlocal
cd /d "%~dp0"
echo run > "%~dp0run-game.trigger"
echo Trigger written: %~dp0run-game.trigger
echo Switch to the Unity window - play mode should start within a second or two.
echo Result log: %~dp0..\Logs\run-game.txt
timeout /t 4 >nul
