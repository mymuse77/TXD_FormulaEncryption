using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdxDllGenerator;

/// <summary>
/// 配置加载器
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// 从JSON文件加载配置
    /// </summary>
    public static TdxFormulaConfig? LoadFromJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<TdxFormulaConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    public static TdxFormulaConfig? Load(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        if (extension == ".json")
        {
            return LoadFromJson(filePath);
        }
        throw new NotSupportedException($"不支持的配置文件格式: {extension}。请使用JSON格式。");
    }

    /// <summary>
    /// 创建默认配置文件
    /// </summary>
    public static void CreateDefaultConfig(string filePath)
    {
        var config = new TdxFormulaConfig
        {
            PluginName = "MyTdxPlugin",
            PluginDescription = "我的通达信公式插件",
            Functions = new System.Collections.Generic.List<FunctionConfig>
            {
                new()
                {
                    Id = 1,
                    Name = "MyMA",
                    Description = "自定义N日均线",
                    FormulaType = "MA",
                    Parameters = new System.Collections.Generic.List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 10, Description = "均线周期" }
                    }
                },
                new()
                {
                    Id = 2,
                    Name = "MyEMA",
                    Description = "自定义N日指数均线",
                    FormulaType = "EMA",
                    Parameters = new System.Collections.Generic.List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 12, Description = "EMA周期" }
                    }
                },
                new()
                {
                    Id = 3,
                    Name = "MyRSI",
                    Description = "自定义RSI指标",
                    FormulaType = "RSI",
                    Parameters = new System.Collections.Generic.List<ParameterConfig>
                    {
                        new() { Name = "period", DefaultValue = 14, Description = "RSI周期" }
                    }
                }
            },
            Security = new SecurityConfig
            {
                EnableTimeLimit = false,
                ExpireYear = 2026,
                EnableDeviceBinding = false,
                EnableOnlineVerify = false
            }
        };

        var extension = Path.GetExtension(filePath).ToLower();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(filePath, json);
    }
}
