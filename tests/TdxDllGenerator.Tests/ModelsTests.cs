using Xunit;
using TdxDllGenerator;

namespace TdxDllGenerator.Tests;

/// <summary>
/// 数据模型单元测试
/// </summary>
public class ModelsTests
{
    /// <summary>
    /// 测试：TdxFormulaConfig 默认值
    /// </summary>
    [Fact]
    public void TdxFormulaConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new TdxFormulaConfig();

        // Assert
        Assert.Equal("MyPlugin", config.PluginName);
        Assert.Equal("通达信自定义公式插件", config.PluginDescription);
        Assert.NotNull(config.Functions);
        Assert.Empty(config.Functions);
        Assert.NotNull(config.Security);
    }

    /// <summary>
    /// 测试：FunctionConfig 默认值
    /// </summary>
    [Fact]
    public void FunctionConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var func = new FunctionConfig();

        // Assert
        Assert.Equal("", func.Name);
        Assert.Equal("MA", func.FormulaType);
        Assert.NotNull(func.Parameters);
        Assert.Empty(func.Parameters);
    }

    /// <summary>
    /// 测试：ParameterConfig 设置值
    /// </summary>
    [Fact]
    public void ParameterConfig_SetValues_WorkCorrectly()
    {
        // Arrange
        var param = new ParameterConfig
        {
            Name = "period",
            DefaultValue = 14.0f,
            Description = "计算周期"
        };

        // Assert
        Assert.Equal("period", param.Name);
        Assert.Equal(14.0f, param.DefaultValue);
        Assert.Equal("计算周期", param.Description);
    }

    /// <summary>
    /// 测试：SecurityConfig 默认值
    /// </summary>
    [Fact]
    public void SecurityConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var security = new SecurityConfig();

        // Assert
        Assert.False(security.EnableTimeLimit);
        Assert.Equal(2026, security.ExpireYear);
        Assert.False(security.EnableDeviceBinding);
        Assert.False(security.EnableOnlineVerify);
        Assert.Null(security.VerifyUrl);
    }

    /// <summary>
    /// 测试：完整配置构建
    /// </summary>
    [Fact]
    public void TdxFormulaConfig_FullConfiguration_Works()
    {
        // Arrange & Act
        var config = new TdxFormulaConfig
        {
            PluginName = "TestPlugin",
            PluginDescription = "测试描述",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "TestFunc",
                    Description = "测试函数",
                    FormulaType = "RSI",
                    Parameters = new List<ParameterConfig>
                    {
                        new() { Name = "n", DefaultValue = 14, Description = "周期" }
                    }
                }
            },
            Security = new SecurityConfig
            {
                EnableTimeLimit = true,
                ExpireYear = 2025
            }
        };

        // Assert
        Assert.Equal("TestPlugin", config.PluginName);
        Assert.Single(config.Functions);
        Assert.Equal("RSI", config.Functions[0].FormulaType);
        Assert.True(config.Security.EnableTimeLimit);
    }
}
