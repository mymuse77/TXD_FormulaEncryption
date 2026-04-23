# 表达式公式语法指南

通达信DLL代码生成器支持使用**通达信风格的表达式语法**来定义技术指标，无需编写C++代码。

## 目录

- [快速开始](#快速开始)
- [基本语法](#基本语法)
- [数据序列](#数据序列)
- [函数参考](#函数参考)
- [运算符](#运算符)
- [常见公式示例](#常见公式示例)
- [注意事项](#注意事项)

## 快速开始

### 最简单的表达式

```json
{
  "formulaType": "EXPRESSION",
  "expression": "CLOSE"
}
```

这会将收盘价直接输出。

### 移动平均线

```json
{
  "formulaType": "EXPRESSION",
  "expression": "MA(CLOSE, 20)"
}
```

计算收盘价的20日简单移动平均线。

### 布林带

```json
{
  "formulaType": "EXPRESSION",
  "expression": "MA(CLOSE, 20) + STD(CLOSE, 20) * 2"
}
```

计算布林带上轨：20日均线 + 2倍标准差。

## 基本语法

### 数值常量

```
10      // 整数
10.5    // 浮点数
0.5     // 小数
```

### 标识符

数据序列和函数名不区分大小写：
- `CLOSE` 和 `close` 相同
- `MA` 和 `ma` 相同

### 括号

使用括号控制运算优先级：

```
MA(CLOSE, 5) + MA(CLOSE, 10) * 2      // 先乘后加
(MA(CLOSE, 5) + MA(CLOSE, 10)) * 2    // 先加后乘
```

## 数据序列

| 序列 | 别名 | 说明 |
|-----|-----|------|
| `CLOSE` | `C` | 收盘价 |
| `OPEN` | `O` | 开盘价 |
| `HIGH` | `H` | 最高价 |
| `LOW` | `L` | 最低价 |
| `VOL` | `V` | 成交量 |
| `AMOUNT` | `AMO` | 成交额 |

**注意**：在通达信DLL中，CLOSE映射到pfIn1，OPEN和VOL映射到pfIn2，HIGH和LOW映射到pfIn3。

## 函数参考

### 技术指标函数

#### MA - 简单移动平均
```
MA(X, N)
```
计算X的N周期简单移动平均。

示例：`MA(CLOSE, 20)` - 20日均线

#### EMA - 指数移动平均
```
EMA(X, N)
```
计算X的N周期指数移动平均。

示例：`EMA(CLOSE, 12)` - 12日指数均线

#### SMA - 加权移动平均
```
SMA(X, N, M)
```
计算X的N周期加权移动平均，权重为M。

示例：`SMA(CLOSE, 20, 10)`

#### WMA - 加权移动平均
```
WMA(X, N)
```
计算X的N周期线性加权移动平均。

示例：`WMA(CLOSE, 10)`

#### STD - 标准差
```
STD(X, N)
```
计算X的N周期标准差。

示例：`STD(CLOSE, 20)` - 20日标准差

#### HHV - 最高值
```
HHV(X, N)
```
计算X在N周期内的最高值。

示例：`HHV(HIGH, 20)` - 20日最高价

#### LLV - 最低值
```
LLV(X, N)
```
计算X在N周期内的最低值。

示例：`LLV(LOW, 20)` - 20日最低价

#### SUM - 求和
```
SUM(X, N)
```
计算X在N周期内的总和。

示例：`SUM(VOL, 5)` - 5日成交量总和

#### REF - 引用
```
REF(X, N)
```
引用X在N周期前的值。

示例：`REF(CLOSE, 1)` - 昨日收盘价

### 数学函数

#### ABS - 绝对值
```
ABS(X)
```

#### MAX - 最大值
```
MAX(A, B)
```

#### MIN - 最小值
```
MIN(A, B)
```

#### MOD - 取模
```
MOD(A, B)
```

#### ROUND - 四舍五入
```
ROUND(X)      // 四舍五入到整数
ROUND(X, N)   // 四舍五入到N位小数
```

#### INTPART - 取整
```
INTPART(X)
```
取整数部分。

#### SQRT - 平方根
```
SQRT(X)
```

#### POW - 幂运算
```
POW(X, N)
```
计算X的N次方。

#### EXP - 指数函数
```
EXP(X)
```
计算e的X次方。

#### LN - 自然对数
```
LN(X)
```

#### LOG - 常用对数
```
LOG(X)
```

#### SIN / COS / TAN - 三角函数
```
SIN(X)
COS(X)
TAN(X)
```

### 逻辑函数

#### IF - 条件判断
```
IF(COND, A, B)
```
如果COND为真，返回A，否则返回B。

示例：`IF(CLOSE > OPEN, CLOSE, OPEN)` - 返回收盘价和开盘价的较大者

#### CROSS - 上穿判断
```
CROSS(A, B)
```
判断A是否上穿B（前一天A<=B，当天A>B）。

示例：`CROSS(MA(CLOSE, 5), MA(CLOSE, 10))` - 5日均线上穿10日均线（金叉）

#### AND / OR / NOT - 逻辑运算
```
COND1 AND COND2
COND1 OR COND2
NOT(COND)
```

示例：`CLOSE > MA(CLOSE, 20) AND VOL > MA(VOL, 20)`

### 统计函数

#### COUNT - 统计满足条件周期数
```
COUNT(COND, N)
```
统计N周期内满足COND条件的周期数。

#### EXIST - 存在
```
EXIST(COND, N)
```
判断N周期内是否存在满足COND条件的周期。

#### EVERY - 一直存在
```
EVERY(COND, N)
```
判断N周期内是否一直满足COND条件。

#### BARSLAST - 上次条件成立位置
```
BARSLAST(COND)
```
返回上次COND条件成立到当前的周期数。

## 运算符

### 算术运算符

| 运算符 | 说明 | 示例 |
|-------|------|------|
| `+` | 加法 | `CLOSE + OPEN` |
| `-` | 减法 | `CLOSE - OPEN` |
| `*` | 乘法 | `CLOSE * 2` |
| `/` | 除法 | `CLOSE / OPEN` |
| `%` | 取模 | `CLOSE % 10` |

### 比较运算符

| 运算符 | 说明 | 示例 |
|-------|------|------|
| `>` | 大于 | `CLOSE > OPEN` |
| `<` | 小于 | `CLOSE < OPEN` |
| `>=` | 大于等于 | `CLOSE >= MA(CLOSE, 20)` |
| `<=` | 小于等于 | `CLOSE <= MA(CLOSE, 20)` |
| `=` 或 `==` | 等于 | `CLOSE = OPEN` |
| `<>` 或 `!=` | 不等于 | `CLOSE <> OPEN` |

### 逻辑运算符

| 运算符 | 说明 | 优先级 |
|-------|------|-------|
| `AND` 或 `&&` | 逻辑与 | 低 |
| `OR` 或 `||` | 逻辑或 | 最低 |
| `NOT` | 逻辑非 | 高 |

### 运算符优先级（从高到低）

1. `NOT` 逻辑非
2. `*`, `/`, `%` 乘除模
3. `+`, `-` 加减
4. `>`, `<`, `>=`, `<=` 比较
5. `=`, `<>`, `==`, `!=` 等于不等于
6. `AND`, `&&` 逻辑与
7. `OR`, `||` 逻辑或

## 常见公式示例

### 布林带 (Bollinger Bands)

```json
{
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

### MACD

```json
{
  "id": 1,
  "name": "MACD_DIF",
  "formulaType": "EXPRESSION",
  "expression": "EMA(CLOSE, 12) - EMA(CLOSE, 26)"
}
```

### 金叉/死叉信号

```json
{
  "functions": [
    {
      "id": 1,
      "name": "GoldenCross",
      "formulaType": "EXPRESSION",
      "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10))"
    },
    {
      "id": 2,
      "name": "DeathCross",
      "formulaType": "EXPRESSION",
      "expression": "CROSS(MA(CLOSE, 10), MA(CLOSE, 5))"
    }
  ]
}
```

### 威廉指标 (Williams %R)

```json
{
  "id": 1,
  "name": "WilliamsR",
  "formulaType": "EXPRESSION",
  "expression": "(HHV(HIGH, 14) - CLOSE) / (HHV(HIGH, 14) - LLV(LOW, 14)) * 100"
}
```

### 价格偏离度

```json
{
  "id": 1,
  "name": "PriceDeviation",
  "formulaType": "EXPRESSION",
  "expression": "(CLOSE - MA(CLOSE, 20)) / MA(CLOSE, 20) * 100"
}
```

### 成交量异动

```json
{
  "id": 1,
  "name": "VolAbnormal",
  "formulaType": "EXPRESSION",
  "expression": "IF(VOL > MA(VOL, 20) * 2, 1, 0)"
}
```

### 多重条件筛选

```json
{
  "id": 1,
  "name": "StrongBuy",
  "formulaType": "EXPRESSION",
  "expression": "CROSS(MA(CLOSE, 5), MA(CLOSE, 10)) AND CLOSE > MA(CLOSE, 20) AND VOL > MA(VOL, 20)"
}
```

## 注意事项

### 1. 临时变量限制

生成的C++代码使用固定大小的临时变量数组（默认256个元素）。如果需要处理更长的数据序列，请修改生成的代码或联系开发者。

### 2. 单输出限制

通达信DLL接口只支持单输出。如果需要多输出（如布林带的上下轨和中轨），需要定义多个函数。

### 3. 输入数据限制

- `CLOSE` 映射到 `pfIn1`
- `OPEN`, `VOL` 映射到 `pfIn2` 
- `HIGH`, `LOW` 映射到 `pfIn3`

**注意**：`LOW` 和 `HIGH` 都映射到 `pfIn3`，这可能会导致冲突。建议在公式中优先使用 `CLOSE`。

### 4. 性能考虑

复杂表达式（如多层嵌套函数）会生成较多的临时变量和循环。对于性能关键的场景，建议使用 CUSTOM 类型编写优化的 C++ 代码。

### 5. 验证表达式

使用 validate 命令验证表达式语法：

```bash
dotnet run -- validate -c myconfig.json
```

### 6. 调试技巧

如果表达式结果不符合预期：

1. 简化表达式，逐步测试
2. 使用简单表达式如 `CLOSE` 或 `MA(CLOSE, 5)` 确认数据输入正确
3. 检查函数参数顺序（如 `REF(X, N)` 不是 `REF(N, X)`）

### 7. 常见错误

```
错误: 未知的函数: XYZ          // 函数名拼写错误或不存在
错误: 函数 MA 至少需要 2 个参数 // 参数数量不足
错误: 缺少右括号 ')'            // 括号不匹配
错误: 意外的字符: '@'           // 使用了不支持的字符
```

## 与通达信公式的差异

虽然语法尽量兼容通达信公式，但存在以下差异：

1. **不支持**通达信的绘图函数（如 `DRAWTEXT`, `STICKLINE` 等）
2. **不支持**动态计算周期（如 `MA(CLOSE, N)` 中的 N 必须是常量）
3. **不支持**序列作为参数（如 `REF(CLOSE, MA(N, 5))` 这种嵌套）
4. **只支持**单输出，通达信支持多输出绘图

## 更多信息

- 查看 `examples/expression-config.json` 获取完整示例
- 查看主 README.md 了解完整功能
- 使用 `dotnet run -- validate -c config.json` 验证配置
