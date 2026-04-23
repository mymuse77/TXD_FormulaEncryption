using Xunit;
using TdxDllGenerator;

namespace TdxDllGenerator.Tests;

public class ExpressionCodeGeneratorTests
{
    [Fact]
    public void GenerateFunctionCode_UsesDynamicTempBuffers_InsteadOfFixed256Arrays()
    {
        var parser = new ExpressionParser();
        var ast = parser.Parse("MA(CLOSE, 10)");
        var generator = new ExpressionCodeGenerator("MA10");

        var code = generator.GenerateFunctionCode(ast);

        Assert.Contains("std::vector<float> _temp_0(nDataLen);", code);
        Assert.DoesNotContain("[256]", code);
        Assert.Contains("if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)", code);
    }

    [Fact]
    public void GenerateFunctionCode_DeclaresAllTempBuffers_WhenExpressionNeedsMoreThan20()
    {
        var parser = new ExpressionParser();
        var ast = parser.Parse(
            "SUM(IF(((HIGH+LOW+CLOSE)/3)>REF((HIGH+LOW+CLOSE)/3,1),((HIGH+LOW+CLOSE)/3)*VOL,0),14)/SUM(IF(((HIGH+LOW+CLOSE)/3)<REF((HIGH+LOW+CLOSE)/3,1),((HIGH+LOW+CLOSE)/3)*VOL,0),14)"
        );
        var generator = new ExpressionCodeGenerator("MFI");

        var code = generator.GenerateFunctionCode(ast);

        Assert.Contains("std::vector<float> _temp_20(nDataLen);", code);
        Assert.Contains("_temp_20", code);
    }
}
