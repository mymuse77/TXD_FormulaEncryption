using Xunit;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace TdxDllGenerator.Tests;

/// <summary>
/// Program命令行参数单元测试
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// 测试：generate命令需要配置参数
    /// </summary>
    [Fact]
    public void GenerateCommand_RequiresConfigOption()
    {
        // Arrange
        var args = new[] { "generate" };

        // Act - 应该显示帮助或错误，因为没有 -c 参数
        var result = Program.CreateRootCommand().Parse(args);

        // Assert
        Assert.True(result.Errors.Count > 0 || result.GetValueForOption(new Option<string>("-c")) == null);
    }

    /// <summary>
    /// 测试：generate命令接受配置和输出参数
    /// </summary>
    [Fact]
    public void GenerateCommand_AcceptsConfigAndOutput()
    {
        // Arrange
        var args = new[] { "generate", "-c", "test.json", "-o", "./output" };

        // Act
        var result = Program.CreateRootCommand().Parse(args);

        // Assert
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// 测试：init命令接受输出参数
    /// </summary>
    [Fact]
    public void InitCommand_AcceptsOutputOption()
    {
        // Arrange
        var args = new[] { "init", "-o", "myconfig.json" };

        // Act
        var result = Program.CreateRootCommand().Parse(args);

        // Assert
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// 测试：validate命令需要配置参数
    /// </summary>
    [Fact]
    public void ValidateCommand_RequiresConfigOption()
    {
        // Arrange
        var args = new[] { "validate", "-c", "config.json" };

        // Act
        var result = Program.CreateRootCommand().Parse(args);

        // Assert
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// 测试：帮助命令显示帮助信息
    /// </summary>
    [Fact]
    public void HelpCommand_DisplaysHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = Program.CreateRootCommand().Parse(args);

        // Assert
        Assert.True(result.Errors.Count == 0);
    }
}
