using Xunit;
using TdxDllGenerator;

namespace TdxDllGenerator.Tests;

/// <summary>
/// 配置加载器单元测试
/// </summary>
public class ConfigLoaderTests
{
    /// <summary>
    /// 测试：从JSON文件加载配置
    /// </summary>
    [Fact]
    public void LoadFromJson_ValidJson_ReturnsConfig()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        var json = @"{
            ""pluginName"": ""TestPlugin"",
            ""pluginDescription"": ""测试插件"",
            ""functions"": [
                {
                    ""id"": 1,
                    ""name"": ""TestMA"",
                    ""description"": ""测试均线"",
                    ""formulaType"": ""MA"",
                    ""parameters"": [{""name"": ""period"", ""defaultValue"": 10, ""description"": ""周期""}]
                }
            ],
            ""security"": {
                ""enableTimeLimit"": false,
                ""expireYear"": 2026,
                ""enableDeviceBinding"": false,
                ""enableOnlineVerify"": false
            }
        }"";
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            var config = ConfigLoader.LoadFromJson(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("TestPlugin", config.PluginName);
            Assert.Equal("测试插件", config.PluginDescription);
            Assert.Single(config.Functions);
            Assert.Equal("TestMA", config.Functions[0].Name);
            Assert.Equal("MA", config.Functions[0].FormulaType);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// 测试：加载不存在的文件应抛出异常
    /// </summary>
    [Fact]
    public void LoadFromJson_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentFile = "/path/that/does/not/exist.json";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.LoadFromJson(nonExistentFile));
    }

    /// <summary>
    /// 测试：创建默认配置文件
    /// </summary>
    [Fact]
    public void CreateDefaultConfig_CreatesValidFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"default_config_{Guid.NewGuid()}.json");

        try
        {
            // Act
            ConfigLoader.CreateDefaultConfig(tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            var config = ConfigLoader.LoadFromJson(tempFile);
            Assert.NotNull(config);
            Assert.Equal("MyTdxPlugin", config.PluginName);
            Assert.Equal(3, config.Functions.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
