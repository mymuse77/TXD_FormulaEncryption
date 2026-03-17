using Xunit;
using System.Diagnostics;

namespace TdxDllGenerator.Tests;

/// <summary>
/// 集成测试 - 端到端场景
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public IntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tdx_int_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "test_config.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    /// <summary>
    /// 集成测试：完整流程 - 创建配置、验证、生成
    /// </summary>
    [Fact]
    public void FullWorkflow_CreateValidateGenerate()
    {
        // Arrange - 创建有效配置
        var config = new TdxFormulaConfig
        {
            PluginName = "IntegrationTestPlugin",
            PluginDescription = "集成测试插件",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "IntTestMA",
                    Description = "集成测试MA",
                    FormulaType = "MA",
                    Parameters = new List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 5, Description = "周期" }
                    }
                }
            },
            Security = new SecurityConfig { EnableTimeLimit = false }
        };

        // Act - 保存配置
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_configPath, json);

        // Assert - 文件存在
        Assert.True(File.Exists(_configPath));

        // Act - 加载配置
        var loadedConfig = ConfigLoader.LoadFromJson(_configPath);

        // Assert - 配置正确
        Assert.Equal("IntegrationTestPlugin", loadedConfig.PluginName);
        Assert.Single(loadedConfig.Functions);

        // Act - 生成项目
        var outputDir = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(outputDir);
        var generator = new CppCodeGenerator(loadedConfig, outputDir);
        generator.GenerateProject();

        // Assert - 输出文件存在
        Assert.True(Directory.Exists(outputDir));
        Assert.True(File.Exists(Path.Combine(outputDir, "PluginTCalcFunc.h")));
        Assert.True(File.Exists(Path.Combine(outputDir, "PluginTCalcFunc.cpp")));
        Assert.True(File.Exists(Path.Combine(outputDir, "CMakeLists.txt")));
    }

    /// <summary>
    /// 集成测试：多函数配置生成
    /// </summary>
    [Fact]
    public void MultiFunctionConfig_GeneratesAllFunctions()
    {
        // Arrange
        var config = new TdxFormulaConfig
        {
            PluginName = "MultiFuncPlugin",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "MA5",
                    FormulaType = "MA",
                    Parameters = new List<ParameterConfig> { new() { Name = "n", DefaultValue = 5 } }
                },
                new()
                {
                    Id = 2,
                    Name = "MA10",
                    FormulaType = "MA",
                    Parameters = new List<ParameterConfig> { new() { Name = "n", DefaultValue = 10 } }
                },
                new()
                {
                    Id = 3,
                    Name = "RSI14",
                    FormulaType = "RSI",
                    Parameters = new List<ParameterConfig> { new() { Name = "period", DefaultValue = 14 } }
                }
            }
        };

        var outputDir = Path.Combine(_tempDir, "multi");
        Directory.CreateDirectory(outputDir);

        // Act
        var generator = new CppCodeGenerator(config, outputDir);
        generator.GenerateProject();

        var sourceContent = File.ReadAllText(Path.Combine(outputDir, "PluginTCalcFunc.cpp"));

        // Assert - 所有函数都生成
        Assert.Contains("MA5", sourceContent);
        Assert.Contains("MA10", sourceContent);
        Assert.Contains("RSI14", sourceContent);
    }

    /// <summary>
    /// 集成测试：带安全设置的配置生成
    /// </summary>
    [Fact]
    public void SecurityEnabledConfig_GeneratesSecurityCode()
    {
        // Arrange
        var config = new TdxFormulaConfig
        {
            PluginName = "SecurePlugin",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "SecureMA",
                    FormulaType = "MA"
                }
            },
            Security = new SecurityConfig
            {
                EnableTimeLimit = true,
                ExpireYear = 2025
            }
        };

        var outputDir = Path.Combine(_tempDir, "secure");
        Directory.CreateDirectory(outputDir);

        // Act
        var generator = new CppCodeGenerator(config, outputDir);
        generator.GenerateProject();

        var sourceContent = File.ReadAllText(Path.Combine(outputDir, "PluginTCalcFunc.cpp"));

        // Assert - 包含安全检查代码
        Assert.Contains("2025", sourceContent);
    }
}
