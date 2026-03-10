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
if "%~1"=="" (
    set "PROJECT_FILE=MyTdxPlugin.vcxproj"
) else (
    set "PROJECT_FILE=%~1"
)

if not exist "%PROJECT_FILE%" (
    echo 错误: 未找到项目文件 %PROJECT_FILE%
    echo.
    pause
    exit /b 1
)

echo 项目文件: %PROJECT_FILE%
echo.

REM 编译Release版本
echo 正在编译 Release Win32 版本...
echo.
"%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe" "%PROJECT_FILE%" -p:Configuration=Release -p:Platform=Win32 -p:WindowsTargetPlatformVersion=10.0

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
echo 输出文件: Release\MyTdxPlugin.dll
echo.
echo 使用方法:
echo 1. 将 Release\MyTdxPlugin.dll 复制到通达信目录
echo 2. 打开通达信,按 Ctrl+F 进入公式管理器
echo 3. 点击【DLL函数】→ 选择编号 → 【打开绑定】
echo 4. 选择生成的DLL文件
echo.
pause
