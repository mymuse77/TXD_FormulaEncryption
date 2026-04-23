using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdxDllGenerator;

/// <summary>
/// 通达信公式配置
/// </summary>
public class TdxFormulaConfig
{
    [JsonPropertyName("pluginName")]
    public string PluginName { get; set; } = "MyPlugin";

    [JsonPropertyName("pluginDescription")]
    public string PluginDescription { get; set; } = "通达信自定义公式插件";

    [JsonPropertyName("functions")]
    public List<FunctionConfig> Functions { get; set; } = new();

    [JsonPropertyName("security")]
    public SecurityConfig Security { get; set; } = new();
}

/// <summary>
/// 函数配置
/// </summary>
public class FunctionConfig
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("formulaType")]
    public string FormulaType { get; set; } = "MA"; // MA, EMA, RSI, MACD, CUSTOM, EXPRESSION

    [JsonPropertyName("parameters")]
    public List<ParameterConfig> Parameters { get; set; } = new();

    [JsonPropertyName("customCode")]
    public string? CustomCode { get; set; }

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }
}

/// <summary>
/// 参数配置
/// </summary>
public class ParameterConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("defaultValue")]
    public float DefaultValue { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfig
{
    [JsonPropertyName("enableTimeLimit")]
    public bool EnableTimeLimit { get; set; }

    [JsonPropertyName("expireYear")]
    public int ExpireYear { get; set; } = 2026;

    [JsonPropertyName("expireMonth")]
    public int ExpireMonth { get; set; } = 12;

    [JsonPropertyName("expireDay")]
    public int ExpireDay { get; set; } = 31;

    [JsonPropertyName("enableDeviceBinding")]
    public bool EnableDeviceBinding { get; set; }

    [JsonPropertyName("enableOnlineVerify")]
    public bool EnableOnlineVerify { get; set; }

    [JsonPropertyName("verifyUrl")]
    public string? VerifyUrl { get; set; }
}
