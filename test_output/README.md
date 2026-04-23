# 编译说明

## 项目: MyTest

### 方法1: 双击编译脚本 (最简单)

直接双击 `build.bat` 文件，脚本会自动:
1. 查找已安装的Visual Studio
2. 编译 Release Win32 版本
3. 输出 DLL 文件到 Release 目录

### 方法2: 使用Visual Studio IDE

1. 打开Visual Studio 2019/2022
2. 打开 `MyTest.vcxproj`
3. 选择 **Release** 配置和 **Win32** 平台
4. 生成 → 生成解决方案

### 方法3: 使用Visual Studio命令行

打开 **Developer Command Prompt for VS 2022**，然后执行:

```cmd
msbuild MyTest.vcxproj /p:Configuration=Release /p:Platform=Win32
```

### 方法4: 使用CMake

```bash
mkdir build
cd build
cmake .. -A Win32
cmake --build . --config Release
```

### 方法5: 使用MinGW (无需VS)

```bash
g++ -shared -o MyTest.dll MyTest.cpp MyTest.def -m32 -O2 -static-libgcc -static-libstdc++
```

## 安装使用

1. 将生成的 `.dll` 文件复制到通达信安装目录:
   ```
   通达信安装目录\T0002\dlls\
   ```

2. 打开通达信，按 `Ctrl+F` 进入公式管理器
3. 点击【DLL函数】按钮
4. 选择DLL编号（1-10）
5. 点击【打开绑定】，选择 `MyTest.dll`
6. 点击【确定】完成绑定

## 公式调用示例

### GoldenCross (金叉信号 - 5日均线上穿10日均线)

```
{调用GoldenCross，函数ID=1}
RESULT: TDXDLL1(1, CLOSE, 10, 0);
```

### BollingerUpper (布林带上轨)

```
{调用BollingerUpper，函数ID=2}
RESULT: TDXDLL1(2, CLOSE, 10, 0);
```

### BollingerLower (布林带下轨)

```
{调用BollingerLower，函数ID=3}
RESULT: TDXDLL1(3, CLOSE, 10, 0);
```

### PriceMomentum (价格动量 - N日涨幅百分比)

```
{调用PriceMomentum，函数ID=4}
RESULT: TDXDLL1(4, CLOSE, 10, 0);
```

### RSI14 (RSI相对强弱指标(14日) - SMA算法)

```
{调用RSI14，函数ID=5}
RESULT: TDXDLL1(5, CLOSE, 10, 0);
```

### MACD_DIF (MACD的DIF线 - EMA12-EMA26)

```
{调用MACD_DIF，函数ID=6}
RESULT: TDXDLL1(6, CLOSE, 10, 0);
```

### MACD_DEA (MACD的DEA线 - DIF的9日EMA)

```
{调用MACD_DEA，函数ID=7}
RESULT: TDXDLL1(7, CLOSE, 10, 0);
```

### MACD_Hist (MACD柱状线 - (DIF-DEA)*2)

```
{调用MACD_Hist，函数ID=8}
RESULT: TDXDLL1(8, CLOSE, 10, 0);
```

### KDJ_K (KDJ指标K值 - RSV的3日SMA)

```
{调用KDJ_K，函数ID=9}
RESULT: TDXDLL1(9, CLOSE, 10, 0);
```

### KDJ_D (KDJ指标D值 - K值的3日SMA)

```
{调用KDJ_D，函数ID=10}
RESULT: TDXDLL1(10, CLOSE, 10, 0);
```

### KDJ_J (KDJ指标J值 - 3*K-2*D)

```
{调用KDJ_J，函数ID=11}
RESULT: TDXDLL1(11, CLOSE, 10, 0);
```

### ATR14 (真实波幅ATR(14日) - TR的14日MA)

```
{调用ATR14，函数ID=12}
RESULT: TDXDLL1(12, CLOSE, 10, 0);
```

### WilliamR (威廉指标WR(14日))

```
{调用WilliamR，函数ID=13}
RESULT: TDXDLL1(13, CLOSE, 10, 0);
```

### CCI14 (CCI商品路径指标(14日))

```
{调用CCI14，函数ID=14}
RESULT: TDXDLL1(14, CLOSE, 10, 0);
```

