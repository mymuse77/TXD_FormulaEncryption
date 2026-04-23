# 通达信DLL代码生成器 (TdxDllGenerator)

一个用C#开发的通达信DLL插件代码生成工具。通过简单的JSON/YAML配置文件，自动生成完整的C++ DLL项目，支持多种技术指标和加密保护功能。

## 功能特性

### 内置技术指标
- **MA** - 移动平均线
- **EMA** - 指数移动平均线  
- **RSI** - 相对强弱指标
- **MACD** - MACD指标
- **CUSTOM** - 自定义C++代码
- **EXPRESSION** - 通达信公式风格表达式 ⭐ 新功能

### 安全保护功能
- ✅ 时间限制验证
- ✅ 设备绑定（需自行实现硬件ID读取）
- ✅ 网络验证（需自行实现服务器通信）

### 生成的项目文件
- PluginTCalcFunc.h - 通达信接口头文件
- {PluginName}.cpp - 插件源代码
- {PluginName}.def - 模块定义文件
- {PluginName}.vcxproj - Visual Studio项目文件
- CMakeLists.txt - CMake构建文件
- README.md - 编译说明文档

## 快速开始

### 1. 安装依赖

```bash
# 克隆或下载项目
cd TdxDllGenerator

# 还原NuGet包
dotnet restore
```

### 2. 创建配置文件

```bash
# 生成默认配置
dotnet run -- init -o myconfig.json
```

### 3. 编辑配置

打开 `myconfig.json` 编辑您的公式配置：

**方式一：使用内置指标**
```json
{
  "pluginName": "MyIndicators",
  "pluginDescription": "我的技术指标",
  "functions": [
    {
      "id": 1,
      "name": "MyMA",
      "description": "自定义均线",
      "formulaType": "MA",
      "parameters": [
        {
          "name": "period",
          "defaultValue": 10,
          "description": "周期"
        }
      ]
    }
  ]
}
```

**方式二：使用表达式（推荐）**
```json
{
  "pluginName": "MyIndicators",
  "pluginDescription": "我的技术指标",
  "functions": [
    {
      "id": 1,
      "name": "BollingerUpper",
      "description": "布林带上轨",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
    },
    {
      "id": 2,
      "name": "GoldenCross",
      "description": "金叉信号",
      "formulaType": "EXPRESSION",
      "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10))"
    }
  ]
}
```

### 4. 生成项目

```bash
dotnet run -- generate -c myconfig.json -o ./output
```

### 5. 编译DLL

#### 方法A: Visual Studio (推荐)
1. 打开 `output/MyIndicators.vcxproj`
2. 选择 **Release** + **Win32**
3. 生成 → 生成解决方案

#### 方法B: CMake
```bash
cd output
mkdir build && cd build
cmake .. -A Win32
cmake --build . --config Release
```

### 6. 使用DLL

1. 复制生成的 `.dll` 到通达信目录：
   ```
   通达信安装目录\T0002\dlls\
   ```

2. 打开通达信，按 `Ctrl+F` 进入公式管理器

3. 点击【DLL函数】→ 选择编号 → 【打开绑定】→ 选择您的DLL

4. 在公式中使用：
   ```
   MA10: TDXDLL1(1, CLOSE, 10, 0);
   ```

## 配置文件详解

### 完整配置示例 (JSON)

**示例1：使用表达式（推荐）**
```json
{
  "pluginName": "ExpressionIndicators",
  "pluginDescription": "使用表达式的高级指标",
  "functions": [
    {
      "id": 1,
      "name": "BollingerUpper",
      "description": "布林带上轨",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
    },
    {
      "id": 2,
      "name": "PriceMomentum",
      "description": "价格动量",
      "formulaType": "EXPRESSION",
      "expression": "(CLOSE - REF(CLOSE, 10)) / REF(CLOSE, 10) * 100"
    },
    {
      "id": 3,
      "name": "GoldenCross",
      "description": "金叉信号",
      "formulaType": "EXPRESSION",
      "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10))"
    }
  ],
  "security": {
    "enableTimeLimit": false
  }
}
```

