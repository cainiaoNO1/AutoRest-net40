@echo off
chcp 936 >nul
title 启动 AutoRest
cd /d "%~dp0"

if exist "%~dp0AutoRest.exe" (
    start "" "%~dp0AutoRest.exe"
    exit /b 0
)

if exist "%~dp0bin\Release\net40\AutoRest.exe" (
    start "" "%~dp0bin\Release\net40\AutoRest.exe"
    exit /b 0
)

echo 未找到 AutoRest.exe，请先执行：dotnet build AutoRest.csproj -c Release
pause
