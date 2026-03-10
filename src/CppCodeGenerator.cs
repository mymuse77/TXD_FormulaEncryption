using System;
using System.IO;
using System.Text;

namespace TdxDllGenerator;

/// <summary>
/// C++代码生成器
/// </summary>
public class CppCodeGenerator
{
    private readonly TdxFormulaConfig _config;
    private readonly string _outputPath;
    private bool _is64Bit = false;

    public CppCodeGenerator(TdxFormulaConfig config, string outputPath)
    {
        _config = config;
        _outputPath = outputPath;
    }

    /// <summary>
    /// 生成完整项目
    /// </summary>
    public void GenerateProject(bool is64Bit = false)
    {
        _is64Bit = is64Bit;
        Directory.CreateDirectory(_outputPath);

        // 生成头文件
        GenerateHeaderFile();

        // 生成源文件
        GenerateSourceFile();

        // 生成模块定义文件
        GenerateDefFile();

        // 生成CMakeLists.txt (支持32/64位)
        GenerateCMakeFile();

        // 生成Visual Studio项目文件 (支持32/64位)
        GenerateVcxprojFile();

        // 生成编译脚本
        GenerateBuildScript();

        // 生成编译说明
        GenerateBuildInstructions();

        Console.WriteLine($"项目已生成到: {_outputPath}");
        Console.WriteLine($"插件名称: {_config.PluginName}");
        Console.WriteLine($"函数数量: {_config.Functions.Count}");
        Console.WriteLine($"目标架构: {(is64Bit ? "x64 (64位)" : "Win32 (32位)")}");
    }

    /// <summary>
    /// 生成PluginTCalcFunc.h头文件
    /// </summary>
    private void GenerateHeaderFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// PluginTCalcFunc.h");
        sb.AppendLine("// 通达信DLL插件接口头文件");
        sb.AppendLine("// 由TdxDllGenerator自动生成");
        sb.AppendLine();
        sb.AppendLine("#ifndef __PLUGIN_TCALC_FUNC");
        sb.AppendLine("#define __PLUGIN_TCALC_FUNC");
        sb.AppendLine();
        sb.AppendLine("#include <windows.h>");
        sb.AppendLine();
        sb.AppendLine("#pragma pack(push, 1)");
        sb.AppendLine();
        sb.AppendLine("// 函数指针类型(数据个数, 输出, 输入a, 输入b, 输入c)");
        sb.AppendLine("typedef void(*pPluginFUNC)(int, float*, float*, float*, float*);");
        sb.AppendLine();
        sb.AppendLine("typedef struct tagPluginTCalcFuncInfo");
        sb.AppendLine("{");
        sb.AppendLine("    unsigned short nFuncMark;  // 函数编号");
        sb.AppendLine("    pPluginFUNC pCallFunc;     // 函数指针");
        sb.AppendLine("} PluginTCalcFuncInfo;");
        sb.AppendLine();
        sb.AppendLine("// 入口函数声明");
        sb.AppendLine("extern \"C\" __declspec(dllexport) BOOL RegisterTdxFunc(PluginTCalcFuncInfo** pFun);");
        sb.AppendLine();
        sb.AppendLine("#pragma pack(pop)");
        sb.AppendLine();
        sb.AppendLine("#endif // __PLUGIN_TCALC_FUNC");