### DMI_PDI (DMI指标+DI(14日) - 上升动向指标)

```
{调用DMI_PDI，函数ID=15}
RESULT: TDXDLL1(15, CLOSE, 10, 0);
```

### DMI_MDI (DMI指标-DI(14日) - 下降动向指标)

```
{调用DMI_MDI，函数ID=16}
RESULT: TDXDLL1(16, CLOSE, 10, 0);
```

### ROC (变动速率ROC(12日))

```
{调用ROC，函数ID=17}
RESULT: TDXDLL1(17, CLOSE, 10, 0);
```

### OBV (能量潮OBV - 基于成交量累计)

```
{调用OBV，函数ID=18}
RESULT: TDXDLL1(18, CLOSE, 10, 0);
```

### VOL_Ratio (量比 - 当日成交量/5日均量)

```
{调用VOL_Ratio，函数ID=19}
RESULT: TDXDLL1(19, CLOSE, 10, 0);
```

### MA_Cross_Count (近20日金叉次数统计)

```
{调用MA_Cross_Count，函数ID=20}
RESULT: TDXDLL1(20, CLOSE, 10, 0);
```

### Price_Breakout (突破20日新高信号)

```
{调用Price_Breakout，函数ID=21}
RESULT: TDXDLL1(21, CLOSE, 10, 0);
```

### Trend_Strength (趋势强度 - 价格偏离20日均线百分比)

```
{调用Trend_Strength，函数ID=22}
RESULT: TDXDLL1(22, CLOSE, 10, 0);
```

### Bollinger_Width (布林带宽度 - 上下轨距离/中轨)

```
{调用Bollinger_Width，函数ID=23}
RESULT: TDXDLL1(23, CLOSE, 10, 0);
```

### Volatility (历史波动率 - 20日收益率标准差)

```
{调用Volatility，函数ID=24}
RESULT: TDXDLL1(24, CLOSE, 10, 0);
```

### MA_Cross_Signal (均线交叉综合信号(5/10/20/60四线))

```
{调用MA_Cross_Signal，函数ID=25}
RESULT: TDXDLL1(25, CLOSE, 10, 0);
```

### Bottom_Divergence (底部信号 - 价格创新低但RSI未创新低)

```
{调用Bottom_Divergence，函数ID=26}
RESULT: TDXDLL1(26, CLOSE, 10, 0);
```

### Consecutive_Rise (连续上涨天数)

```
{调用Consecutive_Rise，函数ID=27}
RESULT: TDXDLL1(27, CLOSE, 10, 0);
```

### Gap_Up (向上跳空缺口信号)

```
{调用Gap_Up，函数ID=28}
RESULT: TDXDLL1(28, CLOSE, 10, 0);
```

### MFI14 (Money Flow Index 14)

```
{调用MFI14，函数ID=29}
RESULT: TDXDLL1(29, CLOSE, 10, 0);
```

### Bollinger_Squeeze (布林带收窄信号 - 带宽低于近20日最低值)

```
{调用Bollinger_Squeeze，函数ID=30}
RESULT: TDXDLL1(30, CLOSE, 10, 0);
```

### CustomMA (带参数的自定义均线(参数传入周期))

```
{调用CustomMA，函数ID=31}
RESULT: TDXDLL1(31, CLOSE, 10, 0);
```

### CustomEMA (带参数的自定义EMA(参数传入周期))

```
{调用CustomEMA，函数ID=32}
RESULT: TDXDLL1(32, CLOSE, 10, 0);
```

### CustomRSI (带参数的自定义RSI(参数传入周期))

```
{调用CustomRSI，函数ID=33}
RESULT: TDXDLL1(33, CLOSE, 10, 0);
```

### MACD_Std (标准MACD指标(DIF线))

```
{调用MACD_Std，函数ID=34}
RESULT: TDXDLL1(34, CLOSE, 10, 0);
```

### VolumeWeightedPrice (成交量加权价格 - 自定义C++算法)

```
{调用VolumeWeightedPrice，函数ID=35}
RESULT: TDXDLL1(35, CLOSE, 10, 0);
```

### 低吸连板前 (低吸连板前主图信号 - 完整通达信公式)

```
{调用低吸连板前，函数ID=36}
RESULT: TDXDLL1(36, CLOSE, 10, 0);
```

