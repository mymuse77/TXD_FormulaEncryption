# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 提供代码仓库的工作指导。

## 项目概述

**TdxDllGenerator** 是一个 C# CLI 工具，用于生成适用于通达信（一款中国股票/金融分析软件）的 C++ DLL 插件。它读取 JSON 配置文件并输出完整的 Visual Studio/CMake C++ 项目，包含技术指标和可选的安全功能（设备绑定、时间限制、在线验证）。

## 架构

### 项目结构
```
src/
├── Program.cs           # CLI 入口点，包含 3 个命令（generate, init, validate）
├── Models.cs            # 数据模型：TdxFormulaConfig, FunctionConfig, SecurityConfig
├── ConfigLoader.cs      # JSON 配置加载（仅支持 .json）
└── CppCodeGenerator.cs  # 生成 C++ 源文件、头文件、.vcxproj、CMakeLists.txt
```

### 核心类
- **TdxFormulaConfig**: 根配置，包含插件名称、函数列表、安全设置
- **FunctionConfig**: 单个指标函数（MA, EMA, RSI, MACD, CUSTOM）
- **CppCodeGenerator**: 生成所有输出文件，包括 Visual Studio 项目文件

### 支持的公式类型
- `MA` - 移动平均线（Moving Average）
- `EMA` - 指数移动平均线（Exponential Moving Average）
- `RSI` - 相对强弱指数（Relative Strength Index）
- `MACD` - MACD 指标（固定参数 12,26,9）
- `CUSTOM` - 用户自定义 C++ 代码

## 常用命令

### 构建与运行
```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 从源码运行
dotnet run -- <命令> [选项]

# 构建独立可执行文件
dotnet publish -c Release
```

### CLI 用法
```bash
# 创建默认配置文件
dotnet run -- init -o myconfig.json

# 从配置生成 C++ DLL 项目
dotnet run -- generate -c myconfig.json -o ./output

# 验证配置文件
dotnet run -- validate -c myconfig.json
```

### 完整开发工作流
```bash
# 1. 创建并编辑配置
dotnet run -- init -o myconfig.json
# ... 编辑 myconfig.json ...

# 2. 验证配置
dotnet run -- validate -c myconfig.json

# 3. 生成 C++ 项目
dotnet run -- generate -c myconfig.json -o ./MyPlugin

# 4. 构建生成的 DLL（需要 Visual Studio 或 CMake）
cd MyPlugin
# 在 Visual Studio 中打开 MyPlugin.vcxproj，选择 Release/Win32，然后构建
# 或使用 CMake：mkdir build && cd build && cmake .. -A Win32 && cmake --build . --config Release
```

## 配置格式

该工具接受具有以下结构的 JSON 配置文件：

```json
{
  "pluginName": "MyPlugin",
  "pluginDescription": "My indicators",
  "functions": [
    {
      "id": 1,
      "name": "MyMA",
      "description": "Custom MA",
      "formulaType": "MA",
      "parameters": [{ "name": "period", "defaultValue": 10, "description": "Period" }]
    }
  ],
  "security": {
    "enableTimeLimit": false,
    "expireYear": 2026,
    "enableDeviceBinding": false,
    "enableOnlineVerify": false,
    "verifyUrl": null
  }
}
```

## 重要说明

- **目标框架**: .NET 8.0
- **输出类型**: 独立单文件可执行文件（`PublishSingleFile=true`, `SelfContained=true`）
- **运行时标识符**: `win-x64`
- 生成的 DLL 应以 **Win32 (x86)** 为目标，以确保与 32 位通达信的兼容性
- `tools/` 目录包含用于设备绑定指纹生成的 `MachineIDGenerator.cpp`
- 详细中文使用说明请参见 `完整编译使用指南.md`
- 示例配置文件请参见 `examples/` 目录
