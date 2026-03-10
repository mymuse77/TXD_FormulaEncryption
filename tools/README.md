# 机器指纹生成工具

## 概述

此工具用于获取计算机的唯一标识（机器指纹），用于通达信DLL插件的设备授权验证。

## 文件说明

| 文件 | 说明 |
|------|------|
| `MachineIDGenerator.cpp` | 源代码 |
| `compile.bat` | 完整版编译脚本（自动查找VS环境） |
| `build_simple.bat` | 简化版编译脚本（需在VS环境中运行） |
| `MachineIDGenerator.exe` | 编译后的可执行文件（生成后） |

## 编译方法

### 方法1：使用compile.bat（推荐）

1. 双击运行 `compile.bat`
2. 脚本会自动查找Visual Studio编译环境
3. 自动编译生成 `MachineIDGenerator.exe`

**要求：**
- 已安装Visual Studio 2019 或 2022
- 安装了"使用C++的桌面开发"工作负载

### 方法2：在Developer Command Prompt中编译

1. 打开开始菜单
2. 搜索 "Developer Command Prompt for VS 2022"
3. 右键选择"以管理员身份运行"
4. 切换到工具目录：
   ```cmd
   cd /d "D:\AI\Test\opencode\tdx\TdxDllGenerator\tools"
   ```
5. 运行简化编译脚本：
   ```cmd
   build_simple.bat
   ```
   或直接执行编译命令：
   ```cmd
   cl MachineIDGenerator.cpp /o MachineIDGenerator.exe /EHsc /O2 /MT iphlpapi.lib ws2_32.lib
   ```

### 方法3：使用Visual Studio IDE

1. 打开Visual Studio
2. 文件 → 新建 → 项目 → 空项目
3. 项目名称：`MachineIDGenerator`
4. 右键项目 → 添加 → 现有项 → 选择 `MachineIDGenerator.cpp`
5. 项目属性设置：
   - 配置：Release
   - 平台：x64
   - C/C++ → 代码生成 → 运行库：多线程(/MT)
6. 生成 → 生成解决方案

## 使用方法

### 获取机器指纹

1. 运行 `MachineIDGenerator.exe`
2. 查看显示的16位机器指纹，例如：
   ```
   您的机器指纹：
   
     A1B2C3D4E5F67890
   ```
3. 指纹已自动复制到剪贴板，可直接粘贴发送给开发者

### 命令行模式（用于脚本）

```cmd
MachineIDGenerator.exe any_argument
```

输出：仅显示指纹，无其他信息

## 授权流程

```
用户                         开发者
  │                            │
  │  1.运行MachineIDGenerator   │
  │    获取机器指纹             │
  │                            │
  │──────── 指纹 ─────────────>│
  │                            │
  │                            │  2.生成license.dat
  │                            │    (包含用户机器指纹)
  │                            │
  │<────── 授权文件 ───────────│
  │                            │
  │  3.将授权文件放入通达信目录  │
  │    T0002\dlls\              │
```

## 常见问题

### Q1: 编译时提示"cl.exe 不是内部或外部命令"

**原因：** 未在Visual Studio环境中运行

**解决：**
- 使用 `compile.bat` （自动查找VS环境）
- 或在"Developer Command Prompt for VS"中运行

### Q2: 编译时提示"无法打开包括文件: iphlpapi.h"

**原因：** 缺少Windows SDK

**解决：**
1. 打开Visual Studio Installer
2. 修改安装
3. 勾选"Windows 10 SDK" 或 "Windows 11 SDK"
4. 安装更新

### Q3: 运行后显示的指纹为0000000000000000

**原因：** 无法获取MAC地址

**解决：**
- 确保计算机有网卡
- 确保网卡驱动正常
- 尝试使用管理员身份运行

### Q4: 同一台机器每次运行显示的指纹不同

**原因：** MAC地址获取失败，使用了随机值

**解决：** 检查网卡状态，确保能正确读取MAC地址

## 技术说明

### 指纹生成算法

```cpp
// 1. 获取网卡MAC地址
unsigned char mac[6];
GetMacAddress(mac);

// 2. 计算哈希值
unsigned int hash = 0;
for (int i = 0; i < 6; i++) {
    hash = hash * 31 + mac[i];
}

// 3. 生成16位十六进制指纹
sprintf(fingerprint, "%08X%08X", hash, hash ^ 0xAAAAAAAA);
```

### 依赖库

- `iphlpapi.lib` - 用于获取网络适配器信息
- `ws2_32.lib` - Windows Socket库

### 兼容性

- Windows 7/8/10/11
- x64架构
- 需要管理员权限（某些环境）

## 分发说明

### 开发者需要提供的文件

```
授权工具包/
├── MachineIDGenerator.exe    # 指纹获取工具
├── 使用说明.txt               # 本说明文件
└── 示例/                      # 可选
    └── license.dat            # 授权文件示例
```

### 用户使用流程

1. 接收 `MachineIDGenerator.exe`
2. 双击运行，获取指纹
3. 将指纹发送给开发者
4. 等待开发者发送授权文件
5. 安装授权文件到通达信目录

## 更新日志

### v1.0 (2026-03-10)
- 初始版本
- 支持MAC地址读取
- 支持命令行模式
- 支持自动复制到剪贴板

## 技术支持

如有问题，请联系DLL开发者。
