@echo off
chcp 65001 >nul
echo ========================================
echo  编译机器指纹生成工具
echo ========================================
echo.

:: 设置路径
set "SOURCE=%~dp0MachineIDGenerator.cpp"
set "OUTPUT=%~dp0MachineIDGenerator.exe"

:: 检查源文件
if not exist "%SOURCE%" (
    echo [错误] 找不到源文件: %SOURCE%
    pause
    exit /b 1
)

echo [信息] 源文件: %SOURCE%
echo [信息] 输出文件: %OUTPUT%
echo.

:: 方法1: 尝试使用VS自带的vcvarsall.bat设置环境
echo [步骤1] 正在查找Visual Studio编译环境...
echo.

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "VCVARS="

if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -property installationPath`) do (
        if exist "%%i\VC\Auxiliary\Build\vcvars64.bat" (
            set "VCVARS=%%i\VC\Auxiliary\Build\vcvars64.bat"
            echo [OK] 找到 Visual Studio: %%i
        )
    )
)

:: 如果没找到，尝试常见路径
if "%VCVARS%"=="" (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
        set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
        echo [OK] 找到 VS2022 Professional
    ) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
        echo [OK] 找到 VS2022 Community
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat" (
        set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat"
        echo [OK] 找到 VS2019 Professional
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"
        echo [OK] 找到 VS2019 Community
    )
)

if "%VCVARS%"=="" (
    echo.
    echo [错误] 未找到Visual Studio编译环境！
    echo.
    echo 请确保以下任一条件满足：
    echo 1. 安装Visual Studio 2019/2022的"使用C++的桌面开发"工作负载
    echo 2. 手动运行"Developer Command Prompt for VS 2022"后再执行此脚本
    echo.
    echo 或者手动编译：
    echo   cl MachineIDGenerator.cpp /o MachineIDGenerator.exe /EHsc /O2 /MT iphlpapi.lib ws2_32.lib
    echo.
    pause
    exit /b 1
)

echo [OK] 使用编译环境: %VCVARS%
echo.

:: 调用vcvarsall.bat
echo [步骤2] 正在初始化编译环境...
call "%VCVARS%" >nul 2>&1

if errorlevel 1 (
    echo.
    echo [错误] 编译环境初始化失败！
    pause
    exit /b 1
)

echo [OK] 编译环境初始化完成
echo.

:: 编译
echo [步骤3] 正在编译...
echo.
echo 命令: cl "%SOURCE%" /o "%OUTPUT%" /EHsc /O2 /MT /nologo iphlpapi.lib ws2_32.lib
echo.

cl "%SOURCE%" /o "%OUTPUT%" /EHsc /O2 /MT /nologo iphlpapi.lib ws2_32.lib

if errorlevel 1 (
    echo.
    echo [错误] 编译失败！
    echo.
    echo 可能的错误原因：
    echo 1. 缺少Windows SDK
    echo 2. 源文件有语法错误
    echo 3. 编译器权限不足
    echo.
    echo 请尝试：
    echo 1. 打开"Developer Command Prompt for VS 2022"（以管理员身份）
    echo 2. 切换到本目录: cd /d "%~dp0"
    echo 3. 运行: cl MachineIDGenerator.cpp /o MachineIDGenerator.exe /EHsc /O2 /MT iphlpapi.lib ws2_32.lib
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  编译成功！
echo ========================================
echo.
echo 输出文件: %OUTPUT%
echo.

:: 验证文件
if exist "%OUTPUT%" (
    echo [OK] 文件生成成功
    for %%F in ("%OUTPUT%") do echo [信息] 文件大小: %%~zF 字节
) else (
    echo [警告] 未找到输出文件
)

echo.
echo 使用方法：
echo   1. 运行: MachineIDGenerator.exe
echo   2. 获取指纹后发送给开发者
echo   3. 开发者会根据此指纹生成授权文件
echo.
pause