**示例2：混合使用多种类型**
```json
{
  "pluginName": "MyPremiumPlugin",
  "pluginDescription": "高级指标插件",
  "functions": [
    {
      "id": 1,
      "name": "PremiumMA",
      "description": "高级均线",
      "formulaType": "MA",
      "parameters": [
        {
          "name": "period",
          "defaultValue": 20,
          "description": "均线周期"
        }
      ]
    },
    {
      "id": 2,
      "name": "BollingerBand",
      "description": "布林带宽度",
      "formulaType": "EXPRESSION",
      "expression": "(MA(CLOSE, 20) + STD(CLOSE, 20) * 2) - (MA(CLOSE, 20) - STD(CLOSE, 20) * 2)"
    },
    {
      "id": 3,
      "name": "MyCustom",
      "description": "自定义算法",
      "formulaType": "CUSTOM",
      "customCode": "for(int i=0; i<nDataLen; i++) { pfOut[i]=pfIn1[i]*2; }"
    }
  ],
  "security": {
    "enableTimeLimit": true,
    "expireYear": 2025,
    "enableDeviceBinding": true,
    "enableOnlineVerify": true,
    "verifyUrl": "https://your-server.com/verify"
  }
}
```

### YAML配置格式

同样支持YAML格式：

```yaml
pluginName: MyYamlPlugin
pluginDescription: YAML配置示例
functions:
  - id: 1
    name: YamlMA
    description: YAML均线
    formulaType: MA
    parameters:
      - name: period
        defaultValue: 10
        description: 周期
security:
  enableTimeLimit: false
```

## 示例文件

项目 `examples/` 目录包含多个示例配置：

| 示例文件 | 说明 |
|---------|------|
| `basic-config.json` | 基础内置指标示例 (MA/EMA/RSI/MACD) |
| `custom-code-config.json` | 自定义C++代码示例 |
| `expression-config.json` | **表达式公式示例（推荐）** |
| `security-config.json` | 安全功能配置示例 |

使用示例：
```bash
# 验证表达式示例
dotnet run -- validate -c examples/expression-config.json

# 生成表达式项目
dotnet run -- generate -c examples/expression-config.json -o ./MyExpressionPlugin
```

## 命令行参数

```
TdxDllGenerator [command] [options]

Commands:
  generate    从配置文件生成DLL项目
  init        创建默认配置文件
  validate    验证配置文件

Options for generate:
  -c, --config <path>    配置文件路径 (必需)
  -o, --output <path>    输出目录 (默认: ./output)

Options for init:
  -o, --output <path>    输出文件路径 (默认: ./config.json)

Options for validate:
  -c, --config <path>    配置文件路径 (必需)
```

## 公式类型说明

| 类型 | 说明 | 参数 |
|------|------|------|
| MA | 简单移动平均线 | period: 周期 |
| EMA | 指数移动平均线 | period: 周期 |
| RSI | 相对强弱指标 | period: 周期 |
| MACD | MACD指标 | 无（固定12,26,9） |
| CUSTOM | 自定义代码 | customCode: C++代码 |
| **EXPRESSION** | **表达式公式** | **expression: 公式表达式** |

## 自定义代码编写

使用 `CUSTOM` 类型时，`customCode` 字段支持完整的C++代码：

```json
{
  "formulaType": "CUSTOM",
  "customCode": "    // 您的C++代码\n    for (int i = 0; i < nDataLen; i++)\n    {\n        pfOut[i] = pfIn1[i] * 2.0f;\n    }"
}
```

可用变量：
- `nDataLen`: 数据长度
- `pfOut`: 输出数组
- `pfIn1`, `pfIn2`, `pfIn3`: 输入数组

## 🆕 表达式公式（推荐）

**EXPRESSION** 类型允许你使用通达信风格的公式语法，无需编写C++代码：

### 基本示例

```json
{
  "id": 1,
  "name": "BollingerUpper",
  "description": "布林带上轨",
  "formulaType": "EXPRESSION",
  "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
}
```

### 支持的函数

#### 数据序列
- `CLOSE` / `C` - 收盘价
- `OPEN` / `O` - 开盘价
- `HIGH` / `H` - 最高价
- `LOW` / `L` - 最低价
- `VOL` / `V` - 成交量

