@echo off
rem BugHunt Unity grader. Close the Unity editor first. Example: grade.bat -NoGraphics -Labs 24,25
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0grade.ps1" %*
exit /b %ERRORLEVEL%
