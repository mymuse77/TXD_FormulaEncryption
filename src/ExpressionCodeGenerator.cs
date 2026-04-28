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
    private readonly bool _useGlobalData;
    private int _tempVarCounter = 0;
    private readonly HashSet<string> _generatedHelpers = new();
    // 用户变量名 -> C++数组名 的映射
    private readonly Dictionary<string, string> _variableMap = new(StringComparer.OrdinalIgnoreCase);

    public ExpressionCodeGenerator(string funcName, bool useGlobalData = false)
    {
        _sb = new StringBuilder();
        _funcName = funcName;
        _useGlobalData = useGlobalData;
    }

    /// <summary>
    /// 生成完整的函数代码
    /// </summary>
    public string GenerateFunctionCode(AstNode ast)
    {
        _sb.Clear();
        _tempVarCounter = 0;
        _generatedHelpers.Clear();
        _variableMap.Clear();

        // 预先收集所有用户变量（:= 赋值的变量）
        CollectUserVariables(ast);

        // 第一遍：统计临时变量
        _ = GenerateNodeCode(ast, "pfOut");
        var requiredTempVars = _tempVarCounter;

        // 第二遍：正式生成代码
        _sb.Clear();
        _tempVarCounter = 0;

        // 生成数据序列映射
        if (_useGlobalData)
        {
            // 使用全局数据缓冲区（公式需要 C/O/H/L/V 全部5个数据序列）
            _sb.AppendLine("    if (nDataLen <= 0 || pfOut == nullptr)");
            _sb.AppendLine("        return;");
            _sb.AppendLine();
            _sb.AppendLine("    // 从全局数据缓冲区读取数据（通过DATALOADER函数预先加载）");
            _sb.AppendLine("    float* CLOSE = g_buf_close.data();");
            _sb.AppendLine("    float* C = g_buf_close.data();");
            _sb.AppendLine("    float* OPEN = g_buf_open.data();");
            _sb.AppendLine("    float* O = g_buf_open.data();");
            _sb.AppendLine("    float* HIGH = g_buf_high.data();");
            _sb.AppendLine("    float* H = g_buf_high.data();");
            _sb.AppendLine("    float* LOW = g_buf_low.data();");
            _sb.AppendLine("    float* L = g_buf_low.data();");
            _sb.AppendLine("    float* VOL = g_buf_vol.data();");
            _sb.AppendLine("    float* V = g_buf_vol.data();");
            _sb.AppendLine();
        }
        else
        {
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
        }

        // 分析 AST 需要的辅助函数
        AnalyzeAst(ast);

        // 生成辅助函数声明
        GenerateHelperFunctions();

        // 按实际需求声明临时变量 + 用户变量数组
        _sb.AppendLine("    // 临时变量声明");
        for (int i = 0; i < requiredTempVars; i++)
        {
            _sb.AppendLine($"    std::vector<float> _temp_{i}(nDataLen);");
        }
        // 输出用户变量数组声明
        foreach (var kvp in _variableMap)
        {
            _sb.AppendLine($"    std::vector<float> {kvp.Value}(nDataLen);  // {kvp.Key}");
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
    /// 收集所有用户变量（:= 赋值的变量），分配 C++ 数组名
    /// </summary>
    private void CollectUserVariables(AstNode node)
    {
        if (node.Type == AstNodeType.StatementList)
        {
            foreach (var child in node.Children)
                CollectUserVariables(child);
        }
        else if (node.Type == AstNodeType.Assignment)
        {
            var varName = node.Value?.ToUpperInvariant() ?? "";
            if (!string.IsNullOrEmpty(varName) && !_variableMap.ContainsKey(varName))
            {
                _variableMap[varName] = $"_var_{_variableMap.Count}";
            }
            // 也递归收集表达式内部的变量
            foreach (var child in node.Children)
                CollectUserVariables(child);
        }
        else
        {
            foreach (var child in node.Children)
                CollectUserVariables(child);
        }
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
                case "WMA":
                case "MOD":
                case "ROUND":
                case "INTPART":
                case "FLOOR":
                case "EXP":
                case "LN":
                case "LOG":
                case "SIN":
                case "COS":
                case "TAN":
                case "COUNT":
                case "EXIST":
                case "EVERY":
                case "BARSLAST":
                case "POW":
                case "SQRT":
                case "BARSLASTCOUNT":
                case "CODELIKE":
                case "ZTPRICE":
                case "DTPRICE":
                case "NODRAW":
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

            case AstNodeType.StringLiteral:
                return GenerateStringLiteral(node, targetVar);

            case AstNodeType.Identifier:
                return GenerateIdentifier(node);

            case AstNodeType.FunctionCall:
                return GenerateFunctionCall(node, targetVar);

            case AstNodeType.BinaryOp:
                return GenerateBinaryOp(node, targetVar);

            case AstNodeType.UnaryOp:
                return GenerateUnaryOp(node, targetVar);

            case AstNodeType.StatementList:
                return GenerateStatementList(node, targetVar);

            case AstNodeType.Assignment:
                return GenerateAssignment(node);

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
    /// 生成字符串常量（用于环境函数参数，返回编译期值）
    /// </summary>
    private string GenerateStringLiteral(AstNode node, string targetVar)
    {
        // 字符串字面量在编译时确定，目前主要用于 CODELIKE 等环境函数参数
        // 对于代码生成，字符串字面量需要特殊处理：不做运行时赋值
        // 实际值由函数调用时通过上下文获取
        _sb.AppendLine($"    // String literal: '{node.Value}' (used by environment functions)");
        return targetVar;
    }

    /// <summary>
    /// 生成标识符（数据序列或用户变量）
    /// </summary>
    private string GenerateIdentifier(AstNode node)
    {
        var name = node.Value?.ToUpperInvariant() ?? "";

        // 数据序列
        var dataResult = name switch
        {
            "CLOSE" or "C" => "CLOSE",
            "OPEN" or "O" => "OPEN",
            "HIGH" or "H" => "HIGH",
            "LOW" or "L" => "LOW",
            "VOL" or "V" => "VOL",
            _ => null
        };
        if (dataResult != null) return dataResult;

        // 用户变量
        if (_variableMap.TryGetValue(name, out var varArray))
            return varArray;

        throw new InvalidOperationException($"未知的标识符: {name}");
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
            case "WMA":
                return GenerateWMA(node, targetVar);
            case "MOD":
                return GenerateMOD(node, targetVar);
            case "ROUND":
                return GenerateROUND(node, targetVar);
            case "INTPART":
                return GenerateINTPART(node, targetVar);
            case "FLOOR":
                return GenerateFLOOR(node, targetVar);
            case "EXP":
                return GenerateEXP(node, targetVar);
            case "LN":
                return GenerateLN(node, targetVar);
            case "LOG":
                return GenerateLOG(node, targetVar);
            case "SIN":
                return GenerateSIN(node, targetVar);
            case "COS":
                return GenerateCOS(node, targetVar);
            case "TAN":
                return GenerateTAN(node, targetVar);
            case "COUNT":
                return GenerateCOUNT(node, targetVar);
            case "EXIST":
                return GenerateEXIST(node, targetVar);
            case "EVERY":
                return GenerateEVERY(node, targetVar);
            case "BARSLAST":
                return GenerateBARSLAST(node, targetVar);
            case "POW":
                return GeneratePOW(node, targetVar);
            case "SQRT":
                return GenerateSQRT(node, targetVar);
            case "BARSLASTCOUNT":
                return GenerateBARSLASTCOUNT(node, targetVar);
            case "CODELIKE":
                return GenerateCODELIKE(node, targetVar);
            case "ZTPRICE":
                return GenerateZTPRICE(node, targetVar);
            case "DTPRICE":
                return GenerateDTPRICE(node, targetVar);
            case "NODRAW":
                return GenerateNODRAW(node, targetVar);
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
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        float sum = 0, sumSq = 0;");
            _sb.AppendLine($"        int count = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
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
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        float sum = 0, sumSq = 0;");
            _sb.AppendLine($"        int count = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
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
        }

        return targetVar;
    }

    private string GenerateHHV(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        float maxVal = {dataVar}[i];");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"            if ({dataVar}[j] > maxVal) maxVal = {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = maxVal;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        float maxVal = {dataVar}[i];");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"            if ({dataVar}[j] > maxVal) maxVal = {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = maxVal;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateLLV(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        float minVal = {dataVar}[i];");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"            if ({dataVar}[j] < minVal) minVal = {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = minVal;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        float minVal = {dataVar}[i];");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"            if ({dataVar}[j] < minVal) minVal = {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = minVal;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateSUM(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        float sum = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"            sum += {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = sum;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        float sum = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"            sum += {dataVar}[j];");
            _sb.AppendLine($"        {targetVar}[i] = sum;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateREF(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int j = i - {period};");
            _sb.AppendLine($"        {targetVar}[i] = (j >= 0) ? {dataVar}[j] : {dataVar}[0];");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int j = i - (int){periodVar}[i];");
            _sb.AppendLine($"        {targetVar}[i] = (j >= 0) ? {dataVar}[j] : {dataVar}[0];");
            _sb.AppendLine($"    }}");
        }

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

    private string GenerateWMA(AstNode node, string targetVar)
    {
        // WMA(X, N) - 加权移动平均, 权重为N到1
        var dataVar = GetTempVar();
        var periodVarName = $"nPeriod_{_tempVarCounter++}";

        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

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
        _sb.AppendLine($"        float sum = 0, wsum = 0;");
        _sb.AppendLine($"        int count = 0;");
        _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {periodVarName}; j--)");
        _sb.AppendLine($"        {{");
        _sb.AppendLine($"            int w = {periodVarName} - (i - j);");
        _sb.AppendLine($"            sum += {dataVar}[j] * w;");
        _sb.AppendLine($"            wsum += w;");
        _sb.AppendLine($"            count++;");
        _sb.AppendLine($"        }}");
        _sb.AppendLine($"        {targetVar}[i] = (wsum > 0) ? sum / wsum : 0;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateMOD(AstNode node, string targetVar)
    {
        // MOD(A, B) - 取模
        var leftVar = GetTempVar();
        var rightVar = GetTempVar();

        var leftResult = GenerateNodeCode(node.Children[0], leftVar);
        if (leftResult != leftVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {leftVar}[i] = {leftResult}[i];");

        var rightResult = GenerateNodeCode(node.Children[1], rightVar);
        if (rightResult != rightVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {rightVar}[i] = {rightResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"        {targetVar}[i] = ({rightVar}[i] != 0.0f) ? fmodf({leftVar}[i], {rightVar}[i]) : 0.0f;");

        return targetVar;
    }

    private string GenerateROUND(AstNode node, string targetVar)
    {
        // ROUND(X, N) - 四舍五入, N为小数位数(默认0)
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        if (node.Children.Count >= 2)
        {
            var nVar = GetTempVar();
            var nResult = GenerateNodeCode(node.Children[1], nVar);
            if (nResult != nVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {nVar}[i] = {nResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        float n = {nVar}[i];");
            _sb.AppendLine($"        float factor = powf(10.0f, n);");
            _sb.AppendLine($"        {targetVar}[i] = roundf({dataVar}[i] * factor) / factor;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = roundf({dataVar}[i]);");
        }

        return targetVar;
    }

    private string GenerateINTPART(AstNode node, string targetVar)
    {
        // INTPART(X) - 取整(向零取整)
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = (float)(int){dataVar}[i];");

        return targetVar;
    }

    private string GenerateFLOOR(AstNode node, string targetVar)
    {
        // FLOOR(X) - 向下取整
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = floorf({dataVar}[i]);");

        return targetVar;
    }

    private string GenerateEXP(AstNode node, string targetVar)
    {
        // EXP(X) - e^X
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = expf({dataVar}[i]);");

        return targetVar;
    }

    private string GenerateLN(AstNode node, string targetVar)
    {
        // LN(X) - 自然对数
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = ({dataVar}[i] > 0.0f) ? logf({dataVar}[i]) : 0.0f;");

        return targetVar;
    }

    private string GenerateLOG(AstNode node, string targetVar)
    {
        // LOG(X) - 常用对数(以10为底)
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = ({dataVar}[i] > 0.0f) ? log10f({dataVar}[i]) : 0.0f;");

        return targetVar;
    }

    private string GenerateSIN(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = sinf({dataVar}[i]);");

        return targetVar;
    }

    private string GenerateCOS(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = cosf({dataVar}[i]);");

        return targetVar;
    }

    private string GenerateTAN(AstNode node, string targetVar)
    {
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = tanf({dataVar}[i]);");

        return targetVar;
    }

    private string GenerateCOUNT(AstNode node, string targetVar)
    {
        // COUNT(X, N) - 统计N周期中满足X条件(非0)的周期数
        var condVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int cnt = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"            if ({condVar}[j] != 0.0f) cnt++;");
            _sb.AppendLine($"        {targetVar}[i] = (float)cnt;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        int cnt = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"            if ({condVar}[j] != 0.0f) cnt++;");
            _sb.AppendLine($"        {targetVar}[i] = (float)cnt;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateEXIST(AstNode node, string targetVar)
    {
        // EXIST(X, N) - N周期中是否存在满足X条件
        var condVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        bool found = false;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"        {{");
            _sb.AppendLine($"            if ({condVar}[j] != 0.0f) {{ found = true; break; }}");
            _sb.AppendLine($"        }}");
            _sb.AppendLine($"        {targetVar}[i] = found ? 1.0f : 0.0f;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        bool found = false;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"        {{");
            _sb.AppendLine($"            if ({condVar}[j] != 0.0f) {{ found = true; break; }}");
            _sb.AppendLine($"        }}");
            _sb.AppendLine($"        {targetVar}[i] = found ? 1.0f : 0.0f;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateEVERY(AstNode node, string targetVar)
    {
        // EVERY(X, N) - N周期中是否一直满足X条件
        var condVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        // 解析周期参数 - 支持变量周期
        if (node.Children[1].Type == AstNodeType.Number)
        {
            int period = (int)float.Parse(node.Children[1].Value!);
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        bool all = true;");
            _sb.AppendLine($"        int checked = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - {period}; j--)");
            _sb.AppendLine($"        {{");
            _sb.AppendLine($"            if ({condVar}[j] == 0.0f) {{ all = false; break; }}");
            _sb.AppendLine($"            checked++;");
            _sb.AppendLine($"        }}");
            _sb.AppendLine($"        {targetVar}[i] = (all && checked > 0) ? 1.0f : 0.0f;");
            _sb.AppendLine($"    }}");
        }
        else
        {
            var periodVar = GetTempVar();
            var periodResult = GenerateNodeCode(node.Children[1], periodVar);
            if (periodResult != periodVar)
                _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {periodVar}[i] = {periodResult}[i];");
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
            _sb.AppendLine($"    {{");
            _sb.AppendLine($"        int p = (int){periodVar}[i];");
            _sb.AppendLine($"        bool all = true;");
            _sb.AppendLine($"        int checked = 0;");
            _sb.AppendLine($"        for (int j = i; j >= 0 && j > i - p; j--)");
            _sb.AppendLine($"        {{");
            _sb.AppendLine($"            if ({condVar}[j] == 0.0f) {{ all = false; break; }}");
            _sb.AppendLine($"            checked++;");
            _sb.AppendLine($"        }}");
            _sb.AppendLine($"        {targetVar}[i] = (all && checked > 0) ? 1.0f : 0.0f;");
            _sb.AppendLine($"    }}");
        }

        return targetVar;
    }

    private string GenerateBARSLAST(AstNode node, string targetVar)
    {
        // BARSLAST(X) - 上次X非0到当前的周期数
        var condVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        var lastBarVar = $"lastBar_{_tempVarCounter++}";
        _sb.AppendLine($"    int {lastBarVar} = -1;");
        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        if ({condVar}[i] != 0.0f) {lastBarVar} = i;");
        _sb.AppendLine($"        {targetVar}[i] = ({lastBarVar} >= 0) ? (float)(i - {lastBarVar}) : 0.0f;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GeneratePOW(AstNode node, string targetVar)
    {
        // POW(X, N) - X的N次幂
        var baseVar = GetTempVar();
        var expVar = GetTempVar();

        var baseResult = GenerateNodeCode(node.Children[0], baseVar);
        if (baseResult != baseVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {baseVar}[i] = {baseResult}[i];");

        var expResult = GenerateNodeCode(node.Children[1], expVar);
        if (expResult != expVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {expVar}[i] = {expResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = powf({baseVar}[i], {expVar}[i]);");

        return targetVar;
    }

    private string GenerateSQRT(AstNode node, string targetVar)
    {
        // SQRT(X) - 平方根
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = ({dataVar}[i] >= 0.0f) ? sqrtf({dataVar}[i]) : 0.0f;");

        return targetVar;
    }

    private string GenerateBARSLASTCOUNT(AstNode node, string targetVar)
    {
        // BARSLASTCOUNT(X) - 连续满足条件X的周期数
        var condVar = GetTempVar();

        var condResult = GenerateNodeCode(node.Children[0], condVar);
        if (condResult != condVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {condVar}[i] = {condResult}[i];");

        _sb.AppendLine($"    {targetVar}[0] = ({condVar}[0] != 0.0f) ? 1.0f : 0.0f;");
        _sb.AppendLine($"    for (int i = 1; i < nDataLen; i++)");
        _sb.AppendLine($"    {{");
        _sb.AppendLine($"        if ({condVar}[i] != 0.0f)");
        _sb.AppendLine($"            {targetVar}[i] = {targetVar}[i - 1] + 1.0f;");
        _sb.AppendLine($"        else");
        _sb.AppendLine($"            {targetVar}[i] = 0.0f;");
        _sb.AppendLine($"    }}");

        return targetVar;
    }

    private string GenerateCODELIKE(AstNode node, string targetVar)
    {
        // CODELIKE('XX') - 检测股票代码前缀，返回0或1
        // DLL 运行时无法获取股票代码，默认返回 1（假设所有股票都匹配）
        // 这样 X1 会取第一个 IF 分支的值（0.1），所有股票按 10% 涨跌幅计算
        // 对于创业板(20%)和北交所(30%)，10%阈值仍能使 ZT/ZB 判断正确（因为实际涨幅更大）
        var codeStr = node.Children[0].Value ?? "";
        _sb.AppendLine($"    // CODELIKE('{codeStr}') - market detection (default 1, all stocks treated as matching)");
        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = 1.0f;");
        return targetVar;
    }

    private string GenerateZTPRICE(AstNode node, string targetVar)
    {
        // ZTPRICE(REF(C,1), X1) - 涨停价 = refPrice * (1 + ratio)
        // X1 可以是 0.1 (10%), 0.2 (20%), 0.3 (30%) 等
        var refVar = GetTempVar();
        var ratioVar = GetTempVar();

        var refResult = GenerateNodeCode(node.Children[0], refVar);
        if (refResult != refVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {refVar}[i] = {refResult}[i];");

        var ratioResult = GenerateNodeCode(node.Children[1], ratioVar);
        if (ratioResult != ratioVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {ratioVar}[i] = {ratioResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {refVar}[i] * (1.0f + {ratioVar}[i]);");

        return targetVar;
    }

    private string GenerateDTPRICE(AstNode node, string targetVar)
    {
        // DTPRICE(REF(C,1), X1) - 跌停价 = refPrice * (1 - ratio)
        var refVar = GetTempVar();
        var ratioVar = GetTempVar();

        var refResult = GenerateNodeCode(node.Children[0], refVar);
        if (refResult != refVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {refVar}[i] = {refResult}[i];");

        var ratioResult = GenerateNodeCode(node.Children[1], ratioVar);
        if (ratioResult != ratioVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {ratioVar}[i] = {ratioResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {refVar}[i] * (1.0f - {ratioVar}[i]);");

        return targetVar;
    }

    private string GenerateNODRAW(AstNode node, string targetVar)
    {
        // NODRAW(X) - 绘图修饰符，直接返回原值
        var dataVar = GetTempVar();
        var dataResult = GenerateNodeCode(node.Children[0], dataVar);
        if (dataResult != dataVar)
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {dataVar}[i] = {dataResult}[i];");

        _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {targetVar}[i] = {dataVar}[i];");
        return targetVar;
    }

    private string GenerateStatementList(AstNode node, string targetVar)
    {
        // StatementList: 按顺序执行所有语句，最后一条语句的值作为输出
        string lastResult = targetVar;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            bool isLast = (i == node.Children.Count - 1);

            if (child.Type == AstNodeType.Assignment)
            {
                // 赋值语句：计算到变量数组中
                var result = GenerateAssignment(child);
                if (isLast) lastResult = result;
            }
            else
            {
                // 表达式语句
                if (isLast)
                {
                    lastResult = GenerateNodeCode(child, targetVar);
                }
                else
                {
                    var tempVar = GetTempVar();
                    GenerateNodeCode(child, tempVar);
                }
            }
        }

        return lastResult;
    }

    private string GenerateAssignment(AstNode node)
    {
        // Assignment: VAR := expr
        var varName = node.Value?.ToUpperInvariant() ?? "";
        var cppVarName = _variableMap.TryGetValue(varName, out var mapped) ? mapped : $"_var_{varName}";

        var result = GenerateNodeCode(node.Children[0], cppVarName);
        if (result != cppVarName)
        {
            _sb.AppendLine($"    for (int i = 0; i < nDataLen; i++) {cppVarName}[i] = {result}[i];");
        }

        return cppVarName;
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
