@echo off
rem BugHunt Unity grader. Close the Unity editor first. Example: grade.bat -NoGraphics -Labs 23,24
rem BugHunt Unity 採点ツール。先に Unity エディターを閉じてください。例: grade.bat -NoGraphics -Labs 23,24
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0grade.ps1" %*
exit /b %ERRORLEVEL%
