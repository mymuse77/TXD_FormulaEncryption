@echo off
chcp 65001 >nul
echo ========================================
echo  通达信DLL编译脚本
echo ========================================
echo.

REM 检查是否安装了Visual Studio
set "VS_PATH="
for %%i in ("C:\Program Files\Microsoft Visual Studio\2022\Professional" "C:\Program Files\Microsoft Visual Studio\2022\Community" "C:\Program Files\Microsoft Visual Studio\2022\Enterprise") do (
    if exist "%%~i\MSBuild\Current\Bin\MSBuild.exe" (
        set "VS_PATH=%%~i"
        goto :found_vs
    )
)

for %%i in ("C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional" "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community" "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise") do (
    if exist "%%~i\MSBuild\Current\Bin\MSBuild.exe" (
        set "VS_PATH=%%~i"
        goto :found_vs
    )
)

echo 错误: 未找到Visual Studio 2019/2022
echo 请确保已安装Visual Studio并且包含C++工作负载
echo.
pause
exit /b 1

:found_vs
echo 找到Visual Studio: %VS_PATH%
echo.

REM 设置项目文件
set "PROJECT_FILE=MyTest.vcxproj"

if not exist "%PROJECT_FILE%" (
    echo 错误: 未找到项目文件 %PROJECT_FILE%
    echo.
    pause
    exit /b 1
)

echo 项目文件: %PROJECT_FILE%
echo.

REM 询问用户选择平台
echo 请选择编译目标平台:
echo 1. Win32 (32位) - 适用于旧版通达信
echo 2. x64   (64位) - 适用于新版64位通达信
echo.
set /p PLATFORM_CHOICE=请输入选项 (1 或 2): 

if "%PLATFORM_CHOICE%"=="1" (
    set "PLATFORM=Win32"
    set "PLATFORM_NAME=Win32 (32位)"
    set "OUTPUT_DIR=Release"
) else if "%PLATFORM_CHOICE%"=="2" (
    set "PLATFORM=x64"
    set "PLATFORM_NAME=x64 (64位)"
    set "OUTPUT_DIR=x64\Release"
) else (
    echo 错误: 无效选项，默认使用 Win32
    set "PLATFORM=Win32"
    set "PLATFORM_NAME=Win32 (32位)"
    set "OUTPUT_DIR=Release"
)

echo.
echo 正在编译 Release %PLATFORM_NAME% 版本...
echo.
"%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe" "%PROJECT_FILE%" -p:Configuration=Release -p:Platform=%PLATFORM%

if errorlevel 1 (
    echo.
    echo ========================================
    echo  编译失败!
    echo ========================================
    echo.
    echo 可能的原因:
    echo 1. 未安装Windows SDK
    echo 2. 未安装C++工作负载
    echo 3. 代码存在语法错误
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  编译成功!
echo ========================================
echo.
echo 输出文件: %OUTPUT_DIR%\MyTest.dll
echo.
echo 使用方法:
echo 1. 将 %OUTPUT_DIR%\MyTest.dll 复制到通达信目录
echo 2. 打开通达信,按 Ctrl+F 进入公式管理器
echo 3. 点击【DLL函数】→ 选择编号 → 【打开绑定】
echo 4. 选择生成的DLL文件
echo.
echo 注意:
echo - 32位通达信请使用 Win32 版本
echo - 64位通达信请使用 x64 版本
echo.
pause
