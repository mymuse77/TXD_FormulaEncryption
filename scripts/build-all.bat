@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo  通达信DLL插件完整编译脚本
echo ========================================
echo.
echo 本脚本将编译以下内容：
echo   1. 指纹生成工具 (MachineIDGenerator.exe)
echo   2. DLL插件 (MyTdxPlugin.dll)
echo.

:: 检查Visual Studio环境
set "VCVARS="

if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
    set "VS_NAME=VS2022 Professional"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
    set "VS_NAME=VS2022 Community"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat"
    set "VS_NAME=VS2019 Professional"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat" (
    set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"
    set "VS_NAME=VS2019 Community"
) else (
    echo [错误] 未找到Visual Studio！
    echo 请安装VS2019或VS2022的"使用C++的桌面开发"工作负载
    pause
    exit /b 1
)

echo [OK] 找到编译环境: %VS_NAME%
echo.

:: 调用vcvars64.bat
call "%VCVARS%" >nul 2>&1
if errorlevel 1 (
    echo [错误] 编译环境初始化失败！
    pause
    exit /b 1
)

echo [OK] 编译环境初始化完成
echo.

:: 设置项目路径
set "PROJECT_ROOT=%~dp0.."
cd /d "%PROJECT_ROOT%"

:: 创建输出目录
if not exist "dist" mkdir "dist"
if not exist "dist\x64" mkdir "dist\x64"
if not exist "dist\tools" mkdir "dist\tools"

:: ========================================
echo ========================================
echo  步骤1: 编译指纹生成工具
echo ========================================
echo.

if not exist "tools\MachineIDGenerator.cpp" (
    echo [错误] 找不到 tools\MachineIDGenerator.cpp
    pause
    exit /b 1
)

cl.exe "tools\MachineIDGenerator.cpp" ^
    /Fo:"dist\tools\MachineIDGenerator.obj" ^
    /Fe:"dist\tools\MachineIDGenerator.exe" ^
    /EHsc /O2 /MT /nologo ^
    iphlpapi.lib ws2_32.lib

if errorlevel 1 (
    echo [错误] 指纹工具编译失败！
    pause
    exit /b 1
)

echo [OK] MachineIDGenerator.exe 编译成功
echo.

:: ========================================
echo ========================================
echo  步骤2: 编译DLL插件
echo ========================================
echo.

if not exist "test-output-binding3\MyTdxPlugin.cpp" (
    echo [错误] 找不到DLL源文件，请先运行生成器
    echo.
    echo 运行命令：
    echo   dotnet run -- generate -c test-config.json -o ./test-output-binding3
    pause
    exit /b 1
)

cd "test-output-binding3"

:: 使用MSBuild编译
echo 正在编译MyTdxPlugin.dll (x64 Release)...
MSBuild.exe MyTdxPlugin.vcxproj ^
    /p:Configuration=Release ^
    /p:Platform=x64 ^
    /p:WindowsTargetPlatformVersion=10.0.22621.0 ^
    /nologo ^
    /verbosity:quiet

if errorlevel 1 (
    echo [错误] DLL编译失败！
    cd ..
    pause
    exit /b 1
)

cd ..

:: 复制DLL到dist目录
copy "test-output-binding3\x64\Release\MyTdxPlugin.dll" "dist\x64\" >nul
copy "test-output-binding3\x64\Release\MyTdxPlugin.lib" "dist\x64\" >nul

echo [OK] MyTdxPlugin.dll 编译成功
echo.

:: ========================================
echo ========================================
echo  步骤3: 创建示例授权文件
echo ========================================
echo.

:: 获取当前机器指纹
echo 正在获取本机指纹...
for /f "tokens=*" %%a in ('"dist\tools\MachineIDGenerator.exe" -console') do (
    set "FINGERPRINT=%%a"
)

:: 创建示例license.dat
echo {> "dist\x64\license.dat"
echo   "license": {>> "dist\x64\license.dat"
echo     "version": "1.0",>> "dist\x64\license.dat"
echo     "user": "测试用户",>> "dist\x64\license.dat"
echo     "email": "test@example.com",>> "dist\x64\license.dat"
echo     "machineFingerprint": "!FINGERPRINT!",>> "dist\x64\license.dat"
echo     "issueDate": "%date:~0,4%-%date:~5,2%-%date:~8,2%",>> "dist\x64\license.dat"
echo     "expireDate": "2025-12-31",>> "dist\x64\license.dat"
echo     "maxDevices": 1,>> "dist\x64\license.dat"
echo     "features": ["MA", "EMA", "RSI"]>> "dist\x64\license.dat"
echo   }>> "dist\x64\license.dat"
echo }>> "dist\x64\license.dat"

echo [OK] license.dat 创建成功
echo.

:: ========================================
echo ========================================
echo  编译完成！
echo ========================================
echo.
echo 输出目录: %CD%\dist\
echo.
echo 文件清单:
echo   dist\tools\MachineIDGenerator.exe   (指纹工具)
echo   dist\x64\MyTdxPlugin.dll            (主程序DLL)
echo   dist\x64\MyTdxPlugin.lib            (导入库)
echo   dist\x64\license.dat                (示例授权文件)
echo.
echo ========================================
echo  下一步操作：
echo ========================================
echo.
echo 1. 分发文件：
echo    - 给用户: MachineIDGenerator.exe
echo    - 给用户: MyTdxPlugin.dll
echo.
echo 2. 用户操作：
echo    a) 运行 MachineIDGenerator.exe 获取指纹
echo    b) 将指纹发送给开发者
echo.
echo 3. 开发者操作：
echo    a) 根据用户指纹更新 license.dat
echo    b) 将 license.dat 发给用户
echo.
echo 4. 用户安装：
echo    将 MyTdxPlugin.dll + license.dat 复制到：
echo    C:\new_tdx64\T0002\dlls\
echo.
echo ========================================

pause
