using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TdxDllGenerator;

/// <summary>
/// 表达式代码生成器 - 将 AST 转换为 C++ 代码
/// </summary>
public class ExpressionCodeGenerator
{
    private readonly StringBuilder _sb;
    private readonly string _funcName;
    private int _tempVarCounter = 0;
    private readonly HashSet<string> _generatedHelpers = new();

    public ExpressionCodeGenerator(string funcName)
    {
        _sb = new StringBuilder();
        _funcName = funcName;
    }

    /// <summary>
    /// 生成完整的函数代码
    /// </summary>
    public string GenerateFunctionCode(AstNode ast)
    {
        _sb.Clear();
        _tempVarCounter = 0;
        _generatedHelpers.Clear();

        // 第一遍：仅用于统计临时变量数量，避免固定数量声明导致编译失败
        _ = GenerateNodeCode(ast, "pfOut");
        var requiredTempVars = _tempVarCounter;

        // 第二遍：正式生成代码
        _sb.Clear();
        _tempVarCounter = 0;

        // 生成数据序列映射
        _sb.AppendLine("    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)");
        _sb.AppendLine("        return;");
        _sb.AppendLine();
        _sb.AppendLine("    // 数据序列映射");
        _sb.AppendLine("    float* CLOSE = pfIn1;");
        _sb.AppendLine("    float* C = pfIn1;");
        _sb.AppendLine("    float* OPEN = pfIn2;");
        _sb.AppendLine("    float* O = pfIn2;");
        _sb.AppendLine("    float* HIGH = pfIn3;");
        _sb.AppendLine("    float* H = pfIn3;");
        _sb.AppendLine("    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理");
        _sb.AppendLine("    float* L = pfIn3;");
        _sb.AppendLine("    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入");
        _sb.AppendLine("    float* V = pfIn2;");
        _sb.AppendLine();

        // 分析 AST 需要的辅助函数
        AnalyzeAst(ast);

        // 生成辅助函数声明
        GenerateHelperFunctions();

        // 按实际需求声明临时变量
        _sb.AppendLine("    // 临时变量声明");
        for (int i = 0; i < requiredTempVars; i++)
        {
            _sb.AppendLine($"    std::vector<float> _temp_{i}(nDataLen);");
        }
        _sb.AppendLine();

        // 生成主计算逻辑
        _sb.AppendLine("    // 计算输出");
        var resultVar = GenerateNodeCode(ast, "pfOut");
        
        if (resultVar != "pfOut")
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"        pfOut[i] = {resultVar}[i];");
        }

        // 应用安全腐蚀
        _sb.AppendLine();
        _sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
        _sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine("    {");
        _sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
        _sb.AppendLine("    }");

        return _sb.ToString();
    }

    /// <summary>
    /// 分析 AST，确定需要的辅助函数
    /// </summary>
    private void AnalyzeAst(AstNode node)
    {
        if (node.Type == AstNodeType.FunctionCall)
        {
            var funcName = node.FunctionName?.ToUpperInvariant();
            if (!string.IsNullOrEmpty(funcName))
            {
                _generatedHelpers.Add(funcName);
            }
        }

        foreach (var child in node.Children)
        {
            AnalyzeAst(child);
        }
    }

    /// <summary>
    /// 生成辅助函数
    /// </summary>
    private void GenerateHelperFunctions()
    {
        foreach (var func in _generatedHelpers.OrderBy(f => f))
        {
            switch (func)
            {
                case "MA":
                    GenerateMAHelper();
                    break;
                case "EMA":
                    GenerateEMAHelper();
                    break;
                case "SMA":
                    GenerateSMAHelper();
                    break;
                case "STD":
                    GenerateSTDHelper();
                    break;
                case "HHV":
                    GenerateHHVHelper();
                    break;
                case "LLV":
                    GenerateLLVHelper();
                    break;
                case "SUM":
                    GenerateSUMHelper();
                    break;
                case "REF":
                    GenerateREFHelper();
                    break;
                case "ABS":
                case "MAX":
                case "MIN":
                case "IF":
                case "CROSS":
                    // 这些函数内联生成，不需要辅助函数
                    break;
            }
        }
    }

    /// <summary>
    /// 生成 AST 节点的代码，返回结果数组变量名
    /// </summary>
    private string GenerateNodeCode(AstNode node, string targetVar)
    {
        switch (node.Type)
        {
            case AstNodeType.Number:
                return GenerateNumber(node, targetVar);

            case AstNodeType.Identifier:
                return GenerateIdentifier(node);

            case AstNodeType.FunctionCall:
                return GenerateFunctionCall(node, targetVar);

            case AstNodeType.BinaryOp:
                return GenerateBinaryOp(node, targetVar);

            case AstNodeType.UnaryOp:
                return GenerateUnaryOp(node, targetVar);

            default:
                throw new InvalidOperationException($"未知的 AST 节点类型: {node.Type}");
        }
    }

    /// <summary>
    /// 生成数字常量
    /// </summary>
    private string GenerateNumber(AstNode node, string targetVar)
    {
        var value = node.Value ?? "0";
        // 确保浮点数格式正确：2 -> 2.0f, 2.5 -> 2.5f
        var floatValue = value.Contains(".") ? $"{value}f" : $"{value}.0f";
        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {floatValue};");
        return targetVar;
    }

    /// <summary>
    /// 生成标识符（数据序列）
    /// </summary>
    private string GenerateIdentifier(AstNode node)
    {
        return node.Value?.ToUpperInvariant() switch
        {
            "CLOSE" or "C" => "CLOSE",
            "OPEN" or "O" => "OPEN",
            "HIGH" or "H" => "HIGH",
            "LOW" or "L" => "LOW",
            "VOL" or "V" => "VOL",
            _ => throw new InvalidOperationException($"未知的数据序列: {node.Value}")
        };
    }

    /// <summary>
    /// 生成函数调用
    /// </summary>
    private string GenerateFunctionCall(AstNode node, string targetVar)
    {
        var funcName = node.FunctionName?.ToUpperInvariant();
        
        switch (funcName)
        {
            case "MA":
                return GenerateMA(node, targetVar);
            case "EMA":
                return GenerateEMA(node, targetVar);
            case "SMA":
                return GenerateSMA(node, targetVar);
            case "STD":
                return GenerateSTD(node, targetVar);
            case "HHV":
                return GenerateHHV(node, targetVar);
            case "LLV":
                return GenerateLLV(node, targetVar);
            case "SUM":
                return GenerateSUM(node, targetVar);
            case "REF":
                return GenerateREF(node, targetVar);
            case "ABS":
                return GenerateABS(node, targetVar);
            case "MAX":
                return GenerateMAX(node, targetVar);
            case "MIN":
                return GenerateMIN(node, targetVar);
            case "IF":
                return GenerateIF(node, targetVar);
            case "CROSS":
                return GenerateCROSS(node, targetVar);
            default:
                throw new InvalidOperationException($"未实现的函数: {funcName}");
        }
    }

    /// <summary>
    /// 生成二元运算
    /// </summary>
    private string GenerateBinaryOp(AstNode node, string targetVar)
    {
        var leftVar = GetTempVar();
        var rightVar = GetTempVar();

        // 生成左操作数
        var leftResult = GenerateNodeCode(node.Children[0], leftVar);
        if (leftResult != leftVar)
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {leftVar}[i] = {leftResult}[i];");
        }

        // 生成右操作数
        var rightResult = GenerateNodeCode(node.Children[1], rightVar);
        if (rightResult != rightVar)
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {rightVar}[i] = {rightResult}[i];");
        }

        // 生成运算
        var op = node.Operator switch
        {
            "+" => "+",
            "-" => "-",
            "*" => "*",
            "/" => "/",
            "%" => "%",
            "=" or "==" => "==",
            "<>" or "!=" => "!=",
            ">" => ">",
            "<" => "<",
            ">=" => ">=",
            "<=" => "<=",
            "AND" or "&&" => "&&",
            "OR" or "||" => "||",
            _ => throw new InvalidOperationException($"未知的运算符: {node.Operator}")
        };

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {leftVar}[i] {op} {rightVar}[i];");

        return targetVar;
    }

    /// <summary>
    /// 生成一元运算
    /// </summary>
    private string GenerateUnaryOp(AstNode node, string targetVar)
    {
        var operandVar = GetTempVar();
        var operandResult = GenerateNodeCode(node.Children[0], operandVar);
        
        if (operandResult != operandVar)
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {operandVar}[i] = {operandResult}[i];");
        }

        var op = node.Operator switch
        {
            "+" => "",
            "-" => "-",
            "NOT" => "!",
            _ => throw new InvalidOperationException($"未知的一元运算符: {node.Operator}")
        };

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {op}{operandVar}[i];");
        
        return targetVar;
    }

    #region Helper Functions Generation

    private void GenerateMAHelper()
    {
        // MA 逻辑内联在 GenerateMA 中
    }

    private void GenerateEMAHelper()
    {
        // EMA 逻辑内联在 GenerateEMA 中
    }

    private void GenerateSMAHelper()
    {
        // SMA 逻辑内联在 GenerateSMA 中
    }

    private void GenerateSTDHelper()
    {
        // STD 逻辑内联在 GenerateSTD 中
    }

    private void GenerateHHVHelper()
    {
        // HHV 逻辑内联在 GenerateHHV 中
    }

    private void GenerateLLVHelper()
    {
        // LLV 逻辑内联在 GenerateLLV 中
    }

    private void GenerateSUMHelper()
    {
        // SUM 逻辑内联在 GenerateSUM 中
    }

    private void GenerateREFHelper()
    {
        // REF 逻辑内联在 GenerateREF 中
    }

    #endregion

    #region Function Implementations

    private string GenerateMA(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";
        
        // 解析数据参数
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        float sum = 0;");
        _sb.AppendLine($"        int count = 0;");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"        {{");
        _sb.AppendLine($"            sum += {dataVar}[j];");
        _sb.AppendLine($"            count++;");
        _sb.AppendLine($"        }}");
        _sb.AppendLine($"        {targetVar}[i] = count > 0 ? sum / count : 0;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateEMA(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var alphaVarName = $"alpha_{_tempVarCounter++}";
        
        // 解析数据参数
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            float alpha = 2.0f / (period + 1);
            _sb.AppendLine($"    float {alphaVarName} = {alpha:F10}f;");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    float {alphaVarName} = 2.0f / ((int){periodVar}[0] + 1);");
        }

        _sb.AppendLine($"    if (nDataLen > 0)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        {targetVar}[0] = {dataVar}[0];");
        _sb.AppendLine($"        for (int i = 1; i < nDataLen; i++)");
        _sb.AppendLine($"            {targetVar}[i] = {alphaVarName} * {dataVar}[i] + (1 - {alphaVarName}) * {targetVar}[i - 1];");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateSMA(AstNode node, string targetVar)
    {
        // SMA(X, N, M) = M/N * X + (N-M)/N * SMA[X, N, M]_{t-1}
        var dataVar = GetTempVar();
        var nVarName = $"n_{_tempVarCounter++}";
        var mVarName = $"m_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析 N 参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int n = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {nVarName} = {n};");
        }
        else
        {
            var nVar = GetTempVar();
            var nResult = GenerateNodeCode(node.Children[1], nVar);
            if (nResult != nVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {nVar}[i] = {nResult}[i];");
            _sb.AppendLine($"    int {nVarName} = (int){nVar}[0];");
        }

        // 解析 M 参数
        if (node.Children[2].Type == AstNodeType.Number)
        {
            int m = (int)float.Parse(node.Children[2].Value!);
            _sb.AppendLine($"    int {mVarName} = {m};");
        }
        else
        {
            var mVar = GetTempVar();
            var mResult = GenerateNodeCode(node.Children[2], mVar);
            if (mResult != mVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {mVar}[i] = {mResult}[i];");
            _sb.AppendLine($"    int {mVarName} = (int){mVar}[0];");
        }

        _sb.AppendLine($"    if (nDataLen > 0)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        {targetVar}[0] = {dataVar}[0];");
        _sb.AppendLine($"        for (int i = 1; i < nDataLen; i++)");
        _sb.AppendLine($"            {targetVar}[i] = ({mVarName} * {dataVar}[i] + ({nVarName} - {mVarName}) * {targetVar}[i - 1]) / {nVarName};");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateSTD(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        float sum = 0, sumSq = 0;");
        _sb.AppendLine($"        int count = 0;");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"        {{");
        _sb.AppendLine($"            sum += {dataVar}[j];");
        _sb.AppendLine($"            sumSq += {dataVar}[j] * {dataVar}[j];");
        _sb.AppendLine($"            count++;");
        _sb.AppendLine($"        }}");
        _sb.AppendLine($"        if (count > 1)");
        _sb.AppendLine($"        {{");
        _sb.AppendLine($"            float mean = sum / count;");
        _sb.AppendLine($"            {targetVar}[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));");
        _sb.AppendLine($"        }}");
        _sb.AppendLine($"        else {targetVar}[i] = 0;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateHHV(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        float maxVal = {dataVar}[i];");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"            if ({dataVar}[j] > maxVal) maxVal = {dataVar}[j];");
        _sb.AppendLine($"        {targetVar}[i] = maxVal;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateLLV(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        float minVal = {dataVar}[i];");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"            if ({dataVar}[j] < minVal) minVal = {dataVar}[j];");
        _sb.AppendLine($"        {targetVar}[i] = minVal;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateSUM(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        float sum = 0;");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"            sum += {dataVar}[j];");
        _sb.AppendLine($"        {targetVar}[i] = sum;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateREF(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var periodVarName = $"nRef_{_tempVarCounter++}";
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    int {periodVarName} = {period};");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    int {periodVarName} = (int){periodVar}[0];");
        }

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        int j = i - {periodVarName};");
        _sb.AppendLine($"        {targetVar}[i] = (j >= 0) ? {dataVar}[j] : {dataVar}[0];");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateABS(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = fabsf({dataVar}[i]);");
        return targetVar;
    }

    private string GenerateMAX(AstNode node, string targetVar)
    {
        var leftVar = GetTempVar();
        var rightVar = GetTempVar();
        
        var leftResult = GenerateNodeCode(node.Children[0], leftVar);
        if (leftResult != leftVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {leftVar}[i] = {leftResult}[i];");

        var rightResult = GenerateNodeCode(node.Children[1], rightVar);
        if (rightResult != rightVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {rightVar}[i] = {rightResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = fmaxf({leftVar}[i], {rightVar}[i]);");
        return targetVar;
    }

    private string GenerateMIN(AstNode node, string targetVar)
    {
        var leftVar = GetTempVar();
        var rightVar = GetTempVar();
        
        var leftResult = GenerateNodeCode(node.Children[0], leftVar);
        if (leftResult != leftVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {leftVar}[i] = {leftResult}[i];");

        var rightResult = GenerateNodeCode(node.Children[1], rightVar);
        if (rightResult != rightVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {rightVar}[i] = {rightResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = fminf({leftVar}[i], {rightVar}[i]);");
        return targetVar;
    }

    private string GenerateIF(AstNode node, string targetVar)
    {
        var condVar = GetTempVar();
        var trueVar = GetTempVar();
        var falseVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        var trueResult = GenerateNodeCode(node.Children[1], trueVar);
        if (trueResult != trueVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {trueVar}[i] = {trueResult}[i];");

        var falseResult = GenerateNodeCode(node.Children[2], falseVar);
        if (falseResult != falseVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {falseVar}[i] = {falseResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"        {targetVar}[i] = {condVar}[i] ? {trueVar}[i] : {falseVar}[i];");

        return targetVar;
    }

    private string GenerateCROSS(AstNode node, string targetVar)
    {
        var leftVar = GetTempVar();
        var rightVar = GetTempVar();

        var leftResult = GenerateNodeCode(node.Children[0], leftVar);
        if (leftResult != leftVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {leftVar}[i] = {leftResult}[i];");

        var rightResult = GenerateNodeCode(node.Children[1], rightVar);
        if (rightResult != rightVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {rightVar}[i] = {rightResult}[i];");

        _sb.AppendLine($"    {targetVar}[0] = 0.0f;");
        _sb.AppendLine($"    for (int i = 1; i < nDataLen; i++)");
        _sb.AppendLine($"        {targetVar}[i] = ({leftVar}[i - 1] <= {rightVar}[i - 1] && {leftVar}[i] > {rightVar}[i]) ? 1.0f : 0.0f;");

        return targetVar;
    }

    #endregion

    #region Utility Methods

    private string GetTempVar()
    {
        return $"_temp_{_tempVarCounter++}";
    }

    /// <summary>
    /// 尝试获取节点的值（如果是常量）
    /// </summary>
    private string GetNodeValue(AstNode node)
    {
        if (node.Type == AstNodeType.Number)
        {
            return $"{node.Value}";
        }
        
        // 对于非常量，需要生成代码并返回临时变量名
        var tempVar = GetTempVar();
        var result = GenerateNodeCode(node, tempVar);
        return result;
    }

    #endregion
}
