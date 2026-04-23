# TdxDllGenerator 完整操作手册

**版本**: v1.0  
**最后更新**: 2026-04-18  
**适用对象**: 通达信插件开发者

---

## 📋 目录

1. [环境准备](#一环境准备)
2. [获取项目](#二获取项目)
3. [创建配置文件](#三创建配置文件)
4. [编辑配置](#四编辑配置)
5. [验证配置](#五验证配置)
6. [生成项目](#六生成项目)
7. [编译 DLL](#七编译-dll)
8. [通达信配置](#八通达信配置)
9. [公式调用](#九公式调用)
10. [更新 DLL](#十更新-dll)
11. [常见问题](#十一常见问题)

---

## 一、环境准备

### 1.1 安装 .NET 8.0 SDK

**Windows 系统：**

1. 访问 https://dotnet.microsoft.com/download/dotnet/8.0
2. 下载 **.NET SDK**（不是 Runtime）
3. 运行安装程序，一路下一步即可
4. 安装完成后，打开 CMD 验证：

```cmd
dotnet --version
```

应该显示类似 `8.0.xxx` 的版本号。

**macOS/Linux：**

```bash
# macOS (使用 Homebrew)
brew install dotnet-sdk@8.0

# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# 验证
dotnet --version
```

### 1.2 安装编译工具（二选一）

**方式 A：Visual Studio 2022（推荐）**

1. 下载地址：https://visualstudio.microsoft.com/zh-hans/vs/
2. 安装时勾选：
   - ✅ **使用 C++ 的桌面开发**
   - ✅ **Windows 11 SDK** 或 **Windows 10 SDK**

**方式 B：MinGW-w64（无需 VS）**

1. 下载：https://github.com/brechtsanders/winlibs_mingw/releases
2. 选择 `x86_64-posix-seh` 版本
3. 解压到 `C:\mingw64`
4. 添加到环境变量：
   ```cmd
   setx PATH "%PATH%;C:\mingw64\bin"
   ```

---

## 二、获取项目

### 2.1 下载项目

**方式 A：Git 克隆（推荐）**

```cmd
git clone https://github.com/your-repo/TdxDllGenerator.git
cd TdxDllGenerator
```

**方式 B：直接下载 ZIP**

1. 下载项目 ZIP 包
2. 解压到任意目录，如 `D:\TdxDllGenerator`
3. 打开 CMD 进入目录：
   ```cmd
   cd D:\TdxDllGenerator
   ```

### 2.2 验证项目

```cmd
dotnet --version
dir
```

应该能看到 `src/` 目录和 `README.md` 文件。

---

## 三、创建配置文件

### 3.1 使用 init 命令创建默认配置

```cmd
dotnet run --project src\TdxDllGenerator.csproj -- init -o myconfig.json
```

这会创建一个名为 `myconfig.json` 的默认配置文件。

### 3.2 或使用示例配置

```cmd
# 复制示例配置
copy examples\expression-config.json myconfig.json
```

---

## 四、编辑配置

### 4.1 使用文本编辑器打开

使用 VS Code、Notepad++ 或记事本打开 `myconfig.json`。

### 4.2 配置文件结构说明

```json
{
  "pluginName": "MyIndicators",           // DLL文件名（英文）
  "pluginDescription": "我的指标插件",     // 插件描述
  "functions": [                          // 函数列表
    {
      "id": 1,                            // 函数ID（1-1000）
      "name": "MyMA",                     // 函数名（英文）
      "description": "20日均线",           // 函数描述
      "formulaType": "EXPRESSION",        // 公式类型
      "expression": "MA(CLOSE, 20)"       // 表达式（仅EXPRESSION类型需要）
    }
  ],
  "security": {                           // 安全设置（可选）
    "enableTimeLimit": false,             // 是否启用时间限制
    "expireYear": 2026,
    "expireMonth": 12,
    "expireDay": 31,
    "enableDeviceBinding": false,         // 是否绑定设备
    "enableOnlineVerify": false           // 是否网络验证
  }
}
```

```json
{
  "pluginName": "MyIndicators",          
  "pluginDescription": "我的指标插件",    
  "functions": [                          
    {
      "id": 1,                           
      "name": "MyMA",                    
      "description": "20日均线",          
      "formulaType": "EXPRESSION",        
      "expression": "MA(CLOSE, 20)"      
    }
  ],
  "security": {                          
    "enableTimeLimit": false,           
    "expireYear": 2026,
    "expireMonth": 12,
    "expireDay": 31,
    "enableDeviceBinding": false,         
    "enableOnlineVerify": false          
  }
}
```

### 4.3 公式类型选择

| 类型 | 说明 | 示例 |
|-----|------|------|
| **MA** | 简单移动平均线 | 参数：period |
| **EMA** | 指数移动平均线 | 参数：period |
| **RSI** | 相对强弱指标 | 参数：period |
| **MACD** | MACD指标 | 固定参数 12,26,9 |
| **EXPRESSION** | **表达式公式（推荐）** | expression字段 |
| **CUSTOM** | 自定义C++代码 | customCode字段 |

### 4.4 完整配置示例

**示例 1：单均线指标**

```json
{
  "pluginName": "SimpleMA",
  "pluginDescription": "简单均线指标",
  "functions": [
    {
      "id": 1,
      "name": "MA20",
      "description": "20日均线",
      "formulaType": "MA",
      "parameters": [
        {
          "name": "period",
          "defaultValue": 20,
          "description": "均线周期"
        }
      ]
    }
  ],
  "security": {
    "enableTimeLimit": false
  }
}
```

**示例 2：表达式公式（推荐）**

```json
{
  "pluginName": "MyStrategy",
  "pluginDescription": "我的交易策略",
  "functions": [
    {
      "id": 1,
      "name": "GoldenCross",
      "description": "金叉信号",
      "formulaType": "EXPRESSION",
      "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10))"
    },
    {
      "id": 2,
      "name": "BollingerUpper",
      "description": "布林带上轨",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
    },
    {
      "id": 3,
      "name": "PriceMomentum",
      "description": "价格动量",
      "formulaType": "EXPRESSION",
      "expression": "(CLOSE - REF(CLOSE, 10)) / REF(CLOSE, 10) * 100"
    }
  ],
  "security": {
    "enableTimeLimit": false
  }
}
```

**示例 3：带时间限制的授权版本**

```json
{
  "pluginName": "PremiumIndicators",
  "pluginDescription": "高级指标（授权版）",
  "functions": [
    {
      "id": 1,
      "name": "SecretStrategy",
      "description": "核心策略",
      "formulaType": "EXPRESSION",
      "expression": "MA(CLOSE, 5) > MA(CLOSE, 10) AND VOL > MA(VOL, 20)"
    }
  ],
  "security": {
    "enableTimeLimit": true,
    "expireYear": 2026,
    "expireMonth": 12,
    "expireDay": 31,
    "enableDeviceBinding": true,
    "enableOnlineVerify": false
  }
}
```

### 4.5 支持的表达式函数

| 函数 | 说明 | 示例 |
|-----|------|------|
| `CLOSE` / `C` | 收盘价 | `MA(CLOSE, 20)` |
| `OPEN` / `O` | 开盘价 | `OPEN > CLOSE` |
| `HIGH` / `H` | 最高价 | `HHV(HIGH, 20)` |
| `LOW` / `L` | 最低价 | `LLV(LOW, 20)` |
| `VOL` / `V` | 成交量 | `MA(VOL, 20)` |
| `MA(X, N)` | N日移动平均 | `MA(CLOSE, 20)` |
| `EMA(X, N)` | N日指数平均 | `EMA(CLOSE, 12)` |
| `STD(X, N)` | N日标准差 | `STD(CLOSE, 20)` |
| `HHV(X, N)` | N日最高值 | `HHV(HIGH, 20)` |
| `LLV(X, N)` | N日最低值 | `LLV(LOW, 20)` |
| `REF(X, N)` | N周期前的值 | `REF(CLOSE, 1)` |
| `CROSS(A, B)` | A上穿B | `CROSS(MA5, MA10)` |
| `IF(C, A, B)` | 条件判断 | `IF(CLOSE > OPEN, 1, 0)` |
| `ABS(X)` | 绝对值 | `ABS(CLOSE - OPEN)` |
| `MAX(A, B)` | 最大值 | `MAX(CLOSE, OPEN)` |
| `MIN(A, B)` | 最小值 | `MIN(CLOSE, OPEN)` |

更多函数参考 `docs/EXPRESSION_GUIDE.md`

---

## 五、验证配置

### 5.1 验证命令

```cmd
dotnet run --project src\TdxDllGenerator.csproj -- validate -c myconfig.json
```

### 5.2 验证成功输出

```
========================================
  通达信DLL代码生成器
  TdxDllGenerator v1.0
========================================

正在验证配置...
✓ 插件名称: MyStrategy
✓ 函数数量: 3

✓ 配置验证通过，可以生成项目
```

### 5.3 常见错误及解决

**错误 1：表达式语法错误**
```
错误: 函数 'GoldenCross' 的表达式语法错误: [行 1, 列 15] 意外的字符: 'x'
```
**解决**：检查 expression 字段，可能是函数名拼写错误或括号不匹配。

**错误 2：函数ID重复**
```
错误: 函数ID 1 重复
```
**解决**：确保每个函数的 id 唯一。

**错误 3：缺少必要字段**
```
错误: 函数 'XXX' 使用 EXPRESSION 类型但 expression 为空
```
**解决**：为 EXPRESSION 类型添加 expression 字段。

---

## 六、生成项目

### 6.1 生成命令

```cmd
dotnet run --project src\TdxDllGenerator.csproj -- generate -c myconfig.json -o ./output
```

参数说明：
- `-c myconfig.json`：配置文件路径
- `-o ./output`：输出目录（会自动创建）

### 6.2 生成成功输出

```
========================================
  通达信DLL代码生成器
  TdxDllGenerator v1.0
========================================

正在加载配置: myconfig.json
正在验证配置...
✓ 插件名称: MyStrategy
✓ 函数数量: 3

开始生成项目...
输出目录: D:\TdxDllGenerator\output

已生成: PluginTCalcFunc.h
已生成: MyStrategy.cpp
已生成: MyStrategy.def
已生成: CMakeLists.txt
已生成: MyStrategy.vcxproj
已生成: build.bat (编译脚本)
已生成: README.md
项目已生成到: D:\TdxDllGenerator\output
插件名称: MyStrategy
函数数量: 3
目标架构: Win32 (32位)

========================================
  生成完成!
========================================
```

### 6.3 查看生成的文件

```cmd
cd output
dir
```

生成的文件：
- `MyStrategy.cpp` - C++ 源代码
- `MyStrategy.h` - 头文件
- `MyStrategy.def` - 导出定义
- `MyStrategy.vcxproj` - Visual Studio 项目文件
- `CMakeLists.txt` - CMake 配置
- `build.bat` - 自动编译脚本
- `README.md` - 编译说明

---

## 七、编译 DLL

### 7.1 方式一：使用 build.bat（最简单）

```cmd
cd output
build.bat
```

按提示选择平台：
- 输入 `1` 选择 Win32（32位，推荐，兼容性好）
- 输入 `2` 选择 x64（64位，仅新版通达信支持）

编译成功后会显示：
```
========================================
  编译成功!
========================================

输出文件: Release\MyStrategy.dll
```

### 7.2 方式二：使用 Visual Studio

1. 打开 `output/MyStrategy.vcxproj`
2. 在工具栏选择：
   - **Release**（而非 Debug）
   - **Win32** 或 **x64**（根据通达信版本）
3. 菜单栏：生成 → 生成解决方案
4. 生成的 DLL 在 `output/Release/MyStrategy.dll`

### 7.3 方式三：使用 CMake

```cmd
cd output
mkdir build
cd build
cmake .. -A Win32
cmake --build . --config Release
```

生成的 DLL 在 `output/build/bin/Release/MyStrategy.dll`

### 7.4 方式四：使用 MinGW（无需 VS）

```cmd
cd output
C:\mingw64\bin\g++.exe -shared -o MyTest.dll MyTest.cpp MyTest.def -m32 -O2 -static-libgcc -static-libstdc++
```

### 7.5 验证 DLL

检查 DLL 是否为 32 位：
```cmd
cd output\Release
dumpbin /headers MyStrategy.dll | findstr machine
```

应该显示 `14C machine (x86)`（32位）或 `8664 machine (x64)`（64位）。

---

## 八、通达信配置

### 8.1 复制 DLL 到通达信目录

1. 找到通达信安装目录，如：
   ```
   C:\通达信\T0002\dlls\
   ```

2. 复制生成的 DLL：
   ```
   copy output\Release\MyStrategy.dll "C:\通达信\T0002\dlls\"
   ```

### 8.2 绑定 DLL

1. 打开通达信软件
2. 按 `Ctrl + F` 打开公式管理器
3. 点击 **【DLL函数】** 按钮
4. 选择一个空闲的编号（1-10），如 **1**
5. 点击 **【打开绑定】**
6. 选择刚才复制的 `MyStrategy.dll`
7. 点击 **【确定】**

### 8.3 验证绑定成功

绑定成功后，界面会显示：
```
DLL1: MyStrategy (绑定成功)
```

---

## 九、公式调用

### 9.1 在公式管理器中新建公式

1. 按 `Ctrl + F` 打开公式管理器
2. 选择 **【技术指标】** 或 **【条件选股】**
3. 点击 **【新建】**
4. 输入公式代码

### 9.2 公式语法

**基本调用格式：**
```
输出名称: TDXDLL编号(函数ID, 参数1, 参数2, 参数3);
```

**示例 1：调用金叉信号**

配置文件中的函数：
```json
{
  "id": 1,
  "name": "GoldenCross",
  "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10))"
}
```

通达信公式：
```
金叉: TDXDLL1(1, CLOSE, 0, 0);
```

**示例 2：调用均线（带参数）**

配置文件中的函数：
```json
{
  "id": 2,
  "name": "MA20",
  "formulaType": "MA",
  "parameters": [{"name": "period", "defaultValue": 20}]
}
```

通达信公式：
```
均线: TDXDLL1(2, CLOSE, 20, 0);
```

**示例 3：调用威廉指标（多输入）**

配置文件中的函数：
```json
{
  "id": 3,
  "name": "WilliamsR",
  "expression": "(HHV(HIGH, 14) - CLOSE) / (HHV(HIGH, 14) - LLV(LOW, 14)) * 100"
}
```

通达信公式：
```
WR: TDXDLL1(3, HIGH, CLOSE, LOW);
```

### 9.3 参数说明

通达信调用格式：
```
TDXDLL编号(函数ID, pfIn1, pfIn2, pfIn3)
```

映射关系：
- pfIn1 → `CLOSE` / `C`
- pfIn2 → `OPEN` / `O` 和 `VOL` / `V`
- pfIn3 → `HIGH` / `H` 和 `LOW` / `L`

### 9.4 完整公式示例

**布林带交易系统：**
```
{参数}
N:=20;
P:=2;

{调用DLL}
中轨: TDXDLL1(1, CLOSE, N, 0);
上轨: TDXDLL1(2, CLOSE, N, P);
下轨: TDXDLL1(3, CLOSE, N, P);

{买卖信号}
买: CROSS(CLOSE, 下轨);
卖: CROSS(上轨, CLOSE);

{绘图}
STICKLINE(买, LOW, LOW*0.98, 2, 0), COLORRED;
STICKLINE(卖, HIGH, HIGH*1.02, 2, 0), COLORGREEN;
```

---

## 十、更新 DLL

### 10.1 修改配置

编辑 `myconfig.json`，修改公式或添加新函数。

### 10.2 重新生成和编译

```cmd
dotnet run --project src\TdxDllGenerator.csproj -- generate -c myconfig.json -o ./output
cd output
build.bat
```

### 10.3 替换 DLL

1. 关闭通达信（必须！）
2. 复制新的 DLL 覆盖旧文件
3. 重新打开通达信
4. 公式会自动生效

**注意**：如果提示"文件正在使用"，请先关闭通达信再复制。

---

## 十一、常见问题

### Q1: 编译报错"找不到 windows.h"

**原因**：未安装 Windows SDK 或 Visual Studio 不完整。

**解决**：
1. 打开 Visual Studio Installer
2. 修改安装，勾选：
   - ✅ 使用 C++ 的桌面开发
   - ✅ Windows 11/10 SDK
3. 重新编译

### Q2: DLL 绑定失败"无法加载 DLL"

**原因1**：DLL 是 64 位，但通达信是 32 位。

**解决**：编译时选择 **Win32 (x86)** 平台。

**原因2**：缺少依赖库。

**解决**：使用 Visual Studio 的 "Release" + "静态运行时库" 编译。

### Q3: 公式返回结果都是 0

**原因1**：表达式语法错误。

**解决**：运行验证命令检查：
```cmd
dotnet run -- validate -c myconfig.json
```

**原因2**：参数传递错误。

**解决**：检查通达信公式中的参数顺序，确保与配置文件一致。

### Q4: 表达式验证通过但运行结果不对

**原因**：通达信传入的数据格式与预期不符。

**调试方法**：
1. 先用简单表达式测试，如 `CLOSE` 或 `MA(CLOSE, 5)`
2. 确认数据正常后，再使用复杂表达式
3. 检查 REF、HHV、LLV 等函数的周期参数

### Q5: 如何调试 C++ 代码？

1. 使用 Visual Studio 打开 .vcxproj
2. 设置断点
3. 附加到通达信进程调试
4. 或者使用自定义公式输出中间值进行调试

### Q6: EXPRESSION 和内置类型的区别？

| 对比项 | EXPRESSION | MA/EMA/RSI |
|-------|-----------|-----------|
| 灵活性 | ⭐⭐⭐ 任意组合 | ⭐⭐ 固定算法 |
| 性能 | ⭐⭐⭐ 良好 | ⭐⭐⭐⭐⭐ 最优 |
| 易用性 | ⭐⭐⭐ 简单 | ⭐⭐⭐ 简单 |
| 适用场景 | 复杂指标组合 | 单一标准指标 |

### Q7: 如何保护 DLL 不被盗用？

1. 在配置中启用安全功能：
   ```json
   "security": {
     "enableTimeLimit": true,
     "expireYear": 2026,
     "enableDeviceBinding": true
   }
   ```

2. 分发 license.dat 授权文件

3. 使用加密的配置文件

### Q8: 支持哪些通达信版本？

- ✅ 通达信金融终端
- ✅ 通达信券商版（大部分）
- ✅ 通达信期权版
- ⚠️ 部分定制版可能不兼容

---

## 十二、命令速查表

```bash
# 验证配置
dotnet run --project src\TdxDllGenerator.csproj -- validate -c myconfig.json

# 生成项目
dotnet run --project src\TdxDllGenerator.csproj -- generate -c myconfig.json -o ./output

# 创建默认配置
dotnet run --project src\TdxDllGenerator.csproj -- init -o myconfig.json

# 查看帮助
dotnet run --project src\TdxDllGenerator.csproj -- --help
```

---

## 十三、相关文档

- [主 README.md](../README.md) - 项目介绍
- [表达式语法指南](../docs/EXPRESSION_GUIDE.md) - 详细函数参考
- [examples/README.md](../examples/README.md) - 配置示例说明

---

## 十四、技术支持

遇到问题？

1. 查看 `docs/EXPRESSION_GUIDE.md` 函数参考
2. 查看 `examples/` 目录的示例配置
3. 运行 `dotnet run -- validate` 验证配置
4. 检查生成的 C++ 代码（`output/*.cpp`）

---

**祝使用愉快！**