        File.WriteAllText(Path.Combine(_outputPath, "PluginTCalcFunc.h"), sb.ToString());
        Console.WriteLine("已生成: PluginTCalcFunc.h");
    }

    /// <summary>
    /// 生成源文件
    /// </summary>
    private void GenerateSourceFile()
    {
        var sb = new StringBuilder();
        
        // 文件头
        sb.AppendLine($"// {_config.PluginName}.cpp");
        sb.AppendLine($"// {_config.PluginDescription}");
        sb.AppendLine("// 由TdxDllGenerator自动生成");
        sb.AppendLine();
        sb.AppendLine("#include \"PluginTCalcFunc.h\"");
        sb.AppendLine("#include <windows.h>");
        sb.AppendLine();

        // 安全验证代码
        GenerateSecurityCode(sb);

        // 函数回调指针
        sb.AppendLine("// 函数回调指针（用于调用通达信内部数据）");
        sb.AppendLine("pPluginFUNC g_pFuncCallBack = NULL;");
        sb.AppendLine();

        // 生成各个函数
        foreach (var func in _config.Functions)
        {
            GenerateFunction(sb, func);
        }

        // 函数注册表
        sb.AppendLine("// 函数注册表");
        sb.AppendLine("PluginTCalcFuncInfo g_CalcFuncSets[] = {");
        foreach (var func in _config.Functions)
        {
            sb.AppendLine($"    {{{func.Id}, {func.Name}}},  // {func.Description}");
        }
        sb.AppendLine("    {0, NULL}       // 结束标记");
        sb.AppendLine("};");
        sb.AppendLine();

        // DLL主入口函数
        sb.AppendLine("// DLL主入口函数");
        sb.AppendLine("BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)");
        sb.AppendLine("{");
        sb.AppendLine("    switch (ul_reason_for_call)");
        sb.AppendLine("    {");
        sb.AppendLine("    case DLL_PROCESS_ATTACH:");
        sb.AppendLine("        DisableThreadLibraryCalls(hModule);");
        sb.AppendLine("        break;");
        sb.AppendLine("    case DLL_THREAD_ATTACH:");
        sb.AppendLine("    case DLL_THREAD_DETACH:");
        sb.AppendLine("    case DLL_PROCESS_DETACH:");
        sb.AppendLine("        break;");
        sb.AppendLine("    }");
        sb.AppendLine("    return TRUE;");
        sb.AppendLine("}");
        sb.AppendLine();

        // DLL入口函数
        sb.AppendLine("// DLL注册函数 - 通达信调用此函数获取函数表");
        sb.AppendLine("extern \"C\" __declspec(dllexport) BOOL RegisterTdxFunc(PluginTCalcFuncInfo** pFun)");
        sb.AppendLine("{");
        
        // 安全检查
        if (_config.Security.EnableTimeLimit)
        {
            sb.AppendLine("    // 时间限制验证");
            sb.AppendLine("    if (!CheckExpiration())");
            sb.AppendLine("    {");
            sb.AppendLine("        return FALSE;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("    // 设备绑定验证");
            sb.AppendLine("    if (!CheckMachineID())");
            sb.AppendLine("    {");
            sb.AppendLine("        return FALSE;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        if (_config.Security.EnableOnlineVerify)
        {
            sb.AppendLine("    // 网络验证");
            sb.AppendLine("    if (!OnlineVerify())");
            sb.AppendLine("    {");
            sb.AppendLine("        return FALSE;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    *pFun = g_CalcFuncSets;");
        sb.AppendLine("    return TRUE;");
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(_outputPath, $"{_config.PluginName}.cpp"), sb.ToString());
        Console.WriteLine($"已生成: {_config.PluginName}.cpp");
    }

    /// <summary>
    /// 生成安全验证代码
    /// </summary>
    private void GenerateSecurityCode(StringBuilder sb)
    {
        if (!_config.Security.EnableTimeLimit && 
            !_config.Security.EnableDeviceBinding && 
            !_config.Security.EnableOnlineVerify)
        {
            return;
        }

        sb.AppendLine("// ==================== 安全验证代码 ====================");
        sb.AppendLine();

        if (_config.Security.EnableTimeLimit)
        {
            sb.AppendLine("// 时间限制验证");
            sb.AppendLine("bool CheckExpiration()");
            sb.AppendLine("{");
            sb.AppendLine("    SYSTEMTIME st;");
            sb.AppendLine("    GetSystemTime(&st);");
            sb.AppendLine($"    return (st.wYear < {_config.Security.ExpireYear});");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("// 设备绑定验证");
            sb.AppendLine("#include <stdio.h>");
            sb.AppendLine("#include <iphlpapi.h>");
            sb.AppendLine("#pragma comment(lib, \"iphlpapi.lib\")");
            sb.AppendLine("#pragma comment(lib, \"ws2_32.lib\")");
            sb.AppendLine("");
            sb.AppendLine("// 获取MAC地址");
            sb.AppendLine("bool GetMacAddress(unsigned char* mac)");
            sb.AppendLine("{");
            sb.AppendLine("    PIP_ADAPTER_INFO pAdapterInfo = (PIP_ADAPTER_INFO)malloc(sizeof(IP_ADAPTER_INFO));");
            sb.AppendLine("    ULONG ulOutBufLen = sizeof(IP_ADAPTER_INFO);");
            sb.AppendLine("    ");
            sb.AppendLine("    if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == ERROR_BUFFER_OVERFLOW) {");
            sb.AppendLine("        free(pAdapterInfo);");
            sb.AppendLine("        pAdapterInfo = (PIP_ADAPTER_INFO)malloc(ulOutBufLen);");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == NO_ERROR) {");
            sb.AppendLine("        PIP_ADAPTER_INFO pAdapter = pAdapterInfo;");
            sb.AppendLine("        while (pAdapter) {");
            sb.AppendLine("            if (pAdapter->Type == MIB_IF_TYPE_ETHERNET && pAdapter->AddressLength == 6) {");
            sb.AppendLine("                memcpy(mac, pAdapter->Address, 6);");
            sb.AppendLine("                free(pAdapterInfo);");
            sb.AppendLine("                return true;");
            sb.AppendLine("            }");
            sb.AppendLine("            pAdapter = pAdapter->Next;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    free(pAdapterInfo);");
            sb.AppendLine("    return false;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 生成机器指纹");
            sb.AppendLine("void GenerateMachineFingerprint(char* fingerprint, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    unsigned char mac[6] = {0};");
            sb.AppendLine("    GetMacAddress(mac);");
            sb.AppendLine("    ");
            sb.AppendLine("    // 基于MAC地址生成指纹");
            sb.AppendLine("    unsigned int hash = 0;");
            sb.AppendLine("    for (int i = 0; i < 6; i++) {");
            sb.AppendLine("        hash = hash * 31 + mac[i];");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    sprintf_s(fingerprint, maxLen, \"%08X%08X\", hash, hash ^ 0xAAAAAAAA);");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 从授权文件读取指纹");
            sb.AppendLine("bool LoadLicenseFingerprint(char* fingerprint, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    FILE* fp = NULL;");
            sb.AppendLine("    fopen_s(&fp, \"license.dat\", \"r\");");
            sb.AppendLine("    if (!fp) return false;");
            sb.AppendLine("    ");
            sb.AppendLine("    char line[256];");
            sb.AppendLine("    while (fgets(line, sizeof(line), fp)) {");
            sb.AppendLine("        if (strstr(line, \"machineFingerprint\")) {");
            sb.AppendLine("            char* start = strchr(line, '\"');");
            sb.AppendLine("            if (start) {");
            sb.AppendLine("                char* end = strchr(start + 1, '\"');");
            sb.AppendLine("                if (end) {");
            sb.AppendLine("                    *end = '\\0';");
            sb.AppendLine("                    strncpy_s(fingerprint, maxLen, start + 1, maxLen - 1);");
            sb.AppendLine("                    fclose(fp);");
            sb.AppendLine("                    return true;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    fclose(fp);");
            sb.AppendLine("    return false;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 检查设备授权");
            sb.AppendLine("bool CheckMachineID()");
            sb.AppendLine("{");
            sb.AppendLine("    char currentFingerprint[32];");
            sb.AppendLine("    char authorizedFingerprint[32];");
            sb.AppendLine("    ");
            sb.AppendLine("    GenerateMachineFingerprint(currentFingerprint, sizeof(currentFingerprint));");
            sb.AppendLine("    ");
            sb.AppendLine("    if (!LoadLicenseFingerprint(authorizedFingerprint, sizeof(authorizedFingerprint))) {");
            sb.AppendLine("        MessageBoxA(NULL, \"未找到授权文件 license.dat！\\n请先获取设备授权。\\n\\n当前机器指纹：\",");
            sb.AppendLine("                    \"授权失败\", MB_OK | MB_ICONERROR);");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    if (strcmp(currentFingerprint, authorizedFingerprint) != 0) {");
            sb.AppendLine("        char msg[256];");
            sb.AppendLine("        sprintf_s(msg, sizeof(msg), \"设备未授权！\\n\\n当前指纹：%s\\n授权指纹：%s\\n\\n请联系开发者重新授权。\",");
            sb.AppendLine("                currentFingerprint, authorizedFingerprint);");
            sb.AppendLine("        MessageBoxA(NULL, msg, \"授权失败\", MB_OK | MB_ICONERROR);");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (_config.Security.EnableOnlineVerify)
        {
            sb.AppendLine("// 网络验证");
            sb.AppendLine("bool OnlineVerify()");
            sb.AppendLine("{");
            sb.AppendLine("    // TODO: 实现网络验证逻辑");
            if (_config.Security.VerifyUrl != null)
            {
                sb.AppendLine($"    // 验证服务器: {_config.Security.VerifyUrl}");
            }
            sb.AppendLine("    // 可以使用WinHTTP或WinInet进行HTTP请求");
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("// ==================== 安全验证代码结束 ====================");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成单个函数
    /// </summary>
    private void GenerateFunction(StringBuilder sb, FunctionConfig func)
    {
        sb.AppendLine($"// {func.Description}");
        sb.AppendLine($"void {func.Name}(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)");
        sb.AppendLine("{");

        switch (func.FormulaType.ToUpper())
        {
            case "MA":
                GenerateMACode(sb, func);
                break;
            case "EMA":
                GenerateEMACode(sb, func);
                break;
            case "RSI":
                GenerateRSICode(sb, func);
                break;
            case "MACD":
                GenerateMACDCode(sb, func);
                break;
            case "CUSTOM":
                if (!string.IsNullOrEmpty(func.CustomCode))
                {
                    sb.AppendLine(func.CustomCode);
                }
                else
                {
                    sb.AppendLine("    // TODO: 实现自定义算法");
                    sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
                    sb.AppendLine("    {");
                    sb.AppendLine("        pfOut[i] = pfIn1[i];");
                    sb.AppendLine("    }");
                }
                break;
            default:
                sb.AppendLine("    // TODO: 实现算法");
                sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
                sb.AppendLine("    {");
                sb.AppendLine("        pfOut[i] = pfIn1[i];");
                sb.AppendLine("    }");
                break;
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成移动平均线代码
    /// </summary>
    private void GenerateMACode(StringBuilder sb, FunctionConfig func)
    {
        var period = func.Parameters.Count > 0 ? (int)func.Parameters[0].DefaultValue : 5;
        sb.AppendLine($"    int nPeriod = (int)pfIn2[0];");
        sb.AppendLine($"    if (nPeriod <= 0) nPeriod = {period};");
        sb.AppendLine();
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        float sum = 0;");
        sb.AppendLine("        int count = 0;");
        sb.AppendLine("        for (int j = i; j >= 0 && j > i - nPeriod; j--)");
        sb.AppendLine("        {");
        sb.AppendLine("            sum += pfIn1[j];");
        sb.AppendLine("            count++;");
        sb.AppendLine("        }");
        sb.AppendLine("        pfOut[i] = (count > 0) ? sum / count : 0;");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成指数移动平均线代码
    /// </summary>
    private void GenerateEMACode(StringBuilder sb, FunctionConfig func)
    {
        var period = func.Parameters.Count > 0 ? (int)func.Parameters[0].DefaultValue : 12;
        sb.AppendLine($"    int nPeriod = (int)pfIn2[0];");
        sb.AppendLine($"    if (nPeriod <= 0) nPeriod = {period};");
        sb.AppendLine("    float alpha = 2.0f / (nPeriod + 1);");
        sb.AppendLine();
        sb.AppendLine("    if (nDataLen > 0)");
        sb.AppendLine("    {");
        sb.AppendLine("        pfOut[0] = pfIn1[0];");
        sb.AppendLine("        for (int i = 1; i < nDataLen; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            pfOut[i] = alpha * pfIn1[i] + (1 - alpha) * pfOut[i - 1];");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成RSI代码
    /// </summary>
    private void GenerateRSICode(StringBuilder sb, FunctionConfig func)
    {
        var period = func.Parameters.Count > 0 ? (int)func.Parameters[0].DefaultValue : 14;
        sb.AppendLine($"    int nPeriod = (int)pfIn2[0];");
        sb.AppendLine($"    if (nPeriod <= 0) nPeriod = {period};");
        sb.AppendLine();
        sb.AppendLine("    for (int i = nPeriod; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        float gain = 0, loss = 0;");
        sb.AppendLine("        for (int j = i - nPeriod + 1; j <= i; j++)");
        sb.AppendLine("        {");
        sb.AppendLine("            float change = pfIn1[j] - pfIn1[j - 1];");
        sb.AppendLine("            if (change > 0) gain += change;");
        sb.AppendLine("            else loss -= change;");
        sb.AppendLine("        }");
        sb.AppendLine("        float avgGain = gain / nPeriod;");
        sb.AppendLine("        float avgLoss = loss / nPeriod;");
        sb.AppendLine("        if (avgLoss == 0) pfOut[i] = 100;");
        sb.AppendLine("        else pfOut[i] = 100 - (100 / (1 + avgGain / avgLoss));");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成MACD代码
    /// </summary>
    private void GenerateMACDCode(StringBuilder sb, FunctionConfig func)
    {
        sb.AppendLine("    // MACD算法实现 - 使用输出数组作为临时存储");
        sb.AppendLine("    int fastPeriod = 12, slowPeriod = 26;");
        sb.AppendLine("    float alphaFast = 2.0f / (fastPeriod + 1);");
        sb.AppendLine("    float alphaSlow = 2.0f / (slowPeriod + 1);");
        sb.AppendLine("    float emaFast = 0, emaSlow = 0;");
        sb.AppendLine();
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (i == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            emaFast = pfIn1[i];");
        sb.AppendLine("            emaSlow = pfIn1[i];");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            emaFast = alphaFast * pfIn1[i] + (1 - alphaFast) * emaFast;");
        sb.AppendLine("            emaSlow = alphaSlow * pfIn1[i] + (1 - alphaSlow) * emaSlow;");
        sb.AppendLine("        }");
        sb.AppendLine("        pfOut[i] = emaFast - emaSlow;  // DIF线");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 生成模块定义文件
    /// </summary>
    private void GenerateDefFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("; " + _config.PluginName + ".def");
        sb.AppendLine("; 模块定义文件 - 通达信DLL插件");
        sb.AppendLine();
        sb.AppendLine("LIBRARY " + _config.PluginName);
        sb.AppendLine();
        sb.AppendLine("EXPORTS");
        sb.AppendLine("    ; 通达信插件入口函数");
        sb.AppendLine("    RegisterTdxFunc @1");
        sb.AppendLine();
        sb.AppendLine("    ; DLL入口点 (可选，但建议保留)");
        sb.AppendLine("    DllMain PRIVATE");

        File.WriteAllText(Path.Combine(_outputPath, $"{_config.PluginName}.def"), sb.ToString());
        Console.WriteLine($"已生成: {_config.PluginName}.def");
    }

    /// <summary>
    /// 生成CMakeLists.txt
    /// </summary>
    private void GenerateCMakeFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("cmake_minimum_required(VERSION 3.10)");
        sb.AppendLine();
        sb.AppendLine($"project({_config.PluginName})");
        sb.AppendLine();
        sb.AppendLine("set(CMAKE_CXX_STANDARD 11)");
        sb.AppendLine("set(CMAKE_CXX_STANDARD_REQUIRED ON)");
        sb.AppendLine();
        sb.AppendLine("# 设置编译为32位");
        sb.AppendLine("if(CMAKE_SIZEOF_VOID_P EQUAL 8)");
        sb.AppendLine("    message(WARNING \"建议编译为32位以兼容通达信\")");
        sb.AppendLine("endif()");
        sb.AppendLine();
        sb.AppendLine("# 源文件");
        sb.AppendLine($"set(SOURCES {_config.PluginName}.cpp)");
        sb.AppendLine();
        sb.AppendLine("# 创建DLL");
        sb.AppendLine($"add_library(${{PROJECT_NAME}} SHARED ${{SOURCES}} {_config.PluginName}.def)");
        sb.AppendLine();
        sb.AppendLine("# 使用静态运行时库(避免依赖VCRUNTIME140.dll)");
        sb.AppendLine("if(MSVC)");
        sb.AppendLine("    set_target_properties(${PROJECT_NAME} PROPERTIES");
        sb.AppendLine("        MSVC_RUNTIME_LIBRARY \"MultiThreaded\"");
        sb.AppendLine("    )");
        sb.AppendLine("endif()");
        sb.AppendLine();
        sb.AppendLine("# 设置输出目录");
        sb.AppendLine("set_target_properties(${PROJECT_NAME} PROPERTIES");
        sb.AppendLine("    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin");
        sb.AppendLine("    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib");
        sb.AppendLine(")");

        File.WriteAllText(Path.Combine(_outputPath, "CMakeLists.txt"), sb.ToString());
        Console.WriteLine("已生成: CMakeLists.txt");
    }

    /// <summary>
    /// 生成Visual Studio项目文件
    /// </summary>
    private void GenerateVcxprojFile()
    {
        var guid = Guid.NewGuid().ToString().ToUpper();
        var sb = new StringBuilder();
        
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<Project DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
        sb.AppendLine("  <ItemGroup Label=\"ProjectConfigurations\">");
        sb.AppendLine("    <ProjectConfiguration Include=\"Debug|Win32\">");
        sb.AppendLine("      <Configuration>Debug</Configuration>");
        sb.AppendLine("      <Platform>Win32</Platform>");
        sb.AppendLine("    </ProjectConfiguration>");
        sb.AppendLine("    <ProjectConfiguration Include=\"Release|Win32\">");
        sb.AppendLine("      <Configuration>Release</Configuration>");
        sb.AppendLine("      <Platform>Win32</Platform>");
        sb.AppendLine("    </ProjectConfiguration>");
        sb.AppendLine("    <ProjectConfiguration Include=\"Debug|x64\">");
        sb.AppendLine("      <Configuration>Debug</Configuration>");
        sb.AppendLine("      <Platform>x64</Platform>");
        sb.AppendLine("    </ProjectConfiguration>");
        sb.AppendLine("    <ProjectConfiguration Include=\"Release|x64\">");
        sb.AppendLine("      <Configuration>Release</Configuration>");
        sb.AppendLine("      <Platform>x64</Platform>");
        sb.AppendLine("    </ProjectConfiguration>");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine($"  <PropertyGroup Label=\"Globals\">");
        sb.AppendLine($"    <ProjectGuid>{{{guid}}}</ProjectGuid>");
        sb.AppendLine($"    <RootNamespace>{_config.PluginName}</RootNamespace>");
        sb.AppendLine("    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.Default.props\" />");
        
        // Win32 configurations
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|Win32'\" Label=\"Configuration\">");
        sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
        sb.AppendLine("    <UseDebugLibraries>true</UseDebugLibraries>");
        sb.AppendLine("    <PlatformToolset>v143</PlatformToolset>");
        sb.AppendLine("    <CharacterSet>MultiByte</CharacterSet>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|Win32'\" Label=\"Configuration\">");
        sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
        sb.AppendLine("    <UseDebugLibraries>false</UseDebugLibraries>");
        sb.AppendLine("    <PlatformToolset>v143</PlatformToolset>");
        sb.AppendLine("    <WholeProgramOptimization>true</WholeProgramOptimization>");
        sb.AppendLine("    <CharacterSet>MultiByte</CharacterSet>");
        sb.AppendLine("  </PropertyGroup>");
        
        // x64 configurations
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|x64'\" Label=\"Configuration\">");
        sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
        sb.AppendLine("    <UseDebugLibraries>true</UseDebugLibraries>");
        sb.AppendLine("    <PlatformToolset>v143</PlatformToolset>");
        sb.AppendLine("    <CharacterSet>MultiByte</CharacterSet>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\" Label=\"Configuration\">");
        sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
        sb.AppendLine("    <UseDebugLibraries>false</UseDebugLibraries>");
        sb.AppendLine("    <PlatformToolset>v143</PlatformToolset>");
        sb.AppendLine("    <WholeProgramOptimization>true</WholeProgramOptimization>");
        sb.AppendLine("    <CharacterSet>MultiByte</CharacterSet>");
        sb.AppendLine("  </PropertyGroup>");
        
        sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.props\" />");
        sb.AppendLine("  <ImportGroup Label=\"ExtensionSettings\">");
        sb.AppendLine("  </ImportGroup>");
        
        // Property sheets for all platforms
        sb.AppendLine("  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Debug|Win32'\">");
        sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
        sb.AppendLine("  </ImportGroup>");
        sb.AppendLine("  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Release|Win32'\">");
        sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
        sb.AppendLine("  </ImportGroup>");
        sb.AppendLine("  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Debug|x64'\">");
        sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
        sb.AppendLine("  </ImportGroup>");
        sb.AppendLine("  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\">");
        sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
        sb.AppendLine("  </ImportGroup>");
        
        sb.AppendLine("  <PropertyGroup Label=\"UserMacros\" />");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|Win32'\">");
        sb.AppendLine("    <LinkIncremental>true</LinkIncremental>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|Win32'\">");
        sb.AppendLine("    <LinkIncremental>false</LinkIncremental>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|x64'\">");
        sb.AppendLine("    <LinkIncremental>true</LinkIncremental>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\">");
        sb.AppendLine("    <LinkIncremental>false</LinkIncremental>");
        sb.AppendLine("  </PropertyGroup>");
        
        // Win32 Debug
        sb.AppendLine("  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|Win32'\">");
        sb.AppendLine("    <ClCompile>");
        sb.AppendLine("      <WarningLevel>Level3</WarningLevel>");
        sb.AppendLine("      <SDLCheck>true</SDLCheck>");
        sb.AppendLine("      <PreprocessorDefinitions>WIN32;_DEBUG;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
        sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
        sb.AppendLine("      <PrecompiledHeader>NotUsing</PrecompiledHeader>");
        sb.AppendLine("      <RuntimeLibrary>MultiThreadedDebug</RuntimeLibrary>");
        sb.AppendLine("    </ClCompile>");
        sb.AppendLine("    <Link>");
        sb.AppendLine("      <SubSystem>Windows</SubSystem>");
        sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
        sb.AppendLine($"      <ModuleDefinitionFile>{_config.PluginName}.def</ModuleDefinitionFile>");
        sb.AppendLine("      <TargetMachine>MachineX86</TargetMachine>");
        sb.AppendLine("    </Link>");
        sb.AppendLine("  </ItemDefinitionGroup>");
        
        // Win32 Release
        sb.AppendLine("  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|Win32'\">");
        sb.AppendLine("    <ClCompile>");
        sb.AppendLine("      <WarningLevel>Level3</WarningLevel>");
        sb.AppendLine("      <FunctionLevelLinking>true</FunctionLevelLinking>");
        sb.AppendLine("      <IntrinsicFunctions>true</IntrinsicFunctions>");
        sb.AppendLine("      <SDLCheck>true</SDLCheck>");
        sb.AppendLine("      <PreprocessorDefinitions>WIN32;NDEBUG;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
        sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
        sb.AppendLine("      <PrecompiledHeader>NotUsing</PrecompiledHeader>");
        sb.AppendLine("      <RuntimeLibrary>MultiThreaded</RuntimeLibrary>");
        sb.AppendLine("    </ClCompile>");
        sb.AppendLine("    <Link>");
        sb.AppendLine("      <SubSystem>Windows</SubSystem>");
        sb.AppendLine("      <EnableCOMDATFolding>true</EnableCOMDATFolding>");
        sb.AppendLine("      <OptimizeReferences>true</OptimizeReferences>");
        sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
        sb.AppendLine($"      <ModuleDefinitionFile>{_config.PluginName}.def</ModuleDefinitionFile>");
        sb.AppendLine("      <TargetMachine>MachineX86</TargetMachine>");
        sb.AppendLine("    </Link>");
        sb.AppendLine("  </ItemDefinitionGroup>");
        
        // x64 Debug
        sb.AppendLine("  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|x64'\">");
        sb.AppendLine("    <ClCompile>");
        sb.AppendLine("      <WarningLevel>Level3</WarningLevel>");
        sb.AppendLine("      <SDLCheck>true</SDLCheck>");
        sb.AppendLine("      <PreprocessorDefinitions>_DEBUG;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
        sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
        sb.AppendLine("      <PrecompiledHeader>NotUsing</PrecompiledHeader>");
        sb.AppendLine("      <RuntimeLibrary>MultiThreadedDebug</RuntimeLibrary>");
        sb.AppendLine("    </ClCompile>");
        sb.AppendLine("    <Link>");
        sb.AppendLine("      <SubSystem>Windows</SubSystem>");
        sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
        sb.AppendLine($"      <ModuleDefinitionFile>{_config.PluginName}.def</ModuleDefinitionFile>");
        sb.AppendLine("      <TargetMachine>MachineX64</TargetMachine>");
        sb.AppendLine("    </Link>");
        sb.AppendLine("  </ItemDefinitionGroup>");
        
        // x64 Release
        sb.AppendLine("  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\">");
        sb.AppendLine("    <ClCompile>");
        sb.AppendLine("      <WarningLevel>Level3</WarningLevel>");
        sb.AppendLine("      <FunctionLevelLinking>true</FunctionLevelLinking>");
        sb.AppendLine("      <IntrinsicFunctions>true</IntrinsicFunctions>");
        sb.AppendLine("      <SDLCheck>true</SDLCheck>");
        sb.AppendLine("      <PreprocessorDefinitions>NDEBUG;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
        sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
        sb.AppendLine("      <PrecompiledHeader>NotUsing</PrecompiledHeader>");
        sb.AppendLine("      <RuntimeLibrary>MultiThreaded</RuntimeLibrary>");
        sb.AppendLine("    </ClCompile>");
        sb.AppendLine("    <Link>");
        sb.AppendLine("      <SubSystem>Windows</SubSystem>");
        sb.AppendLine("      <EnableCOMDATFolding>true</EnableCOMDATFolding>");
        sb.AppendLine("      <OptimizeReferences>true</OptimizeReferences>");
        sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
        sb.AppendLine($"      <ModuleDefinitionFile>{_config.PluginName}.def</ModuleDefinitionFile>");
        sb.AppendLine("      <TargetMachine>MachineX64</TargetMachine>");
        sb.AppendLine("      <AdditionalDependencies>iphlpapi.lib;%(AdditionalDependencies)</AdditionalDependencies>");
        sb.AppendLine("    </Link>");
        sb.AppendLine("  </ItemDefinitionGroup>");
        
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine($"    <ClCompile Include=\"{_config.PluginName}.cpp\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <ClInclude Include=\"PluginTCalcFunc.h\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine($"    <None Include=\"{_config.PluginName}.def\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.targets\" />");
        sb.AppendLine("</Project>");

        File.WriteAllText(Path.Combine(_outputPath, $"{_config.PluginName}.vcxproj"), sb.ToString());
        Console.WriteLine($"已生成: {_config.PluginName}.vcxproj");
    }

    /// <summary>
    /// 生成编译脚本
    /// </summary>
    private void GenerateBuildScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul");
        sb.AppendLine("echo ========================================");
        sb.AppendLine("echo  通达信DLL编译脚本");
        sb.AppendLine("echo ========================================");
        sb.AppendLine("echo.");
        sb.AppendLine();
        sb.AppendLine("REM 检查是否安装了Visual Studio");
        sb.AppendLine("set \"VS_PATH=\"");
        sb.AppendLine("for %%i in (\"C:\\Program Files\\Microsoft Visual Studio\\2022\\Professional\" \"C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\" \"C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\") do (");
        sb.AppendLine("    if exist \"%%~i\\MSBuild\\Current\\Bin\\MSBuild.exe\" (");
        sb.AppendLine("        set \"VS_PATH=%%~i\"");
        sb.AppendLine("        goto :found_vs");
        sb.AppendLine("    )");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("for %%i in (\"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Professional\" \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\" \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\") do (");
        sb.AppendLine("    if exist \"%%~i\\MSBuild\\Current\\Bin\\MSBuild.exe\" (");
        sb.AppendLine("        set \"VS_PATH=%%~i\"");
        sb.AppendLine("        goto :found_vs");
        sb.AppendLine("    )");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo 错误: 未找到Visual Studio 2019/2022");
        sb.AppendLine("echo 请确保已安装Visual Studio并且包含C++工作负载");
        sb.AppendLine("echo.");
        sb.AppendLine("pause");
        sb.AppendLine("exit /b 1");
        sb.AppendLine();
        sb.AppendLine(":found_vs");
        sb.AppendLine("echo 找到Visual Studio: %VS_PATH%");
        sb.AppendLine("echo.");
        sb.AppendLine();
        sb.AppendLine("REM 设置项目文件");
        sb.AppendLine($"set \"PROJECT_FILE={_config.PluginName}.vcxproj\"");
        sb.AppendLine();
        sb.AppendLine("if not exist \"%PROJECT_FILE%\" (");
        sb.AppendLine("    echo 错误: 未找到项目文件 %PROJECT_FILE%");
        sb.AppendLine("    echo.");
        sb.AppendLine("    pause");
        sb.AppendLine("    exit /b 1");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo 项目文件: %PROJECT_FILE%");
        sb.AppendLine("echo.");
        sb.AppendLine();
        sb.AppendLine("REM 询问用户选择平台");
        sb.AppendLine("echo 请选择编译目标平台:");
        sb.AppendLine("echo 1. Win32 (32位) - 适用于旧版通达信");
        sb.AppendLine("echo 2. x64   (64位) - 适用于新版64位通达信");
        sb.AppendLine("echo.");
        sb.AppendLine("set /p PLATFORM_CHOICE=请输入选项 (1 或 2): ");
        sb.AppendLine();
        sb.AppendLine("if \"%PLATFORM_CHOICE%\"==\"1\" (");
        sb.AppendLine("    set \"PLATFORM=Win32\"");
        sb.AppendLine("    set \"PLATFORM_NAME=Win32 (32位)\"");
        sb.AppendLine("    set \"OUTPUT_DIR=Release\"");
        sb.AppendLine(") else if \"%PLATFORM_CHOICE%\"==\"2\" (");
        sb.AppendLine("    set \"PLATFORM=x64\"");
        sb.AppendLine("    set \"PLATFORM_NAME=x64 (64位)\"");
        sb.AppendLine("    set \"OUTPUT_DIR=x64\\Release\"");
        sb.AppendLine(") else (");
        sb.AppendLine("    echo 错误: 无效选项，默认使用 Win32");
        sb.AppendLine("    set \"PLATFORM=Win32\"");
        sb.AppendLine("    set \"PLATFORM_NAME=Win32 (32位)\"");
        sb.AppendLine("    set \"OUTPUT_DIR=Release\"");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo.");
        sb.AppendLine("echo 正在编译 Release %PLATFORM_NAME% 版本...");
        sb.AppendLine("echo.");
        sb.AppendLine("\"%VS_PATH%\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"%PROJECT_FILE%\" -p:Configuration=Release -p:Platform=%PLATFORM%");
        sb.AppendLine();
        sb.AppendLine("if errorlevel 1 (");
        sb.AppendLine("    echo.");
        sb.AppendLine("    echo ========================================");
        sb.AppendLine("    echo  编译失败!");
        sb.AppendLine("    echo ========================================");
        sb.AppendLine("    echo.");
        sb.AppendLine("    echo 可能的原因:");
        sb.AppendLine("    echo 1. 未安装Windows SDK");
        sb.AppendLine("    echo 2. 未安装C++工作负载");
        sb.AppendLine("    echo 3. 代码存在语法错误");
        sb.AppendLine("    echo.");
        sb.AppendLine("    pause");
        sb.AppendLine("    exit /b 1");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo.");
        sb.AppendLine("echo ========================================");
        sb.AppendLine("echo  编译成功!");
        sb.AppendLine("echo ========================================");
        sb.AppendLine("echo.");
        sb.AppendLine("echo 输出文件: %OUTPUT_DIR%\\" + _config.PluginName + ".dll");
        sb.AppendLine("echo.");
        sb.AppendLine("echo 使用方法:");
        sb.AppendLine("echo 1. 将 %OUTPUT_DIR%\\" + _config.PluginName + ".dll 复制到通达信目录");
        sb.AppendLine("echo 2. 打开通达信,按 Ctrl+F 进入公式管理器");
        sb.AppendLine("echo 3. 点击【DLL函数】→ 选择编号 → 【打开绑定】");
        sb.AppendLine("echo 4. 选择生成的DLL文件");
        sb.AppendLine("echo.");
        sb.AppendLine("echo 注意:");
        sb.AppendLine("echo - 32位通达信请使用 Win32 版本");
        sb.AppendLine("echo - 64位通达信请使用 x64 版本");
        sb.AppendLine("echo.");
        sb.AppendLine("pause");

        File.WriteAllText(Path.Combine(_outputPath, "build.bat"), sb.ToString());
        Console.WriteLine("已生成: build.bat (编译脚本)");
    }

    /// <summary>
    /// 生成编译说明
    /// </summary>
    private void GenerateBuildInstructions()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 编译说明");
        sb.AppendLine();
        sb.AppendLine($"## 项目: {_config.PluginName}");
        sb.AppendLine();
        sb.AppendLine("### 方法1: 双击编译脚本 (最简单)");
        sb.AppendLine();
        sb.AppendLine("直接双击 `build.bat` 文件，脚本会自动:");
        sb.AppendLine("1. 查找已安装的Visual Studio");
        sb.AppendLine("2. 编译 Release Win32 版本");
        sb.AppendLine("3. 输出 DLL 文件到 Release 目录");
        sb.AppendLine();
        sb.AppendLine("### 方法2: 使用Visual Studio IDE");
        sb.AppendLine();
        sb.AppendLine("1. 打开Visual Studio 2019/2022");
        sb.AppendLine($"2. 打开 `{_config.PluginName}.vcxproj`");
        sb.AppendLine("3. 选择 **Release** 配置和 **Win32** 平台");
        sb.AppendLine("4. 生成 → 生成解决方案");
        sb.AppendLine();
        sb.AppendLine("### 方法3: 使用Visual Studio命令行");
        sb.AppendLine();
        sb.AppendLine("打开 **Developer Command Prompt for VS 2022**，然后执行:");
        sb.AppendLine();
        sb.AppendLine("```cmd");
        sb.AppendLine($"msbuild {_config.PluginName}.vcxproj /p:Configuration=Release /p:Platform=Win32");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### 方法4: 使用CMake");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("mkdir build");
        sb.AppendLine("cd build");
        sb.AppendLine("cmake .. -A Win32");
        sb.AppendLine("cmake --build . --config Release");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### 方法5: 使用MinGW (无需VS)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine($"g++ -shared -o {_config.PluginName}.dll {_config.PluginName}.cpp {_config.PluginName}.def -m32 -O2 -static-libgcc -static-libstdc++");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## 安装使用");
        sb.AppendLine();
        sb.AppendLine("1. 将生成的 `.dll` 文件复制到通达信安装目录:");
        sb.AppendLine("   ```");
        sb.AppendLine("   通达信安装目录\\T0002\\dlls\\");
        sb.AppendLine("   ```");
        sb.AppendLine();
        sb.AppendLine("2. 打开通达信，按 `Ctrl+F` 进入公式管理器");
        sb.AppendLine("3. 点击【DLL函数】按钮");
        sb.AppendLine("4. 选择DLL编号（1-10）");
        sb.AppendLine($"5. 点击【打开绑定】，选择 `{_config.PluginName}.dll`");
        sb.AppendLine("6. 点击【确定】完成绑定");
        sb.AppendLine();
        sb.AppendLine("## 公式调用示例");
        sb.AppendLine();
        foreach (var func in _config.Functions)
        {
            sb.AppendLine($"### {func.Name} ({func.Description})");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine($"{{调用{func.Name}，函数ID={func.Id}}}");
            sb.AppendLine($"RESULT: TDXDLL1({func.Id}, CLOSE, 10, 0);");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(_outputPath, "README.md"), sb.ToString());
        Console.WriteLine("已生成: README.md");
    }
}
