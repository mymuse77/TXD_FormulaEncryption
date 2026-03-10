#!/usr/bin/env python3
"""
机器指纹生成工具编译脚本
在Git Bash或其他bash环境中使用
"""

import subprocess
import sys
import os
from pathlib import Path


def find_vs_compiler():
    """查找Visual Studio编译器"""
    possible_paths = [
        # VS2022 Professional
        r"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.44.35207\bin\Hostx86\x64\cl.exe",
        r"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.43.34808\bin\Hostx86\x64\cl.exe",
        # VS2022 Community
        r"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.44.35207\bin\Hostx86\x64\cl.exe",
        r"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.43.34808\bin\Hostx86\x64\cl.exe",
        # VS2019 Professional
        r"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\bin\Hostx86\x64\cl.exe",
        # VS2019 Community
        r"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.29.30133\bin\Hostx86\x64\cl.exe",
    ]

    for path in possible_paths:
        if os.path.exists(path):
            return path

    return None


def find_sdk_include():
    """查找Windows SDK包含目录"""
    sdk_paths = [
        r"C:\Program Files (x86)\Windows Kits\10\Include\10.0.22621.0",
        r"C:\Program Files (x86)\Windows Kits\10\Include\10.0.22000.0",
        r"C:\Program Files (x86)\Windows Kits\10\Include\10.0.19041.0",
    ]

    for path in sdk_paths:
        if os.path.exists(path):
            return path

    return None


def main():
    print("=" * 50)
    print("  编译机器指纹生成工具")
    print("=" * 50)
    print()

    # 获取当前目录
    script_dir = Path(__file__).parent
    os.chdir(script_dir)

    source_file = "MachineIDGenerator.cpp"
    output_file = "MachineIDGenerator.exe"

    # 检查源文件
    if not os.path.exists(source_file):
        print(f"[错误] 找不到源文件: {source_file}")
        return 1

    print(f"[信息] 源文件: {source_file}")
    print()

    # 查找编译器
    print("[步骤1] 正在查找Visual Studio编译器...")
    cl_path = find_vs_compiler()

    if not cl_path:
        print()
        print("[错误] 找不到cl.exe编译器！")
        print()
        print("请使用以下方法之一：")
        print()
        print("方法1：在Windows命令提示符中运行")
        print("   1. 按Win+R，输入cmd，回车")
        print(f'   2. cd /d "{script_dir}"')
        print("   3. compile.bat")
        print()
        print("方法2：使用Developer Command Prompt")
        print("   1. 开始菜单 → Developer Command Prompt for VS 2022")
        print(f'   2. cd /d "{script_dir}"')
        print(
            "   3. cl MachineIDGenerator.cpp /o MachineIDGenerator.exe /EHsc /O2 /MT iphlpapi.lib ws2_32.lib"
        )
        print()
        input("按回车键退出...")
        return 1

    print(f"[OK] 找到编译器: {cl_path}")
    print()

    # 查找SDK
    sdk_path = find_sdk_include()
    if sdk_path:
        print(f"[OK] 找到Windows SDK: {sdk_path}")
        print()

    # 编译
    print("[步骤2] 正在编译...")
    print()

    # 构建编译命令
    cmd = [
        cl_path,
        source_file,
        f"/Fe:{output_file}",
        "/EHsc",
        "/O2",
        "/MT",
        "/nologo",
    ]

    # 添加包含目录
    if sdk_path:
        cmd.extend(
            [
                f"/I{sdk_path}\\um",
                f"/I{sdk_path}\\shared",
            ]
        )

    # 添加库
    cmd.extend(
        [
            "iphlpapi.lib",
            "ws2_32.lib",
        ]
    )

    print(f"命令: {' '.join(cmd)}")
    print()

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, shell=True)

        if result.returncode != 0:
            print(result.stdout)
            print(result.stderr)
            print()
            print("[错误] 编译失败！")
            print()
            print("建议：")
            print("1. 确保已安装Visual Studio 2019/2022")
            print("2. 确保安装了'使用C++的桌面开发'工作负载")
            print("3. 确保安装了Windows SDK")
            print()
            print("或者尝试在Developer Command Prompt中手动编译")
            print()
            input("按回车键退出...")
            return 1

        print(result.stdout)

        if os.path.exists(output_file):
            size = os.path.getsize(output_file)
            print()
            print("=" * 50)
            print("  编译成功！")
            print("=" * 50)
            print()
            print(f"输出文件: {output_file}")
            print(f"文件大小: {size:,} 字节")
            print()
            print("使用方法:")
            print("  1. 双击运行 MachineIDGenerator.exe")
            print("  2. 获取指纹后发送给开发者")
            print("  3. 开发者生成授权文件")
            print()
        else:
            print("[警告] 未找到输出文件")

    except Exception as e:
        print(f"[错误] 执行失败: {e}")
        return 1

    input("按回车键退出...")
    return 0


if __name__ == "__main__":
    sys.exit(main())
