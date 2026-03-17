# DLL部署与导入完整指南

本文档详细描述从生成DLL到在通达信中成功导入使用的完整流程。

## 目录

1. [生成DLL插件](#一生成dll插件)
2. [编译DLL](#二编译dll)
3. [部署到通达信](#三部署到通达信)
4. [导入DLL](#四导入dll)
5. [验证使用](#五验证使用)
6. [故障排除](#六故障排除)

---

## 一、生成DLL插件

### 1.1 准备配置文件

创建 `config.json`：

```json
{
  "pluginName": "MyIndicators",
  "pluginDescription": "我的自定义指标",
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
          "description": "均线周期"
        }
      ]
    },
    {
      "id": 2,
      "name": "MyRSI",
      "description": "自定义RSI",
      "formulaType": "RSI",
      "parameters": [
        {
          "name": "n",
          "defaultValue": 14,
          "description": "RSI周期"
        }
      ]
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

### 1.2 运行生成器

```bash
TdxDllGenerator generate -c config.json -o ./MyIndicators
```

生成文件结构：
```
MyIndicators/
├── PluginTCalcFunc.h       # 头文件
├── PluginTCalcFunc.cpp     # 源文件
├── MyIndicators.vcxproj    # VS项目
├── MyIndicators.def        # 模块定义
├── CMakeLists.txt          # CMake配置
├── build.bat               # 编译脚本
├── README.md               # 说明文档
└── 编译指南.md              # 中文编译指南
```

---

## 二、编译DLL

### 2.1 使用Visual Studio编译

**方法一：命令行编译**

```bash
cd MyIndicators

# 设置VS环境变量（根据你的VS版本调整）
"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars32.bat"

# 编译
msbuild MyIndicators.vcxproj /p:Configuration=Release /p:Platform=Win32
```

**方法二：GUI编译**

1. 双击 `MyIndicators.vcxproj` 打开Visual Studio
2. 选择 **Release** 配置
3. 选择 **Win32** 平台（通达信是32位程序）
4. 菜单 → 生成 → 生成解决方案
5. 在 `Release/` 目录找到 `MyIndicators.dll`

### 2.2 使用CMake编译

```bash
cd MyIndicators
mkdir build && cd build

# 生成32位项目
cmake .. -A Win32

# 编译
cmake --build . --config Release

# 输出文件在 Release/MyIndicators.dll
```

### 2.3 编译输出

编译成功后得到：
```
MyIndicators/
├── Release/
│   ├── MyIndicators.dll      # 主文件
│   ├── MyIndicators.lib      # 导入库（可选）
│   └── MyIndicators.exp      # 导出文件（可选）
```

---

## 三、部署到通达信

### 3.1 确定通达信目录

常见通达信安装路径：

| 版本 | 默认路径 |
|------|----------|
| 通达信金融终端 | `C:\new_tdx\` |
| 通达信64位 | `C:\new_tdx64\` |
| 券商定制版 | `C:\[券商名称]\` |

### 3.2 复制DLL文件

**目标目录**：通达信安装目录下的 `T0002\dlls\`

```bash
# 示例：复制到通达信目录
copy MyIndicators.dll C:\new_tdx64\T0002\dlls\
```

**完整部署结构**：
```
C:\new_tdx64\T0002\dlls\
├── MyIndicators.dll          # 你的插件
├── MyIndicators\             # 插件数据目录（可选）
│   └── config.dat
└── (其他DLL文件)
```

### 3.3 设备绑定部署（如启用）

如果启用了设备绑定，需要同时部署：

1. **指纹生成工具**：`MachineIDGenerator.exe`
2. **授权文件**：`license.dat`

部署结构：
```
C:\new_tdx64\T0002\dlls\
├── MyIndicators.dll
├── license.dat               # 授权文件（与DLL同目录）
└── MachineIDGenerator.exe    # 指纹工具（可选）
```

---

## 四、导入DLL

### 4.1 打开公式管理器

1. 启动通达信软件
2. 菜单 → **功能** → **公式系统** → **公式管理器**
3. 或按快捷键 **Ctrl+F**

### 4.2 绑定DLL

1. 在公式管理器左侧选择 **DLL函数**
2. 找到你的插件名称（如 `MyIndicators`）
3. 点击 **绑定**

### 4.3 验证绑定

绑定成功后，在公式编辑器中可以使用：

```
{ 使用DLL函数 }
MA10: MyMA(CLOSE, 10);
RSI14: MyRSI(CLOSE, 14);
```

---

## 五、验证使用

### 5.1 创建测试公式

1. 公式管理器 → **技术指标** → **新建**
2. 输入公式代码：

```
{ MyTest指标 }
{ 测试自定义DLL }

MA5: MyMA(CLOSE, 5);
MA10: MyMA(CLOSE, 10);
MA20: MyMA(CLOSE, 20);

RSI_VALUE: MyRSI(CLOSE, 14);
```

3. 点击 **测试公式**
4. 显示 **测试通过** 表示DLL正常工作

### 5.2 应用到图表

1. 保存公式
2. 在K线图界面输入公式名称
3. 查看指标是否正确显示

---

## 六、故障排除

### 6.1 常见问题

#### Q1: 绑定DLL时提示"找不到DLL"

**原因**：
- DLL未放入正确目录
- DLL文件名与配置不匹配

**解决**：
```
1. 确认DLL在 T0002\dlls\ 目录
2. 确认文件名与pluginName一致
3. 重启通达信后再试
```

#### Q2: 公式测试时显示"DLL调用失败"

**原因**：
- DLL编译架构错误（必须是32位）
- 缺少依赖库
- 导出函数名不正确

**解决**：
```
1. 使用Dependency Walker检查导出函数
   - 应该导出: RegisterTdxFunc

2. 确认编译为Win32/x86平台
   - 不是x64!

3. 检查是否静态链接运行时库
   - 项目属性 → C/C++ → 代码生成 → 运行库 → 多线程(/MT)
```

#### Q3: 设备绑定失败，提示"未授权"

**原因**：
- 缺少license.dat文件
- 机器指纹不匹配
- 授权已过期

**解决**：
```
1. 确认license.dat与DLL同目录
2. 重新运行MachineIDGenerator获取指纹
3. 联系开发者重新生成授权
4. 检查系统时间是否正确
```

#### Q4: 通达信崩溃或异常退出

**原因**：
- DLL中存在内存错误
- 数组越界
- 除零错误

**解决**：
```
1. 检查代码中的数组边界
2. 添加除零保护
3. 使用调试版DLL定位问题
4. 查看通达信日志（如有）
```

### 6.2 调试方法

#### 查看导出函数

使用Dependency Walker或类似工具：
```
dependencywalker.exe MyIndicators.dll

# 应看到导出函数:
# RegisterTdxFunc
```

#### 检查DLL架构

```bash
# 使用dumpbin
dumpbin /headers MyIndicators.dll

# 查找 "machine" 行
# 应为 14C (x86) 而不是 8664 (x64)
```

#### 日志调试

在DLL代码中添加日志：
```cpp
#include <stdio.h>

void LogMessage(const char* msg) {
    FILE* fp = fopen("C:\\tdx_dll_debug.log", "a");
    if (fp) {
        fprintf(fp, "%s\n", msg);
        fclose(fp);
    }
}
```

### 6.3 性能优化

#### 减少计算量

```cpp
// 优化前：每次都重新计算
float CalculateMA(int n, float* data, int dataSize) {
    float sum = 0;
    for (int i = 0; i < n; i++) {
        sum += data[i];
    }
    return sum / n;
}

// 优化后：使用增量计算
float CalculateMAOptimized(int n, float* data, int dataSize, float prevMA) {
    // 增量更新均线
    return prevMA + (data[0] - data[n]) / n;
}
```

#### 避免重复分配内存

```cpp
// 使用静态缓冲区
static float buffer[MAX_SIZE];

// 而不是动态分配
float* buffer = (float*)malloc(size);  // 避免
```

---

## 附录

### 附录A：目录结构速查

**开发目录**：
```
Project/
├── config.json           # 配置文件
├── TdxDllGenerator.exe   # 生成器
└── output/               # 输出目录
    └── MyIndicators/
        ├── PluginTCalcFunc.cpp
        ├── MyIndicators.vcxproj
        └── ...
```

**部署目录**：
```
C:\new_tdx64\T0002\dlls\
├── MyIndicators.dll
├── MyIndicators\        # 数据目录（可选）
└── license.dat          # 授权文件（如启用）
```

**用户文档目录**：
```
C:\Users\[用户名]\Documents\通达信\
└── (用户数据)
```

### 附录B：快捷命令

```bash
# 生成项目
TdxDllGenerator generate -c config.json -o ./output

# 编译（VS命令行）
msbuild MyIndicators.vcxproj /p:Configuration=Release /p:Platform=Win32

# 复制到通达信
copy MyIndicators.dll C:\new_tdx64\T0002\dlls\

# 生成授权（开发者）
TdxDllGenerator license -f [指纹] -u [用户名] -e [邮箱] -d [日期] -o license.dat
```

### 附录C：多版本管理

如果需要同时维护多个版本：

```
dlls/
├── MyIndicators_v1.dll
├── MyIndicators_v2.dll
└── MyIndicators_v3.dll
```

在通达信中分别绑定不同版本。

---

**版本**: v1.0
**更新日期**: 2026-03-17
