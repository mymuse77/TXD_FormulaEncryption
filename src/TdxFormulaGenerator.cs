using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TdxDllGenerator;

/// <summary>
/// 通达信最终公式生成器
/// 根据原始公式文件和DLL配置，生成可直接复制到通达信的完整公式
/// </summary>
public class TdxFormulaGenerator
{
    private readonly TdxFormulaConfig _config;
    private readonly string _outputPath;

    public TdxFormulaGenerator(TdxFormulaConfig config, string outputPath)
    {
        _config = config;
        _outputPath = outputPath;
    }

    /// <summary>
    /// 生成所有配置了 originalFormulaFile 的最终公式文件
    /// </summary>
    public void GenerateAllFormulas()
    {
        foreach (var func in _config.Functions)
        {
            if (!string.IsNullOrWhiteSpace(func.OriginalFormulaFile))
            {
                try
                {
                    var formula = GenerateFormula(func);
                    if (!string.IsNullOrWhiteSpace(formula))
                    {
                        var fileName = $"{func.Name}.txt";
                        var filePath = Path.Combine(_outputPath, fileName);
                        File.WriteAllText(filePath, formula, System.Text.Encoding.UTF8);
                        Console.WriteLine($"  已生成公式文件: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  警告: 生成公式 '{func.Name}' 失败: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 为单个函数生成最终通达信公式
    /// </summary>
    private string GenerateFormula(FunctionConfig targetFunc)
    {
        // 确定DLL槽位号（从配置推断，默认1）
        int dllSlot = DetectDllSlot(targetFunc);
        string dllPrefix = $"TDXDLL{dllSlot}";

        // 读取原始公式
        string originalFormula = ReadOriginalFormula(targetFunc.OriginalFormulaFile!);
        if (string.IsNullOrWhiteSpace(originalFormula))
        {
            throw new Exception($"原始公式文件为空或不存在: {targetFunc.OriginalFormulaFile}");
        }

        // 解析原始公式行
        var lines = originalFormula
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("{"))
            .ToList();

        // 提取绘图指令
        var drawingCommands = ExtractDrawingCommands(lines);

        // 确定输出变量名
        string outputVar = !string.IsNullOrWhiteSpace(targetFunc.OutputVariableName)
            ? targetFunc.OutputVariableName
            : targetFunc.Name;

        // 查找导出读取函数
        var exportReaders = _config.Functions
            .Where(f => f.FormulaType?.ToUpper() == "EXPORTREADER")
            .ToList();

        // 收集可用的导出变量
        var availableExports = exportReaders
            .Select(f => f.Expression?.Trim()?.ToUpperInvariant() ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();

        var sb = new StringBuilder();
        sb.AppendLine($"{{=== {targetFunc.Name} - DLL保护版 ===}}");
        sb.AppendLine();

        // 1. 生成数据加载调用（DATALOADER / CUSTOM数据加载 类型函数）
        var dataLoaders = FindDataLoaders(targetFunc);
        if (dataLoaders.Any())
        {
            sb.AppendLine("{ DLL数据加载 }");
            foreach (var loader in dataLoaders)
            {
                var call = BuildDllCall(loader, dllPrefix);
                // 使用 := 定义中间变量，避免输出到图表且确保调用不被优化
                sb.AppendLine($"_L{loader.Id}:= {call};");
            }
            sb.AppendLine();
        }

        // 2. 生成核心信号调用
        sb.AppendLine("{ 核心信号 - DLL保护 }");
        var targetCall = BuildDllCall(targetFunc, dllPrefix);
        sb.AppendLine($"{outputVar}: {targetCall}, NODRAW;");
        sb.AppendLine();

        // 3. 生成导出变量读取（用于 DRAWSL 等）
        if (exportReaders.Any())
        {
            sb.AppendLine("{ DLL导出变量读取（用于DRAWSL等绘图指令）}");
            foreach (var reader in exportReaders)
            {
                var varName = reader.Expression?.Trim() ?? reader.Name;
                sb.AppendLine($"{varName}: {dllPrefix}({reader.Id}, 0, 0, 0), NODRAW;");
            }
            sb.AppendLine();
        }

        // 4. 生成简化的辅助变量（用于绘图着色）
        sb.AppendLine("{ 涨停/跌停/炸板判断（简化版，仅用于着色）}");
        sb.AppendLine("ZT:=C=H AND C>=REF(C,1)*1.1;");
        sb.AppendLine("DT:=C=L AND C<=REF(C,1)*0.9;");
        sb.AppendLine("ZB:=C<H AND H>=REF(C,1)*1.1;");
        sb.AppendLine();

        // 5. 输出绘图指令
        if (drawingCommands.Any())
        {
            sb.AppendLine("{ ========== 绘图指令 ========== }");
            foreach (var cmd in drawingCommands)
            {
                // 过滤掉依赖未导出内部变量的绘图指令
                if (ContainsInternalVar(cmd, availableExports))
                {
                    sb.AppendLine($"{{ 已省略: {cmd} (依赖未导出的DLL内部变量) }}");
                    continue;
                }

                // 将绘图指令中的 XG 等信号变量替换为 DLL 输出变量
                var replaced = ReplaceSignalVariables(cmd, outputVar);
                sb.AppendLine(replaced);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 读取原始公式文件内容
    /// </summary>
    private string ReadOriginalFormula(string filePath)
    {
        // 尝试相对路径（相对于项目根目录）
        var searchPaths = new List<string>();

        if (Path.IsPathRooted(filePath))
        {
            searchPaths.Add(filePath);
        }
        else
        {
            searchPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), filePath));
            searchPaths.Add(Path.Combine(AppContext.BaseDirectory, filePath));
            // 尝试相对于输出目录
            searchPaths.Add(Path.Combine(Path.GetDirectoryName(_outputPath) ?? ".", filePath));
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException($"找不到原始公式文件: {filePath}");
    }

    /// <summary>
    /// 从原始公式行中提取绘图指令
    /// </summary>
    private List<string> ExtractDrawingCommands(List<string> lines)
    {
        var drawingPrefixes = new[]
        {
            "DRAWKLINE",
            "STICKLINE",
            "DRAWICON",
            "DRAWSL",
            "DRAWTEXT",
            "DRAWTEXT_FIX",
            "DRAWNUMBER",
            "DRAWBAND",
            "DRAWAREA",
            "FILLRGN",
            "PARTLINE",
            "VERTLINE",
            "HORLINE"
        };

        var result = new List<string>();
        foreach (var line in lines)
        {
            // 跳过纯变量定义行（包含 := 的行）
            if (line.Contains(":="))
            {
                // 但如果行同时包含绘图指令（如 A:=X;DRAWICON(A,...); 这种合并行）
                // 需要提取绘图部分
                var afterAssign = line;
                int lastSemi = line.LastIndexOf(';');
                if (lastSemi >= 0)
                {
                    afterAssign = line.Substring(lastSemi + 1).Trim();
                }

                if (!string.IsNullOrWhiteSpace(afterAssign) &&
                    drawingPrefixes.Any(p => afterAssign.ToUpper().Contains(p)))
                {
                    result.Add(afterAssign);
                }
                continue;
            }

            // 直接是绘图指令的行
            if (drawingPrefixes.Any(p => line.ToUpper().StartsWith(p)))
            {
                result.Add(line);
            }
        }

        return result;
    }

    /// <summary>
    /// 将绘图指令中的信号变量名替换为DLL输出变量名
    /// </summary>
    private string ReplaceSignalVariables(string command, string outputVar)
    {
        // 只替换 XG（最终信号），不替换 XG1/XG2（子信号由 EXPORTREADER 提供）
        var sb = new StringBuilder();
        int i = 0;
        while (i < command.Length)
        {
            bool replaced = false;
            var candidates = new[] { "XG" };
            foreach (var cand in candidates)
            {
                if (i + cand.Length <= command.Length &&
                    command.Substring(i, cand.Length) == cand)
                {
                    // 确保是完整的变量名（前后不是字母/数字/下划线）
                    bool beforeOk = i == 0 || !IsVarChar(command[i - 1]);
                    bool afterOk = i + cand.Length >= command.Length || !IsVarChar(command[i + cand.Length]);

                    if (beforeOk && afterOk)
                    {
                        sb.Append(outputVar);
                        i += cand.Length;
                        replaced = true;
                        break;
                    }
                }
            }

            if (!replaced)
            {
                sb.Append(command[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private bool IsVarChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// 检查绘图指令是否包含未导出的 DLL 内部变量（如 NN1, NN3, BYN 等）
    /// 如果变量已通过 EXPORTREADER 导出，则认为可用
    /// </summary>
    private bool ContainsInternalVar(string command, HashSet<string> availableExports)
    {
        var internalVars = new[] { "NN1", "NN2", "NN3", "NN4", "NN5", "BYN" };
        foreach (var varName in internalVars)
        {
            if (availableExports.Contains(varName))
                continue; // 该变量已通过 EXPORTREADER 导出，可用

            int idx = 0;
            while ((idx = command.IndexOf(varName, idx)) >= 0)
            {
                bool beforeOk = idx == 0 || !IsVarChar(command[idx - 1]);
                bool afterOk = idx + varName.Length >= command.Length || !IsVarChar(command[idx + varName.Length]);
                if (beforeOk && afterOk)
                    return true;
                idx++;
            }
        }
        return false;
    }

    /// <summary>
    /// 查找当前计算函数依赖的数据加载函数
    /// </summary>
    private List<FunctionConfig> FindDataLoaders(FunctionConfig targetFunc)
    {
        var result = new List<FunctionConfig>();

        // 如果目标函数使用全局数据，则需要找到所有数据加载函数
        // 包括 DATALOADER 类型，以及名称以 LoadData 开头的 CUSTOM 类型
        if (targetFunc.UseGlobalData)
        {
            var loaders = _config.Functions
                .Where(f => f.FormulaType?.ToUpper() == "DATALOADER" ||
                           (f.FormulaType?.ToUpper() == "CUSTOM" && f.Name?.StartsWith("LoadData") == true))
                .OrderBy(f => f.Id)
                .ToList();
            result.AddRange(loaders);
        }

        return result;
    }

    /// <summary>
    /// 构建 DLL 调用表达式
    /// </summary>
    private string BuildDllCall(FunctionConfig func, string dllPrefix)
    {
        // 根据函数类型确定参数
        var param1 = "0";
        var param2 = "0";
        var param3 = "0";

        if (func.FormulaType?.ToUpper() == "DATALOADER" ||
            (func.FormulaType?.ToUpper() == "CUSTOM" && func.Name?.StartsWith("LoadData") == true))
        {
            // 数据加载函数使用实际数据参数
            // 根据名称或描述推断加载的数据类型
            if (func.Name == "LoadData2" || func.Description?.Contains("LOW/VOL") == true)
            {
                param1 = "LOW";
                param2 = "VOL";
                param3 = "0";
            }
            else
            {
                param1 = "CLOSE";
                param2 = "OPEN";
                param3 = "HIGH";
            }
        }
        else if (func.UseGlobalData)
        {
            // 使用全局数据的计算函数不需要实际输入
            param1 = "0";
            param2 = "0";
            param3 = "0";
        }
        else
        {
            // 普通函数根据参数推断
            var paramNames = func.Parameters.Select(p => p.Name?.ToUpper()).ToList();
            if (paramNames.Any(p => p == "CLOSE" || p == "C")) param1 = "CLOSE";
            if (paramNames.Any(p => p == "VOL" || p == "V")) param2 = "VOL";
        }

        return $"{dllPrefix}({func.Id}, {param1}, {param2}, {param3})";
    }

    /// <summary>
    /// 获取 DLL 绑定槽位号
    /// </summary>
    private int DetectDllSlot(FunctionConfig func)
    {
        return _config.DllSlot > 0 ? _config.DllSlot : 1;
    }
}
