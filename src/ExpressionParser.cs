using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TdxDllGenerator;

/// <summary>
/// 表达式解析异常
/// </summary>
public class ExpressionParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ExpressionParseException(string message, int line, int column)
        : base($"[行 {line}, 列 {column}] {message}")
    {
        Line = line;
        Column = column;
    }

    public ExpressionParseException(string message)
        : base(message)
    {
        Line = 0;
        Column = 0;
    }
}

/// <summary>
/// 表达式 AST 节点类型
/// </summary>
public enum AstNodeType
{
    Number,         // 数字常量
    StringLiteral,  // 字符串常量
    Identifier,     // 标识符 (CLOSE, HIGH, 用户变量等)
    FunctionCall,   // 函数调用
    BinaryOp,       // 二元运算
    UnaryOp,        // 一元运算
    Parameter,      // 参数引用
    StatementList,  // 语句列表 (分号分隔)
    Assignment,     // 赋值语句 (VAR:=expr)
}

/// <summary>
/// AST 节点
/// </summary>
public class AstNode
{
    public AstNodeType Type { get; set; }
    public string? Value { get; set; }
    public string? Operator { get; set; }
    public string? FunctionName { get; set; }
    public List<AstNode> Children { get; set; } = new();
    public int Line { get; set; }
    public int Column { get; set; }

    public AstNode(AstNodeType type)
    {
        Type = type;
    }

    public override string ToString()
    {
        return Type switch
        {
            AstNodeType.Number => $"Number({Value})",
            AstNodeType.StringLiteral => $"String({Value})",
            AstNodeType.Identifier => $"Identifier({Value})",
            AstNodeType.FunctionCall => $"FunctionCall({FunctionName})",
            AstNodeType.BinaryOp => $"BinaryOp({Operator})",
            AstNodeType.UnaryOp => $"UnaryOp({Operator})",
            AstNodeType.Parameter => $"Parameter({Value})",
            AstNodeType.StatementList => $"StatementList({Children.Count} stmts)",
            AstNodeType.Assignment => $"Assignment({Value}:=)",
            _ => $"{Type}"
        };
    }
}

/// <summary>
/// 支持的函数定义
/// </summary>
public class ExpressionFunction
{
    public string Name { get; set; } = "";
    public int MinArgs { get; set; }
    public int MaxArgs { get; set; }
    public string Description { get; set; } = "";
    public bool IsDataSeries { get; set; }

    public ExpressionFunction(string name, int minArgs, int maxArgs, string description, bool isDataSeries = false)
    {
        Name = name;
        MinArgs = minArgs;
        MaxArgs = maxArgs;
        Description = description;
        IsDataSeries = isDataSeries;
    }
}

/// <summary>
/// 通达信风格表达式解析器 - 支持多行赋值语句
/// </summary>
public class ExpressionParser
{
    private string _expression = "";
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

    // 用户定义的变量集合（在多语句解析中累积）
    private readonly HashSet<string> _userVariables = new(StringComparer.OrdinalIgnoreCase);

