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

    /// <summary>
    /// DLL绑定槽位号（通达信中绑定到第几个DLL，如1表示TDXDLL1，2表示TDXDLL2）
    /// 默认值为1
    /// </summary>
    [JsonPropertyName("dllSlot")]
    public int DllSlot { get; set; } = 1;

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

    /// <summary>
    /// 是否使用全局数据缓冲区（当公式需要超过3个数据输入时使用）
    /// true时，C/O/H/L/V 从全局数组读取而非 pfIn1/pfIn2/pfIn3
    /// 需配合 DATALOADER 类型的函数预先加载数据
    /// </summary>
    [JsonPropertyName("useGlobalData")]
    public bool UseGlobalData { get; set; }

    /// <summary>
    /// 原始通达信公式文件路径（相对或绝对路径）
    /// 如果提供，生成器会自动提取绘图指令，生成可直接粘贴到通达信的最终公式文件
    /// </summary>
    [JsonPropertyName("originalFormulaFile")]
    public string? OriginalFormulaFile { get; set; }

    /// <summary>
    /// 此函数在通达信中对应的输出变量名
    /// 用于生成最终公式时替换原始公式中的信号变量
    /// 例如："低吸信号"
    /// </summary>
    [JsonPropertyName("outputVariableName")]
    public string? OutputVariableName { get; set; }
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
