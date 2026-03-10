#!/bin/bash
# 在Git Bash中编译MachineIDGenerator
# 注意：此脚本需要在已设置VS环境的终端中运行

echo "========================================"
echo "  编译机器指纹生成工具"
echo "========================================"
echo ""

# 切换到工具目录
cd "$(dirname "$0")"

# 检查源文件
if [ ! -f "MachineIDGenerator.cpp" ]; then
    echo "[错误] 找不到源文件: MachineIDGenerator.cpp"
    exit 1
fi

echo "[信息] 源文件: MachineIDGenerator.cpp"
echo ""

# 尝试找到cl.exe
CL_PATH=""

# 检查常见路径
if [ -f "/c/Program Files/Microsoft Visual Studio/2022/Professional/VC/Tools/MSVC/14.44.35207/bin/Hostx86/x64/cl.exe" ]; then
    CL_PATH="/c/Program Files/Microsoft Visual Studio/2022/Professional/VC/Tools/MSVC/14.44.35207/bin/Hostx86/x64/cl.exe"
elif [ -f "/c/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/bin/Hostx86/x64/cl.exe" ]; then
    CL_PATH="/c/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/bin/Hostx86/x64/cl.exe"
fi

if [ -z "$CL_PATH" ]; then
    echo "[错误] 找不到cl.exe编译器！"
    echo ""
    echo "请使用以下方法之一："
    echo ""
    echo "方法1：在Windows命令提示符中运行"
    echo "   1. 按Win+R，输入cmd，回车"
    echo "   2. cd /d \"D:\AI\Test\opencode\tdx\TdxDllGenerator\tools\""
    echo "   3. compile.bat"
    echo ""
    echo "方法2：使用Developer Command Prompt"
    echo "   1. 开始菜单 → Developer Command Prompt for VS 2022"
    echo "   2. cd /d \"D:\AI\Test\opencode\tdx\TdxDllGenerator\tools\""
    echo "   3. cl MachineIDGenerator.cpp /o MachineIDGenerator.exe /EHsc /O2 /MT"
    echo ""
    exit 1
fi

echo "[OK] 找到编译器: $CL_PATH"
echo ""

# 编译
echo "[步骤] 正在编译..."
echo ""

"$CL_PATH" MachineIDGenerator.cpp \
    /Fe:MachineIDGenerator.exe \
    /EHsc \
    /O2 \
    /MT \
    /nologo \
    iphlpapi.lib \
    ws2_32.lib

if [ $? -ne 0 ]; then
    echo ""
    echo "[错误] 编译失败！"
    echo ""
    echo "建议使用方法1或方法2（在Windows cmd中运行）"
    exit 1
fi

echo ""
echo "========================================"
echo "  编译成功！"
echo "========================================"
echo ""
echo "输出文件: MachineIDGenerator.exe"
echo ""

if [ -f "MachineIDGenerator.exe" ]; then
    ls -lh MachineIDGenerator.exe
fi

echo ""
read -p "按回车键退出..."