    // 需要过滤的绘图函数
    private static readonly HashSet<string> DrawFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "DRAWICON", "DRAWKLINE", "STICKLINE", "DRAWSL", "DRAWTEXT_FIX",
        "DRAWLINE", "DRAWNUMBER", "DRAWTEXT", "DRAWRECTREL"
    };

    // 需要过滤的颜色常量前缀
    private static readonly Regex ColorRegex = new(@"\bCOLOR\w+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 需要过滤的绘图修饰符
    private static readonly Regex NodrawModifierRegex = new(@",?\s*\bNODRAW\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinethickRegex = new(@"\bLINETHICK\d*\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PointdotRegex = new(@"\bPOINTDOT\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CircledotRegex = new(@"\bCIRCLEDOT\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CrossdotRegex = new(@"\bCROSSDOT\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StickRegex = new(@"\bSTICK\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VolstickRegex = new(@"\bVOLSTICK\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ColorstickRegex = new(@"\bCOLORSTICK\b\s*,?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 支持的函数表
    public static readonly Dictionary<string, ExpressionFunction> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        // 数据序列
        ["CLOSE"] = new("CLOSE", 0, 0, "收盘价", true),
        ["C"] = new("C", 0, 0, "收盘价", true),
        ["OPEN"] = new("OPEN", 0, 0, "开盘价", true),
        ["O"] = new("O", 0, 0, "开盘价", true),
        ["HIGH"] = new("HIGH", 0, 0, "最高价", true),
        ["H"] = new("H", 0, 0, "最高价", true),
        ["LOW"] = new("LOW", 0, 0, "最低价", true),
        ["L"] = new("L", 0, 0, "最低价", true),
        ["VOL"] = new("VOL", 0, 0, "成交量", true),
        ["V"] = new("V", 0, 0, "成交量", true),
        ["AMOUNT"] = new("AMOUNT", 0, 0, "成交额", true),
        ["AMO"] = new("AMO", 0, 0, "成交额", true),

        // 数学运算函数
        ["ABS"] = new("ABS", 1, 1, "绝对值"),
        ["MAX"] = new("MAX", 2, 2, "最大值"),
        ["MIN"] = new("MIN", 2, 2, "最小值"),
        ["MOD"] = new("MOD", 2, 2, "取模"),
        ["ROUND"] = new("ROUND", 1, 2, "四舍五入"),
        ["INTPART"] = new("INTPART", 1, 1, "取整"),
        ["FLOOR"] = new("FLOOR", 1, 1, "向下取整"),
        ["SQRT"] = new("SQRT", 1, 1, "平方根"),
        ["POW"] = new("POW", 2, 2, "幂运算"),
        ["EXP"] = new("EXP", 1, 1, "指数函数"),
        ["LN"] = new("LN", 1, 1, "自然对数"),
        ["LOG"] = new("LOG", 1, 1, "常用对数"),
        ["SIN"] = new("SIN", 1, 1, "正弦"),
        ["COS"] = new("COS", 1, 1, "余弦"),
        ["TAN"] = new("TAN", 1, 1, "正切"),

        // 技术指标函数
        ["MA"] = new("MA", 2, 2, "简单移动平均"),
        ["EMA"] = new("EMA", 2, 2, "指数移动平均"),
        ["SMA"] = new("SMA", 3, 3, "加权移动平均"),
        ["WMA"] = new("WMA", 2, 2, "加权移动平均"),
        ["STD"] = new("STD", 2, 2, "标准差"),
        ["HHV"] = new("HHV", 2, 2, "最高值"),
        ["LLV"] = new("LLV", 2, 2, "最低值"),
        ["SUM"] = new("SUM", 2, 2, "求和"),
        ["REF"] = new("REF", 2, 2, "引用"),

        // 逻辑函数
        ["IF"] = new("IF", 3, 3, "条件判断"),
        ["AND"] = new("AND", 2, int.MaxValue, "逻辑与"),
        ["OR"] = new("OR", 2, int.MaxValue, "逻辑或"),
        ["NOT"] = new("NOT", 1, 1, "逻辑非"),
        ["CROSS"] = new("CROSS", 2, 2, "上穿"),

        // 统计函数
        ["COUNT"] = new("COUNT", 2, 2, "统计满足条件周期数"),
        ["EXIST"] = new("EXIST", 2, 2, "存在"),
        ["EVERY"] = new("EVERY", 2, 2, "一直存在"),
        ["BARSLAST"] = new("BARSLAST", 1, 1, "上次条件成立位置"),
        ["BARSLASTCOUNT"] = new("BARSLASTCOUNT", 1, 1, "连续满足条件的周期数"),

        // 环境/系统函数
        ["CODELIKE"] = new("CODELIKE", 1, 1, "检测股票代码前缀"),
        ["ZTPRICE"] = new("ZTPRICE", 2, 2, "涨停价"),
        ["DTPRICE"] = new("DTPRICE", 2, 2, "跌停价"),
        ["NODRAW"] = new("NODRAW", 1, 1, "绘图修饰符（返回原值）"),
    };

    // 运算符优先级
    private static readonly Dictionary<string, int> OperatorPrecedence = new()
    {
        ["OR"] = 1,
        ["||"] = 1,
        ["AND"] = 2,
        ["&&"] = 2,
        ["="] = 3,
        ["=="] = 3,
        ["<>"] = 3,
        ["!="] = 3,
        [">"] = 4,
        ["<"] = 4,
        [">="] = 4,
        ["<="] = 4,
        ["+"] = 5,
        ["-"] = 5,
        ["*"] = 6,
        ["/"] = 6,
        ["%"] = 6,
    };

    /// <summary>
    /// 预处理通达信公式 - 移除注释、绘图函数、颜色常量、绘图修饰符
    /// </summary>
    public static string Preprocess(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return "";

        var result = formula;

        // 1. 移除 {注释}
        var sb = new StringBuilder();
        int depth = 0;
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == '{') { depth++; continue; }
            if (result[i] == '}') { depth = Math.Max(0, depth - 1); continue; }
            if (depth == 0) sb.Append(result[i]);
        }
        result = sb.ToString();

        // 2. 移除绘图函数调用（从函数名到匹配的右括号）
        foreach (var funcName in DrawFunctions)
        {
            result = RemoveFunctionCall(result, funcName);
        }

        // 3. 移除颜色常量
        result = ColorRegex.Replace(result, "");

        // 4. 移除绘图修饰符（NODRAW, LINETHICK, POINTDOT, CIRCLEDOT, CROSSDOT, STICK, VOLSTICK, COLORSTICK）
        result = NodrawModifierRegex.Replace(result, "");
        result = LinethickRegex.Replace(result, "");
        result = PointdotRegex.Replace(result, "");
        result = CircledotRegex.Replace(result, "");
        result = CrossdotRegex.Replace(result, "");
        result = StickRegex.Replace(result, "");
        result = VolstickRegex.Replace(result, "");
        result = ColorstickRegex.Replace(result, "");

        // 5. 移除逗号残留（颜色/修饰符被移除后可能残留）- 循环清理直到无变化
        string prev;
        do
        {
            prev = result;
            result = Regex.Replace(result, @",\s*;", ";");
            result = Regex.Replace(result, @",\s*$", "");
        } while (result != prev);

        // 6. 清理多余空白和连续分号
        result = Regex.Replace(result, @"\s+", " ");
        result = Regex.Replace(result, @";\s*;+", ";");  // 连续分号合并为一个

        return result.Trim();
    }

    /// <summary>
    /// 移除指定函数的完整调用（含括号匹配）
    /// </summary>
    private static string RemoveFunctionCall(string text, string funcName)
    {
        while (true)
        {
            var upperText = text.ToUpperInvariant();
            int idx = -1;

            for (int i = 0; i <= upperText.Length - funcName.Length; i++)
            {
                if (upperText.Substring(i, funcName.Length) == funcName)
                {
                    bool wordStart = (i == 0 || !char.IsLetterOrDigit(text[i - 1]) && text[i - 1] != '_');
                    bool wordEnd = (i + funcName.Length >= text.Length || !char.IsLetterOrDigit(text[i + funcName.Length]) && text[i + funcName.Length] != '_');
                    if (wordStart && wordEnd)
                    {
                        idx = i;
                        break;
                    }
                }
            }

            if (idx < 0) break;

            int parenStart = idx + funcName.Length;
            while (parenStart < text.Length && char.IsWhiteSpace(text[parenStart])) parenStart++;

            if (parenStart >= text.Length || text[parenStart] != '(')
            {
                // 不是函数调用格式，跳过此位置
                // 在前面插入一个不可见字符防止重复匹配，然后移除
                text = text.Substring(0, idx) + " " + text.Substring(idx + funcName.Length);
                continue;
            }

            // 找到匹配的右括号
            int pdepth = 1;
            int parenEnd = parenStart + 1;
            bool inString = false;
            while (parenEnd < text.Length && pdepth > 0)
            {
                if (text[parenEnd] == '\'' && !inString) inString = true;
                else if (text[parenEnd] == '\'' && inString) inString = false;
                else if (!inString)
                {
                    if (text[parenEnd] == '(') pdepth++;
                    else if (text[parenEnd] == ')') pdepth--;
                }
                parenEnd++;
            }

            text = text.Substring(0, idx) + text.Substring(parenEnd);
        }
        return text;
    }

    /// <summary>
    /// 解析表达式（支持多行赋值）
    /// </summary>
    public AstNode Parse(string expression)
    {
        _expression = expression?.Trim() ?? "";
        _position = 0;
        _line = 1;
        _column = 1;
        _userVariables.Clear();

        if (string.IsNullOrWhiteSpace(_expression))
        {
            throw new ExpressionParseException("表达式不能为空", 1, 1);
        }

        // 检查是否包含分号或 :=（多语句模式）
        bool hasSemicolon = _expression.Contains(';');
        bool hasAssignment = _expression.Contains(":=");

        if (hasSemicolon || hasAssignment)
        {
            return ParseStatementList();
        }

        var result = ParseExpression();
        SkipWhitespace();

        if (_position < _expression.Length)
        {
            throw new ExpressionParseException($"意外的字符: '{Peek()}'", _line, _column);
        }

        return result;
    }

    /// <summary>
    /// 验证表达式是否有效
    /// </summary>
    public static bool Validate(string expression, out string? error)
    {
        try
        {
            var preprocessed = Preprocess(expression);
            var parser = new ExpressionParser();
            parser.Parse(preprocessed);
            error = null;
            return true;
        }
        catch (ExpressionParseException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = $"验证失败: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 解析语句列表 (分号分隔，支持 := 赋值)
    /// </summary>
    private AstNode ParseStatementList()
    {
        var listNode = new AstNode(AstNodeType.StatementList);

        while (_position < _expression.Length)
        {
            SkipWhitespace();
            if (_position >= _expression.Length) break;

            // 尝试解析赋值语句 VAR:=expr 或 VAR:expr
            var stmt = ParseStatement();
            if (stmt != null)
                listNode.Children.Add(stmt);

            // 跳过分号
            SkipWhitespace();
            if (Match(';'))
            {
                Consume(";");
                _column++;
            }
        }

        // 如果只有一个语句且不是赋值，直接返回（保持向后兼容）
        if (listNode.Children.Count == 1 && listNode.Children[0].Type != AstNodeType.Assignment)
            return listNode.Children[0];

        return listNode;
    }

    /// <summary>
    /// 解析单条语句 (赋值或表达式)
    /// </summary>
    private AstNode? ParseStatement()
    {
        SkipWhitespace();
        if (_position >= _expression.Length) return null;

        // 检查是否是绘图函数（跳过）
        if (IsDrawFunctionAtCurrentPosition())
        {
            SkipDrawFunctionCall();
            return null;
        }

        // 尝试匹配 VAR:=expr 模式
        var savedPos = _position;
        var savedCol = _column;

        // 读取标识符
        if (char.IsLetter(Peek()) || Peek() == '_')
        {
            var sb = new StringBuilder();
            int startPos = _position;
            while (_position < _expression.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                sb.Append(Peek());
                _position++;
                _column++;
            }

            var name = sb.ToString().ToUpperInvariant();
            SkipWhitespace();

            // 检查 := 赋值
            if (Match(':') && _position + 1 < _expression.Length && _expression[_position + 1] == '=')
            {
                Consume(":=");
                _column += 2;

                // 注册变量
                _userVariables.Add(name);

                // 解析赋值表达式
                var expr = ParseExpression();

                var assignNode = new AstNode(AstNodeType.Assignment)
                {
                    Value = name,
                    Line = _line,
                    Column = savedCol
                };
                assignNode.Children.Add(expr);
                return assignNode;
            }

            // 检查 : 输出赋值 (VAR:expr)
            if (Match(':') && (_position + 1 >= _expression.Length || _expression[_position + 1] != '='))
            {
                Consume(":");
                _column++;

                // 注册变量
                _userVariables.Add(name);

                // 解析赋值表达式
                var expr = ParseExpression();

                var assignNode = new AstNode(AstNodeType.Assignment)
                {
                    Value = name,
                    Line = _line,
                    Column = savedCol
                };
                assignNode.Children.Add(expr);
                return assignNode;
            }

            // 不是赋值，回退
            _position = savedPos;
            _column = savedCol;
        }

        // 普通表达式
        return ParseExpression();
    }

    /// <summary>
    /// 检查当前位置是否是绘图函数
    /// </summary>
    private bool IsDrawFunctionAtCurrentPosition()
    {
        foreach (var funcName in DrawFunctions)
        {
            if (PeekKeyword(funcName))
            {
                int afterName = _position + funcName.Length;
                while (afterName < _expression.Length && char.IsWhiteSpace(_expression[afterName])) afterName++;
                if (afterName < _expression.Length && _expression[afterName] == '(')
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 跳过绘图函数调用（含括号匹配）
    /// </summary>
    private void SkipDrawFunctionCall()
    {
        // 跳过函数名
        while (_position < _expression.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            _position++;
            _column++;
        }

        SkipWhitespace();

        // 跳过括号内容
        if (Match('('))
        {
            Consume("(");
            _column++;
            int depth = 1;
            bool inStr = false;
            while (_position < _expression.Length && depth > 0)
            {
                if (Peek() == '\'' && !inStr) inStr = true;
                else if (Peek() == '\'' && inStr) inStr = false;
                else if (!inStr)
                {
                    if (Peek() == '(') depth++;
                    else if (Peek() == ')') depth--;
                }
                _position++;
                _column++;
            }
        }
    }

    /// <summary>
    /// 解析表达式 (支持二元运算)
    /// </summary>
    private AstNode ParseExpression(int minPrecedence = 0)
    {
        var left = ParseUnary();

        while (true)
        {
            SkipWhitespace();

            if (_position >= _expression.Length)
                break;

            // 分号表示语句结束
            if (Peek() == ';')
                break;

            var op = PeekOperator();
            if (op == null || !OperatorPrecedence.TryGetValue(op, out var precedence))
                break;

            if (precedence < minPrecedence)
                break;

            Consume(op);
            _column += op.Length;

            var right = ParseExpression(precedence + 1);

            var binaryOp = new AstNode(AstNodeType.BinaryOp)
            {
                Operator = op,
                Line = left.Line,
                Column = left.Column
            };
            binaryOp.Children.Add(left);
            binaryOp.Children.Add(right);

            left = binaryOp;
        }

        return left;
    }

    /// <summary>
    /// 解析一元运算
    /// </summary>
    private AstNode ParseUnary()
    {
        SkipWhitespace();

        if (_position >= _expression.Length)
        {
            throw new ExpressionParseException("意外的表达式结束", _line, _column);
        }

        var startLine = _line;
        var startColumn = _column;

        // 正负号
        if (Match('+') || Match('-'))
        {
            var op = Peek().ToString();
            Consume(op);
            _column++;

            var operand = ParseUnary();
            var unaryOp = new AstNode(AstNodeType.UnaryOp)
            {
                Operator = op,
                Line = startLine,
                Column = startColumn
            };
            unaryOp.Children.Add(operand);
            return unaryOp;
        }

        // NOT 运算符
        if (PeekKeyword("NOT"))
        {
            Consume("NOT");
            _column += 3;

            var operand = ParseUnary();
            var unaryOp = new AstNode(AstNodeType.UnaryOp)
            {
                Operator = "NOT",
                Line = startLine,
                Column = startColumn
            };
            unaryOp.Children.Add(operand);
            return unaryOp;
        }

        return ParsePrimary();
    }

    /// <summary>
    /// 解析基本元素
    /// </summary>
    private AstNode ParsePrimary()
    {
        SkipWhitespace();

        if (_position >= _expression.Length)
        {
            throw new ExpressionParseException("意外的表达式结束", _line, _column);
        }

        var startLine = _line;
        var startColumn = _column;

        // 括号表达式
        if (Match('('))
        {
            Consume("(");
            _column++;
            var node = ParseExpression();
            SkipWhitespace();
            if (!Match(')'))
            {
                throw new ExpressionParseException("缺少右括号 ')'", _line, _column);
            }
            Consume(")");
            _column++;
            return node;
        }

        // 数字
        if (char.IsDigit(Peek()) || (Peek() == '.' && char.IsDigit(PeekNext())))
        {
            return ParseNumber(startLine, startColumn);
        }

        // 字符串字面量 (如 '00', '30', '60')
        if (Peek() == '\'')
        {
            return ParseStringLiteral(startLine, startColumn);
        }

        // 标识符或函数调用
        if (char.IsLetter(Peek()) || Peek() == '_')
        {
            return ParseIdentifierOrFunction(startLine, startColumn);
        }

        throw new ExpressionParseException($"意外的字符: '{Peek()}'", _line, _column);
    }

    /// <summary>
    /// 解析数字
    /// </summary>
    private AstNode ParseNumber(int line, int column)
    {
        var sb = new StringBuilder();
        var hasDot = false;

        while (_position < _expression.Length &&
               (char.IsDigit(Peek()) || (Peek() == '.' && !hasDot)))
        {
            if (Peek() == '.')
                hasDot = true;
            sb.Append(Peek());
            _position++;
            _column++;
        }

        return new AstNode(AstNodeType.Number)
        {
            Value = sb.ToString(),
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// 解析字符串字面量 (如 '00', '30', '60')
    /// </summary>
    private AstNode ParseStringLiteral(int line, int column)
    {
        Consume("'"); // consume opening quote
        _column++;

        var sb = new StringBuilder();
        while (_position < _expression.Length && Peek() != '\'')
        {
            sb.Append(Peek());
            _position++;
            _column++;
        }

        if (!Match('\''))
        {
            throw new ExpressionParseException("字符串未以单引号关闭", _line, _column);
        }
        Consume("'"); // consume closing quote
        _column++;

        return new AstNode(AstNodeType.StringLiteral)
        {
            Value = sb.ToString(),
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// 解析标识符或函数调用
    /// </summary>
    private AstNode ParseIdentifierOrFunction(int line, int column)
    {
        var sb = new StringBuilder();

        while (_position < _expression.Length &&
               (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            sb.Append(Peek());
            _position++;
            _column++;
        }

        var name = sb.ToString().ToUpperInvariant();

        SkipWhitespace();

        // 函数调用
        if (Match('('))
        {
            if (!Functions.TryGetValue(name, out var func))
            {
                throw new ExpressionParseException($"未知的函数: {name}", line, column);
            }

            Consume("(");
            _column++;

            var funcNode = new AstNode(AstNodeType.FunctionCall)
            {
                FunctionName = name,
                Line = line,
                Column = column
            };

            // 解析参数列表
            SkipWhitespace();
            if (!Match(')'))
            {
                do
                {
                    SkipWhitespace();
                    var arg = ParseExpression();
                    funcNode.Children.Add(arg);
                    SkipWhitespace();
                } while (Match(',') && Consume(",") != null && _column++ > 0);
            }

            SkipWhitespace();
            if (!Match(')'))
            {
                throw new ExpressionParseException("缺少右括号 ')'", _line, _column);
            }
            Consume(")");
            _column++;

            // 验证参数数量
            if (funcNode.Children.Count < func.MinArgs)
            {
                throw new ExpressionParseException(
                    $"函数 {name} 至少需要 {func.MinArgs} 个参数，但提供了 {funcNode.Children.Count} 个",
                    line, column);
            }
            if (funcNode.Children.Count > func.MaxArgs)
            {
                throw new ExpressionParseException(
                    $"函数 {name} 最多接受 {func.MaxArgs} 个参数，但提供了 {funcNode.Children.Count} 个",
                    line, column);
            }

            return funcNode;
        }

        // 数据序列
        if (Functions.TryGetValue(name, out var dataFunc) && dataFunc.IsDataSeries)
        {
            return new AstNode(AstNodeType.Identifier)
            {
                Value = name,
                Line = line,
                Column = column
            };
        }

        // 用户定义的变量
        if (_userVariables.Contains(name))
        {
            return new AstNode(AstNodeType.Identifier)
            {
                Value = name,
                Line = line,
                Column = column
            };
        }

        throw new ExpressionParseException($"未知的标识符: {name}", line, column);
    }

    #region Helper Methods

    private char Peek()
    {
        if (_position >= _expression.Length)
            return '\0';
        return _expression[_position];
    }

    private char PeekNext()
    {
        if (_position + 1 >= _expression.Length)
            return '\0';
        return _expression[_position + 1];
    }

    private bool Match(char c)
    {
        if (_position >= _expression.Length)
            return false;
        return _expression[_position] == c;
    }

    private string? Consume(string s)
    {
        if (_position + s.Length > _expression.Length)
            return null;
        if (_expression.Substring(_position, s.Length) == s)
        {
            _position += s.Length;
            return s;
        }
        return null;
    }

    private void SkipWhitespace()
    {
        while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
        {
            if (_expression[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private string? PeekOperator()
    {
        if (_position >= _expression.Length)
            return null;

        // 检查多字符运算符
        var twoCharOps = new[] { "==", "!=", "<=", ">=", "||", "&&", "<>" };
        if (_position + 1 < _expression.Length)
        {
            var twoChar = _expression.Substring(_position, 2);
            if (twoCharOps.Contains(twoChar))
                return twoChar;
        }

        // 单字符运算符
        var singleCharOps = new[] { '+', '-', '*', '/', '%', '>', '<', '=' };
        var ch = _expression[_position];
        if (singleCharOps.Contains(ch))
            return ch.ToString();

        // 检查关键字运算符
        var keywords = new[] { "AND", "OR" };
        foreach (var kw in keywords)
        {
            if (PeekKeyword(kw))
                return kw;
        }

        return null;
    }

    private bool PeekKeyword(string keyword)
    {
        if (_position + keyword.Length > _expression.Length)
            return false;

        var substr = _expression.Substring(_position, keyword.Length);
        if (!string.Equals(substr, keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        // 确保关键字后面不是字母或数字（避免匹配前缀）
        if (_position + keyword.Length < _expression.Length)
        {
            var nextChar = _expression[_position + keyword.Length];
            if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                return false;
        }

        return true;
    }

    #endregion
}
