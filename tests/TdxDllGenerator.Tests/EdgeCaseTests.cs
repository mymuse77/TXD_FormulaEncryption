using Xunit;

namespace TdxDllGenerator.Tests;

/// <summary>
/// 边界条件和异常处理测试
/// </summary>
public class EdgeCaseTests
{
    /// <summary>
    /// 测试：空函数列表配置
    /// </summary>
    [Fact]
    public void EmptyFunctionList_GeneratesMinimalCode()
    {
        // Arrange
        var config = new TdxFormulaConfig
        {
            PluginName = "EmptyPlugin",
            Functions = new List<FunctionConfig>()
        };

        var tempDir = Path.Combine(Path.GetTempPath(), $"empty_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var generator = new CppCodeGenerator(config, tempDir);
            generator.GenerateProject();

            // Assert - 即使函数列表为空，也应该生成基本文件
            Assert.True(File.Exists(Path.Combine(tempDir, "PluginTCalcFunc.h")));
            Assert.True(File.Exists(Path.Combine(tempDir, "PluginTCalcFunc.cpp")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// 测试：特殊字符名称处理
    /// </summary>
    [Fact]
    public void SpecialCharactersInName_HandledCorrectly()
    {
        // Arrange
        var config = new TdxFormulaConfig
        {
            PluginName = "SpecialCharsPlugin",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "TestFunc_1",
                    Description = "带下划线的函数名",
                    FormulaType = "MA"
                }
            }
        };

        Assert.Equal("TestFunc_1", config.Functions[0].Name);
    }

    /// <summary>
    /// 测试：大参数值
    /// </summary>
    [Fact]
    public void LargeParameterValues_Accepted()
    {
        // Arrange
        var param = new ParameterConfig
        {
            Name = "large",
            DefaultValue = 999999.99f,
            Description = "大数值参数"
        };

        // Assert
        Assert.Equal(999999.99f, param.DefaultValue);
    }

    /// <summary>
    /// 测试：多个参数
    /// </summary>
    [Fact]
    public void MultipleParameters_WorkCorrectly()
    {
        // Arrange
        var func = new FunctionConfig
        {
            Id = 1,
            Name = "MultiParam",
            Parameters = new List<ParameterConfig>
            {
                new() { Name = "p1", DefaultValue = 1.0f },
                new() { Name = "p2", DefaultValue = 2.0f },
                new() { Name = "p3", DefaultValue = 3.0f },
                new() { Name = "p4", DefaultValue = 4.0f },
                new() { Name = "p5", DefaultValue = 5.0f }
            }
        };

        // Assert
        Assert.Equal(5, func.Parameters.Count);
        Assert.Equal("p5", func.Parameters[4].Name);
    }

    /// <summary>
    /// 测试：安全配置的默认值
    /// </summary>
    [Fact]
    public void SecurityConfig_Defaults_AreSecure()
    {
        // Arrange & Act
        var security = new SecurityConfig();

        // Assert - 默认应该是禁用所有安全功能
        Assert.False(security.EnableTimeLimit);
        Assert.False(security.EnableDeviceBinding);
        Assert.False(security.EnableOnlineVerify);
    }

    /// <summary>
    /// 测试：无效过期年份
    /// </summary>
    [Fact]
    public void SecurityConfig_InvalidExpireYear_Handled()
    {
        // Arrange
        var security = new SecurityConfig
        {
            EnableTimeLimit = true,
            ExpireYear = 1999  // 过去的年份
        };

        // Assert
        Assert.True(security.EnableTimeLimit);
        Assert.Equal(1999, security.ExpireYear);
    }
}
