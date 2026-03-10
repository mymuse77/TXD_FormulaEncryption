@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo  通达信DLL插件工具编译脚本
echo ========================================
echo.

:: 检查Visual Studio环境
set "VS_PATH="
set "VCVARS="

:: 查找VS2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
    echo [OK] 找到 Visual Studio 2022 Professional
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
    echo [OK] 找到 Visual Studio 2022 Community
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
    echo [OK] 找到 Visual Studio 2022 Enterprise
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat"
    echo [OK] 找到 Visual Studio 2019 Professional
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"
    echo [OK] 找到 Visual Studio 2019 Community
) else (
    echo [错误] 未找到Visual Studio编译环境！
    echo.
    echo 请确保已安装以下组件：
    echo   - Visual Studio 2019 或 2022
    echo   - 使用C++的桌面开发 工作负载
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  正在初始化编译环境...
echo ========================================
echo.

:: 调用vcvars64.bat设置环境
call "%VCVARS%"

if errorlevel 1 (
    echo [错误] 编译环境初始化失败！
    pause
    exit /b 1
)

echo [OK] 编译环境初始化完成
echo.

:: 创建输出目录
if not exist "..\tools\bin" mkdir "..\tools\bin"

echo ========================================
echo  正在编译指纹生成工具...
echo ========================================
echo.

:: 编译MachineIDGenerator.cpp
cl.exe "..\tools\MachineIDGenerator.cpp" ^
    /Fo:"..\tools\bin\MachineIDGenerator.obj" ^
    /Fe:"..\tools\bin\MachineIDGenerator.exe" ^
    /EHsc ^
    /O2 ^
    /MT ^
    /nologo ^
    iphlpapi.lib ^
    ws2_32.lib

if errorlevel 1 (
    echo.
    echo [错误] 编译失败！
    echo.
    echo 常见原因：
    echo 1. MachineIDGenerator.cpp 文件不存在
    echo 2. 缺少必要的Windows SDK
    echo 3. 编译器权限不足
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] 编译成功！
echo.

:: 复制DLL到bin目录
echo ========================================
echo  正在复制DLL文件...
echo ========================================
echo.

set "DLL_SOURCE=..\test-output-binding3\x64\Release\MyTdxPlugin.dll"

if exist "%DLL_SOURCE%" (
    copy "%DLL_SOURCE%" "..\tools\bin\" >nul
    echo [OK] 已复制 MyTdxPlugin.dll
) else (
    echo [警告] 未找到MyTdxPlugin.dll，请先编译DLL项目
)

echo.
echo ========================================
echo  编译完成！
echo ========================================
echo.
echo 输出文件位置：
echo   ..\tools\bin\
echo.
echo 包含文件：
echo   - MachineIDGenerator.exe  (指纹生成工具)
echo   - MyTdxPlugin.dll         (主程序DLL，如已编译)
echo.
echo ========================================
echo  使用说明：
echo ========================================
echo.
echo 1. 运行指纹工具获取机器指纹：
echo    .\tools\bin\MachineIDGenerator.exe
echo.
echo 2. 将生成的指纹发送给开发者
echo.
echo 3. 开发者根据指纹生成license.dat
echo.
echo 4. 用户将以下文件放入通达信目录：
echo    C:\new_tdx64\T0002\dlls\
echo      - MyTdxPlugin.dll
echo      - license.dat
echo.
echo ========================================

pause
