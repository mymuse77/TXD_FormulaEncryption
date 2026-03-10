@echo off
chcp 65001 >nul
echo ========================================
echo  简单编译脚本 - 机器指纹生成工具
echo ========================================
echo.

:: 切换到当前目录
cd /d "%~dp0"

:: 编译
echo 正在编译 MachineIDGenerator.cpp ...
echo.

cl.exe MachineIDGenerator.cpp ^
    /o MachineIDGenerator.exe ^
    /EHsc ^
    /O2 ^
    /MT ^
    /nologo ^
    iphlpapi.lib ^
    ws2_32.lib

if errorlevel 1 (
    echo.
    echo [编译失败]
    echo.
    echo 请确保：
    echo 1. 已安装Visual Studio 2019/2022
    echo 2. 已安装"使用C++的桌面开发"工作负载
    echo 3. 在"Developer Command Prompt for VS"中运行此脚本
    echo.
    echo 或者尝试完整版本：compile.bat
    echo.
    pause
    exit /b 1
)

echo.
echo [编译成功！]
echo.
echo 输出: MachineIDGenerator.exe
echo.
pause
