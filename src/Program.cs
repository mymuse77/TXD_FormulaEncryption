using System;
using System.IO;

namespace TdxDllGenerator;

/// <summary>
/// 程序入口
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  通达信DLL代码生成器");
        Console.WriteLine("  TdxDllGenerator v1.0");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // 如果没有参数，显示帮助
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "generate":
                case "gen":
                case "g":
                    return HandleGenerate(args);

                case "init":
                case "i":
                    return HandleInit(args);

                case "validate":
                case "val":
                case "v":
                    return HandleValidate(args);

                case "help":
                case "-h":
                case "--help":
                case "/?":
                    ShowHelp();
                    return 0;

                default:
                    Console.WriteLine($"未知命令: {command}");
                    Console.WriteLine();
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部错误: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    static void ShowHelp()
    {
        Console.WriteLine("用法:");
        Console.WriteLine("  TdxDllGenerator <command> [options]");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  generate, g    从配置文件生成DLL项目");
        Console.WriteLine("  init, i        创建默认配置文件");
        Console.WriteLine("  validate, v    验证配置文件");
        Console.WriteLine("  help           显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  -c, --config <path>    配置文件路径");
        Console.WriteLine("  -o, --output <path>    输出目录/文件路径");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  # 创建默认配置");
        Console.WriteLine("  TdxDllGenerator init -o myconfig.json");
        Console.WriteLine();
        Console.WriteLine("  # 生成DLL项目");
        Console.WriteLine("  TdxDllGenerator generate -c myconfig.json -o ./MyPlugin");
        Console.WriteLine();
        Console.WriteLine("  # 验证配置");
        Console.WriteLine("  TdxDllGenerator validate -c myconfig.json");
        Console.WriteLine();
    }

    /// <summary>
    /// 处理generate命令
    /// </summary>
    static int HandleGenerate(string[] args)
    {
        string? configPath = null;
        string outputPath = "./output";

        // 解析参数
        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "-c" || args[i] == "--config") && i + 1 < args.Length)
            {
                configPath = args[++i];
            }
            else if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
        }

        if (string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine("错误: 缺少配置文件路径");
            Console.WriteLine("用法: TdxDllGenerator generate -c <config.json> -o <output_dir>");
            return 1;
        }

        // 检查配置文件是否存在
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"错误: 配置文件不存在: {configPath}");
            Console.WriteLine("提示: 使用 'init' 命令创建默认配置");
            return 1;
        }

        // 加载配置
        Console.WriteLine($"正在加载配置: {configPath}");
        var config = ConfigLoader.Load(configPath);
        if (config == null)
        {
            Console.WriteLine("错误: 配置文件格式无效");
            return 1;
        }

        // 验证配置
        if (!ValidateConfiguration(config, true))
        {
            Console.WriteLine();
            Console.WriteLine("配置验证失败，请修复上述问题后再生成");
            return 1;
        }

        // 确保输出目录存在
        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(fullOutputPath);

        Console.WriteLine();
        Console.WriteLine("开始生成项目...");
        Console.WriteLine($"输出目录: {fullOutputPath}");
        Console.WriteLine();

        // 生成代码
        var generator = new CppCodeGenerator(config, fullOutputPath);
        generator.GenerateProject();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  生成完成!");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("下一步:");
        Console.WriteLine($"1. 打开项目: {fullOutputPath}");
        Console.WriteLine($"2. 用Visual Studio打开 {config.PluginName}.vcxproj");
        Console.WriteLine("3. 选择Release/Win32配置并编译");
        Console.WriteLine("4. 将生成的DLL复制到通达信 T0002/dlls/ 目录");
        Console.WriteLine();

        return 0;
    }

    /// <summary>
    /// 处理init命令
    /// </summary>
    static int HandleInit(string[] args)
    {
        string outputPath = "./config.json";

        // 解析参数
        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
        }

        ConfigLoader.CreateDefaultConfig(outputPath);
        Console.WriteLine($"已创建默认配置文件: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("您可以编辑此文件，然后运行:");
        Console.WriteLine($"  TdxDllGenerator generate -c {outputPath} -o ./output");

        return 0;
    }

    /// <summary>
    /// 处理validate命令
    /// </summary>
    static int HandleValidate(string[] args)
    {
        string? configPath = null;

        // 解析参数
        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "-c" || args[i] == "--config") && i + 1 < args.Length)
            {
                configPath = args[++i];
            }
        }

        if (string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine("错误: 缺少配置文件路径");
            Console.WriteLine("用法: TdxDllGenerator validate -c <config.json>");
            return 1;
        }

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"错误: 配置文件不存在: {configPath}");
            return 1;
        }

        var config = ConfigLoader.Load(configPath);
        if (config == null)
        {
            Console.WriteLine("错误: 配置文件格式无效");
            return 1;
        }

        if (ValidateConfiguration(config, true))
        {
            Console.WriteLine();
            Console.WriteLine("✓ 配置验证通过，可以生成项目");
            return 0;
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("✗ 配置验证失败，请修复上述问题");
            return 1;
        }
    }

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    static bool ValidateConfiguration(TdxFormulaConfig config, bool verbose = false)
    {
        var isValid = true;

        if (verbose) Console.WriteLine("正在验证配置...");

        // 验证插件名称
        if (string.IsNullOrWhiteSpace(config.PluginName))
        {
            Console.WriteLine("警告: PluginName为空，将使用默认值 'MyPlugin'");
            config.PluginName = "MyPlugin";
            isValid = false;
        }

        // 检查插件名称合法性
        foreach (var c in config.PluginName)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                Console.WriteLine($"错误: PluginName '{config.PluginName}' 包含非法字符");
                return false;
            }
        }

        // 验证函数
        if (config.Functions == null || config.Functions.Count == 0)
        {
            Console.WriteLine("警告: 没有定义任何函数");
            isValid = false;
        }
        else
        {
            var usedIds = new System.Collections.Generic.HashSet<int>();
            var usedNames = new System.Collections.Generic.HashSet<string>();

            foreach (var func in config.Functions)
            {
                if (func.Id <= 0)
                {
                    Console.WriteLine($"错误: 函数 '{func.Name}' 的ID必须大于0");
                    isValid = false;
                }

                if (usedIds.Contains(func.Id))
                {
                    Console.WriteLine($"错误: 函数ID {func.Id} 重复");
                    isValid = false;
                }
                usedIds.Add(func.Id);

                if (string.IsNullOrWhiteSpace(func.Name))
                {
                    Console.WriteLine($"错误: 函数ID {func.Id} 的名称为空");
                    isValid = false;
                }
                else if (usedNames.Contains(func.Name))
                {
                    Console.WriteLine($"错误: 函数名称 '{func.Name}' 重复");
                    isValid = false;
                }
                else
                {
                    usedNames.Add(func.Name);
                }

                var validTypes = new[] { "MA", "EMA", "RSI", "MACD", "CUSTOM", "EXPRESSION", "DATALOADER", "EXPORTREADER" };
                if (!validTypes.Contains(func.FormulaType?.ToUpper()))
                {
                    Console.WriteLine($"错误: 函数 '{func.Name}' 的公式类型无效: {func.FormulaType}");
                    isValid = false;
                }

                // 验证 EXPRESSION 类型必须有表达式
                if (func.FormulaType?.ToUpper() == "EXPRESSION" && string.IsNullOrWhiteSpace(func.Expression))
                {
                    Console.WriteLine($"错误: 函数 '{func.Name}' 使用 EXPRESSION 类型但 expression 为空");
                    isValid = false;
                }

                // 验证 EXPRESSION 表达式语法
                if (func.FormulaType?.ToUpper() == "EXPRESSION" && !string.IsNullOrWhiteSpace(func.Expression))
                {
                    if (!ExpressionParser.Validate(func.Expression, out var error))
                    {
                        Console.WriteLine($"错误: 函数 '{func.Name}' 的表达式语法错误: {error}");
                        isValid = false;
                    }
                }
            }
        }

        if (verbose && isValid)
        {
            Console.WriteLine($"✓ 插件名称: {config.PluginName}");
            Console.WriteLine($"✓ 函数数量: {config.Functions?.Count ?? 0}");
        }

        return isValid;
    }
}
