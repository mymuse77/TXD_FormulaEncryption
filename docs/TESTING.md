# 测试流程文档

本文档描述 TdxDllGenerator 项目的测试流程和方法。

## 测试框架

本项目使用 **xUnit** 作为测试框架。

## 运行测试

### 运行所有测试

```bash
cd tests/TdxDllGenerator.Tests
dotnet test
```

### 运行特定测试

```bash
dotnet test --filter "ConfigLoaderTests"
dotnet test --filter "ModelsTests"
dotnet test --filter "CppCodeGeneratorTests"
dotnet test --filter "ProgramTests"
dotnet test --filter "IntegrationTests"
```

### 带覆盖率运行

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 测试结构

```
tests/
└── TdxDllGenerator.Tests/
    ├── ConfigLoaderTests.cs    # 配置加载器测试
    ├── ModelsTests.cs          # 数据模型测试
    ├── CppCodeGeneratorTests.cs # C++代码生成器测试
    ├── ProgramTests.cs         # 命令行参数测试
    └── IntegrationTests.cs     # 集成测试
```

## 编写新测试

1. 在 `tests/TdxDllGenerator.Tests/` 目录下创建新的测试类
2. 命名规范：`{ClassName}Tests.cs`
3. 继承测试类并添加 `[Fact]` 或 `[Theory]` 特性

### 示例

```csharp
using Xunit;
using TdxDllGenerator;

namespace TdxDllGenerator.Tests;

public class MyClassTests
{
    [Fact]
    public void MyMethod_WhenCalled_ReturnsExpected()
    {
        // Arrange
        var obj = new MyClass();

        // Act
        var result = obj.MyMethod();

        // Assert
        Assert.Equal(expected, result);
    }
}
```

## 测试覆盖率目标

- 核心类：> 80%
- 工具方法：> 60%
- UI/控制台输出：可选测试

## 持续集成

建议在 CI/CD 流程中添加：

```yaml
- name: 运行测试
  run: dotnet test tests/TdxDllGenerator.Tests

- name: 生成覆盖率报告
  run: dotnet test --collect:"XPlat Code Coverage"
```

## 当前测试状态

| 类 | 测试文件 | 状态 |
|---|---------|------|
| ConfigLoader | ConfigLoaderTests.cs | ✓ |
| Models | ModelsTests.cs | ✓ |
| CppCodeGenerator | CppCodeGeneratorTests.cs | ✓ |
| Program | ProgramTests.cs | ✓ |
| 集成测试 | IntegrationTests.cs | ✓ |

## 待办事项

- [x] 添加 CppCodeGenerator 测试
- [x] 添加 Program 命令行参数测试
- [x] 添加集成测试
- [x] 配置 CI/CD 自动化测试 (详见 CICD.md)
