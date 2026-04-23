using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
    Identifier,     // 标识符 (CLOSE, HIGH等)
    FunctionCall,   // 函数调用
    BinaryOp,       // 二元运算
    UnaryOp,        // 一元运算
    Parameter       // 参数引用
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
            AstNodeType.Identifier => $"Identifier({Value})",
            AstNodeType.FunctionCall => $"FunctionCall({FunctionName})",
            AstNodeType.BinaryOp => $"BinaryOp({Operator})",
            AstNodeType.UnaryOp => $"UnaryOp({Operator})",
            AstNodeType.Parameter => $"Parameter({Value})",
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
/// 通达信风格表达式解析器
/// </summary>
public class ExpressionParser
{
    private string _expression = "";
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

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
    /// 解析表达式
    /// </summary>
    public AstNode Parse(string expression)
    {
        _expression = expression?.Trim() ?? "";
        _position = 0;
        _line = 1;
        _column = 1;

        if (string.IsNullOrWhiteSpace(_expression))
        {
            throw new ExpressionParseException("表达式不能为空", 1, 1);
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
            var parser = new ExpressionParser();
            parser.Parse(expression);
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

        // 数据序列或标识符
        if (Functions.TryGetValue(name, out var dataFunc) && dataFunc.IsDataSeries)
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