#### 技术指标
- `MA(X, N)` - 简单移动平均
- `EMA(X, N)` - 指数移动平均
- `SMA(X, N, M)` - 加权移动平均
- `STD(X, N)` - 标准差
- `HHV(X, N)` - N周期最高值
- `LLV(X, N)` - N周期最低值
- `SUM(X, N)` - N周期求和
- `REF(X, N)` - 引用N周期前的值

#### 数学函数
- `ABS(X)` - 绝对值
- `MAX(A, B)` - 最大值
- `MIN(A, B)` - 最小值
- `SQRT(X)` - 平方根
- `POW(X, N)` - 幂运算

#### 逻辑函数
- `IF(COND, A, B)` - 条件判断
- `CROSS(A, B)` - 上穿判断
- `AND`, `OR`, `NOT` - 逻辑运算

### 运算符

支持 `+`, `-`, `*`, `/`, `%` 以及比较运算符 `>`, `<`, `>=`, `<=`, `=`, `<>`

### 完整示例：布林带

```json
{
  "pluginName": "BollingerBands",
  "functions": [
    {
      "id": 1,
      "name": "BBUpper",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
    },
    {
      "id": 2,
      "name": "BBMiddle", 
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20)"
    },
    {
      "id": 3,
      "name": "BBLower",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20) - STD(CLOSE, 20) * 2"
    }
  ]
}
```

### 表达式 vs CUSTOM

| 特性 | EXPRESSION | CUSTOM |
|-----|-----------|--------|
| 易用性 | ⭐⭐⭐ 简单 | ⭐ 需懂C++ |
| 灵活性 | ⭐⭐⭐ 标准指标 | ⭐⭐⭐⭐⭐ 任意代码 |
| 安全性 | ⭐⭐⭐ 自动验证 | ⭐⭐ 可能出错 |
| 性能 | ⭐⭐⭐ 良好 | ⭐⭐⭐⭐⭐ 最优 |
| 适合场景 | 标准指标组合 | 复杂算法 |

**建议**：优先使用 EXPRESSION，复杂逻辑使用 CUSTOM。

## 安全功能实现

### 时间限制
自动验证当前年份是否小于设定值：
```json
{
  "security": {
    "enableTimeLimit": true,
    "expireYear": 2025
  }
}
```

### 设备绑定
生成代码包含 `CheckMachineID()` 函数框架，需自行实现硬件ID验证逻辑。

### 网络验证
生成代码包含 `OnlineVerify()` 函数框架，需自行实现HTTP验证请求。

## 技术架构

```
TdxDllGenerator/
├── Program.cs          # 命令行入口
├── Models.cs           # 配置模型
├── ConfigLoader.cs     # 配置加载器(JSON/YAML)
└── CppCodeGenerator.cs # C++代码生成器

生成的DLL项目/
├── PluginTCalcFunc.h   # 通达信接口头文件
├── {Name}.cpp         # 实现代码
├── {Name}.def         # 导出定义
├── {Name}.vcxproj     # VS项目文件
└── CMakeLists.txt     # CMake配置
```

## 编译工具要求

生成DLL需要以下任一工具：

1. **Visual Studio 2019/2022** (推荐)
   - 安装"使用C++的桌面开发"工作负载

2. **MinGW-w64**
   ```bash
   x86_64-w64-mingw32-g++ -shared -o plugin.dll ...
   ```

3. **CMake 3.10+**
   ```bash
   cmake .. -A Win32
   ```

## 注意事项

1. **32位编译**: 通达信通常需要32位(x86)DLL，不是64位
2. **函数ID**: 建议使用1-100，避免与系统冲突
3. **参数传递**: 通过 `pfIn2[0]` 获取公式传入的第二个参数
4. **内存安全**: 确保数组访问不越界（`0 <= i < nDataLen`）

## 许可证

MIT License

## 问题反馈

如有问题，请检查：
1. 配置文件格式是否正确
2. 函数ID是否有重复
3. 编译时是否选择了Win32平台
4. DLL是否正确放到T0002/dlls目录
