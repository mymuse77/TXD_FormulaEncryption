# 配置文件示例

本目录包含 TdxDllGenerator 的各种配置文件示例。

## 示例文件列表

### 1. basic-config.json - 基础指标示例

**适用场景**：初次使用，了解基本内置指标

**包含内容**：
- MA (移动平均线) - 带参数
- EMA (指数移动平均) - 带参数
- RSI (相对强弱指标) - 带参数
- MACD - 无参数

**使用**：
```bash
dotnet run -- validate -c examples/basic-config.json
dotnet run -- generate -c examples/basic-config.json -o ./BasicPlugin
```

### 2. expression-config.json - 表达式公式示例 ⭐ 推荐

**适用场景**：使用通达信风格的公式语法，无需编写C++代码

**包含内容**：
- **布林带上轨**：`MA(CLOSE, 20) + STD(CLOSE, 20) * 2`
- **布林带中轨**：`MA(CLOSE, 20)`
- **布林带下轨**：`MA(CLOSE, 20) - STD(CLOSE, 20) * 2`
- **价格动量**：`(CLOSE - REF(CLOSE, 10)) / REF(CLOSE, 10) * 100`
- **成交量均线**：`MA(VOL, 20)`
- **金叉信号**：`CROSS(MA(CLOSE, 5), MA(CLOSE, 10))`
- **类RSI指标**：复杂条件判断示例
- **威廉指标**：`HHV`, `LLV` 函数示例
- **EMA金叉**：`CROSS(EMA(CLOSE, 12), EMA(CLOSE, 26))`
- **价格偏离度**：`(CLOSE - MA(CLOSE, 20)) / MA(CLOSE, 20) * 100`

**使用**：
```bash
# 验证配置
dotnet run -- validate -c examples/expression-config.json

# 生成项目
dotnet run -- generate -c examples/expression-config.json -o ./ExpressionPlugin

# 编译（需要Visual Studio）
cd ExpressionPlugin
build.bat
```

**通达信公式调用示例**：
```
// 布林带上轨
BB_UPPER: TDXDLL1(1, CLOSE, 0, 0);

// 金叉信号（返回1表示金叉，0表示无）
GOLDEN_CROSS: TDXDLL1(6, CLOSE, 0, 0);

// 威廉指标
WR: TDXDLL1(8, HIGH, CLOSE, LOW);
```

### 3. custom-code-config.json - 自定义C++代码示例

**适用场景**：需要自定义算法或优化性能

**包含内容**：
- 使用 CUSTOM 类型
- 编写原生 C++ 代码实现自定义策略
- 访问 `pfIn1`, `pfIn2`, `pfIn3`, `pfOut` 数组

**使用**：
```bash
dotnet run -- generate -c examples/custom-code-config.json -o ./CustomPlugin
```

**注意事项**：
- 需要了解C++编程
- 需要处理数组边界（`0 <= i < nDataLen`）
- 可以访问通达信传入的3个输入数组

### 4. security-config.json - 安全功能示例

**适用场景**：需要保护DLL不被滥用

**包含内容**：
- **时间限制**：设置过期日期
- **设备绑定**：绑定特定机器
- **网络验证**：在线授权验证

**配置说明**：
```json
{
  "security": {
    "enableTimeLimit": true,      // 启用时间限制
    "expireYear": 2026,
    "expireMonth": 12,
    "expireDay": 31,
    "enableDeviceBinding": true,  // 启用设备绑定
    "enableOnlineVerify": true,   // 启用网络验证
    "verifyUrl": "https://your-server.com/verify"
  }
}
```

**使用**：
```bash
dotnet run -- generate -c examples/security-config.json -o ./SecurePlugin
```

**设备绑定流程**：
1. 生成授权申请文件
2. 用户发送机器指纹给开发者
3. 开发者生成 license.dat
4. 用户放置 license.dat 到DLL同级目录

## 快速对比

| 示例文件 | 难度 | 灵活性 | 适用场景 |
|---------|------|-------|---------|
| basic-config.json | ⭐ 简单 | ⭐⭐ 固定指标 | 快速上手 |
| **expression-config.json** | ⭐⭐ 中等 | ⭐⭐⭐ 组合指标 | **推荐常用** |
| custom-code-config.json | ⭐⭐⭐ 困难 | ⭐⭐⭐⭐⭐ 任意算法 | 复杂策略 |
| security-config.json | ⭐⭐ 中等 | - | 授权保护 |

## 如何选择

### 如果你是新手
1. 从 `basic-config.json` 开始，了解基本流程
2. 尝试修改参数，如均线周期
3. 然后使用 `expression-config.json` 学习公式语法

### 如果你熟悉通达信公式
直接使用 `expression-config.json`，修改表达式即可：
```json
{
  "formulaType": "EXPRESSION",
  "expression": "你的通达信公式"
}
```

### 如果你需要复杂算法
参考 `custom-code-config.json` 编写C++代码。

### 如果你要分发DLL
使用 `security-config.json` 添加时间限制或设备绑定。

## 配置组合建议

### 方案1：纯表达式（推荐）
```json
{
  "pluginName": "MyStrategy",
  "functions": [
    { "formulaType": "EXPRESSION", "expression": "..." },
    { "formulaType": "EXPRESSION", "expression": "..." }
  ]
}
```

### 方案2：混合使用
```json
{
  "pluginName": "MixedStrategy",
  "functions": [
    { "formulaType": "MA" },           // 简单指标
    { "formulaType": "EXPRESSION" },   // 复杂组合
    { "formulaType": "CUSTOM" }        // 特殊算法
  ]
}
```

### 方案3：受保护的分发
```json
{
  "pluginName": "PremiumStrategy",
  "functions": [
    { "formulaType": "EXPRESSION", "expression": "..." }
  ],
  "security": {
    "enableTimeLimit": true,
    "expireYear": 2026,
    "enableDeviceBinding": true
  }
}
```

## 更多帮助

- 查看 `docs/EXPRESSION_GUIDE.md` 获取表达式语法详细说明
- 查看项目根目录 `README.md` 获取完整文档
- 使用 `dotnet run -- --help` 查看命令行帮助

## 常见问题

**Q: EXPRESSION 和 CUSTOM 有什么区别？**

A: 
- EXPRESSION：使用预定义的函数和语法，简单但功能受限
- CUSTOM：直接写C++代码，灵活但需要编程知识

**Q: 表达式可以做什么？**

A: 可以组合使用 MA, EMA, STD, HHV, LLV, REF 等标准技术指标函数，支持算术运算和逻辑判断。

**Q: 表达式不能做什么？**

A: 不能：
- 访问超过3个输入数据源
- 输出多个结果（只能输出单个值）
- 使用循环或变量
- 访问网络或文件

这些限制可以通过 CUSTOM 类型突破。

**Q: 如何验证表达式语法？**

A: 使用 validate 命令：
```bash
dotnet run -- validate -c examples/expression-config.json
```

会检查表达式语法是否正确。
