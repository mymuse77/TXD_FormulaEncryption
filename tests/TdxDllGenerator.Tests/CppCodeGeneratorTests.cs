using Xunit;
using TdxDllGenerator;

namespace TdxDllGenerator.Tests;

/// <summary>
/// C++代码生成器单元测试
/// </summary>
public class CppCodeGeneratorTests
{
    private readonly string _tempDir;

    public CppCodeGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tdx_test_{Guid.NewGuid()}");
    }

    /// <summary>
    /// 测试：生成项目创建目录结构
    /// </summary>
    [Fact]
    public void GenerateProject_CreatesDirectory()
    {
        // Arrange
        var config = CreateTestConfig();
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();

            // Assert
            Assert.True(Directory.Exists(_tempDir));
            Assert.True(File.Exists(Path.Combine(_tempDir, "PluginTCalcFunc.h")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "PluginTCalcFunc.cpp")));
        }
        finally
        {
            CleanupTempDir();
        }
    }

    /// <summary>
    /// 测试：生成头文件包含必要内容
    /// </summary>
    [Fact]
    public void GenerateProject_HeaderFile_ContainsRequiredContent()
    {
        // Arrange
        var config = CreateTestConfig();
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();
            var headerContent = File.ReadAllText(Path.Combine(_tempDir, "PluginTCalcFunc.h"));

            // Assert
            Assert.Contains("#pragma once", headerContent);
            Assert.Contains("#ifdef __cplusplus", headerContent);
            Assert.Contains("extern \"C\"", headerContent);
        }
        finally
        {
            CleanupTempDir();
        }
    }

    /// <summary>
    /// 测试：生成源文件包含函数导出
    /// </summary>
    [Fact]
    public void GenerateProject_SourceFile_ContainsFunctionExports()
    {
        // Arrange
        var config = CreateTestConfig();
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();
            var sourceContent = File.ReadAllText(Path.Combine(_tempDir, "PluginTCalcFunc.cpp"));

            // Assert
            Assert.Contains("RegisterTdxFunc", sourceContent);
            Assert.Contains(config.PluginName, sourceContent);
        }
        finally
        {
            CleanupTempDir();
        }
    }

    /// <summary>
    /// 测试：生成项目文件
    /// </summary>
    [Fact]
    public void GenerateProject_CreatesProjectFiles()
    {
        // Arrange
        var config = CreateTestConfig();
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();

            // Assert
            Assert.True(File.Exists(Path.Combine(_tempDir, $"{config.PluginName}.vcxproj")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "CMakeLists.txt")));
            Assert.True(File.Exists(Path.Combine(_tempDir, $"{config.PluginName}.def")));
        }
        finally
        {
            CleanupTempDir();
        }
    }

    /// <summary>
    /// 测试：生成构建脚本
    /// </summary>
    [Fact]
    public void GenerateProject_CreatesBuildScripts()
    {
        // Arrange
        var config = CreateTestConfig();
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();

            // Assert
            Assert.True(File.Exists(Path.Combine(_tempDir, "build.bat")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "README.md")));
        }
        finally
        {
            CleanupTempDir();
        }
    }

    /// <summary>
    /// 测试：MA函数生成
    /// </summary>
    [Fact]
    public void GenerateProject_WithMAFunction_GeneratesMACode()
    {
        // Arrange
        var config = new TdxFormulaConfig
        {
            PluginName = "TestPlugin",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "TestMA",
                    FormulaType = "MA",
                    Parameters = new List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 10 }
                    }
                }
            }
        };
        Directory.CreateDirectory(_tempDir);

        try
        {
            var generator = new CppCodeGenerator(config, _tempDir);

            // Act
            generator.GenerateProject();
            var sourceContent = File.ReadAllText(Path.Combine(_tempDir, "PluginTCalcFunc.cpp"));

            // Assert
            Assert.Contains("TestMA", sourceContent);
        }
        finally
        {
            CleanupTempDir();
        }
    }

    #region Helper Methods

    private TdxFormulaConfig CreateTestConfig()
    {
        return new TdxFormulaConfig
        {
            PluginName = "TestPlugin",
            PluginDescription = "测试插件",
            Functions = new List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "TestFunc1",
                    Description = "测试函数1",
                    FormulaType = "MA",
                    Parameters = new List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 10, Description = "周期" }
                    }
                }
            }
        };
    }

    private void CleanupTempDir()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    #endregion
}
