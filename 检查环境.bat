@echo off
chcp 936 >nul
title AutoRest 运行环境检查
setlocal EnableExtensions

echo ============================================================
echo  AutoRest 运行环境检查
echo ============================================================
echo.

echo [1] 操作系统版本
echo ------------------------------------------------------------
ver
if exist "%SystemRoot%\System32\wbem\wmic.exe" (
    wmic os get Caption,Version,OSArchitecture /value 2>nul | findstr /i "Caption Version OSArchitecture"
)
echo.

echo [2] 已安装的 .NET Framework 版本
echo ------------------------------------------------------------
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP" 2>nul
echo.
echo   -- v4 Full（关键：这里必须存在 4.x）--
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Version 2>nul
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v InstallPath 2>nul
echo.
echo   -- v3.5（老机器可能只有它）--
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" /v Version 2>nul
echo.

echo [3] 程序文件
echo ------------------------------------------------------------
if exist "%~dp0AutoRest.exe" (
    echo   已找到 AutoRest.exe
    dir "%~dp0AutoRest.exe" | findstr /i "AutoRest.exe"
) else (
    echo   [缺少] 当前目录没有 AutoRest.exe，请把本脚本放到 exe 同一目录
)
echo.

echo [4] 尝试启动并收集崩溃日志
echo ------------------------------------------------------------
if not exist "%~dp0AutoRest.exe" goto done
if exist "%~dp0crash.log" del /q "%~dp0crash.log"
start "" "%~dp0AutoRest.exe"
echo   已尝试启动，等待 6 秒...
ping -n 7 127.0.0.1 >nul
tasklist /fi "imagename eq AutoRest.exe" | findstr /i "AutoRest.exe" >nul
if errorlevel 1 (
    echo   结果: 进程未存活，启动失败或已退出
) else (
    echo   结果: 进程正在运行，本次启动成功
)
echo.
echo   -- crash.log 内容 --
echo ------------------------------------------------------------
if exist "%~dp0crash.log" (
    type "%~dp0crash.log"
) else (
    echo   没有生成 crash.log，说明程序没走到写日志的阶段，
    echo   通常意味着 .NET Framework 运行时缺失或清单解析失败
)

:done
echo.
echo ============================================================
echo  检查结束，请把以上完整内容反馈给开发者
echo ============================================================
pause
