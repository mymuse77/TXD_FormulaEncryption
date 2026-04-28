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

        // 生成通达信最终公式文件（如果配置了 originalFormulaFile）
        var formulaGenerator = new TdxFormulaGenerator(_config, _outputPath);
        formulaGenerator.GenerateAllFormulas();

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
        sb.AppendLine("#include <cmath>       // For sqrtf, fabsf, etc.");
        sb.AppendLine("#include <algorithm>   // For std::max, std::min");
        sb.AppendLine("#include <vector>      // For dynamic temp buffers sized by nDataLen");
        sb.AppendLine();

        // 安全验证代码
        GenerateSecurityCode(sb);

        // 如果没有启用任何安全功能，定义默认的安全函数
        bool hasSecurity = _config.Security.EnableTimeLimit || 
                           _config.Security.EnableDeviceBinding || 
                           _config.Security.EnableOnlineVerify;
        if (!hasSecurity)
        {
            sb.AppendLine("// 无安全验证 - 使用默认的安全函数");
            sb.AppendLine("volatile long g_securityPass = 1; // 默认通过");
            sb.AppendLine("");
            sb.AppendLine("float ObfuscateResult(float value)");
            sb.AppendLine("{");
            sb.AppendLine("    return value; // 无安全检查，直接返回原值");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 安全初始化 - 无安全功能时默认通过");
            sb.AppendLine("bool InitializeSecurity()");
            sb.AppendLine("{");
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            sb.AppendLine("");
        }

        // 函数回调指针
        sb.AppendLine("// 函数回调指针（用于调用通达信内部数据）");
        sb.AppendLine("pPluginFUNC g_pFuncCallBack = NULL;");
        sb.AppendLine();

        // 检查是否有 useGlobalData 的函数，需要生成全局数据缓冲区
        bool needsGlobalData = _config.Functions.Any(f => f.UseGlobalData);
        if (needsGlobalData)
        {
            sb.AppendLine("// ==================== 全局数据缓冲区 ====================");
            sb.AppendLine("// 用于多步数据加载：DATALOADER函数存储数据，计算函数从全局数组读取");
            sb.AppendLine("static std::vector<float> g_buf_close;  // CLOSE/C");
            sb.AppendLine("static std::vector<float> g_buf_open;   // OPEN/O");
            sb.AppendLine("static std::vector<float> g_buf_high;   // HIGH/H");
            sb.AppendLine("static std::vector<float> g_buf_low;    // LOW/L");
            sb.AppendLine("static std::vector<float> g_buf_vol;    // VOL/V");
            sb.AppendLine();
        }

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
        sb.AppendLine("        // 执行安全初始化检查（防DLL替换和调试）");
        sb.AppendLine("        if (!InitializeSecurity())");
        sb.AppendLine("            return FALSE; // 安全检查失败，阻止DLL加载");
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
        
        // 安全标志和算法腐蚀函数 - 防止一字节patch绕过
        sb.AppendLine("// 全局安全标志 - 0表示未通过验证，1表示通过验证");
        sb.AppendLine("volatile long g_securityPass = 0;");
        sb.AppendLine("");
        sb.AppendLine("// 算法结果腐蚀函数 - 验证失败时返回错误结果而非崩溃");
        sb.AppendLine("// 核心思想: 不直接返回false,而是让算法输出错误值");
        sb.AppendLine("float ObfuscateResult(float correctValue)");
        sb.AppendLine("{");
        sb.AppendLine("    // 如果安全验证通过，返回正确值");
        sb.AppendLine("    if (g_securityPass == 1) {");
        sb.AppendLine("        return correctValue;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    // 验证失败时返回腐蚀后的值(使用多种方式混淆)");
        sb.AppendLine("    static unsigned int xorSeed = 0xDEADBEEF;");
        sb.AppendLine("    xorSeed = xorSeed * 1103515245 + 12345;");
        sb.AppendLine("    float corrupted = correctValue * (float)(int)(xorSeed & 0xFF) / 255.0f;");
        sb.AppendLine("    return corrupted;");
        sb.AppendLine("}");
        sb.AppendLine("");

        if (_config.Security.EnableTimeLimit)
        {
            sb.AppendLine("// 时间限制验证 - 增强版（防时间回调）");
            sb.AppendLine("#include <time.h>");
            sb.AppendLine("");
            sb.AppendLine("// 获取NTP时间（简单实现，实际生产环境建议用可靠NTP库）");
            sb.AppendLine("bool GetNetworkTime(time_t* networkTime)");
            sb.AppendLine("{");
            sb.AppendLine("    // TODO: 实现NTP时间获取，返回系统是否可获取网络时间");
            sb.AppendLine("    // 示例：可从多个时间服务器获取并对比");
            sb.AppendLine("    *networkTime = time(NULL);");
            sb.AppendLine("    return false; // 默认返回false表示未实现");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 获取或创建首次运行时间戳");
            sb.AppendLine("time_t GetFirstRunTime()");
            sb.AppendLine("{");
            sb.AppendLine("    static const char* timeFile = \"C:\\\\Windows\\\\Temp\\\\sysdata.dat\";");
            sb.AppendLine("    FILE* fp = NULL;");
            sb.AppendLine("    fopen_s(&fp, timeFile, \"rb\");");
            sb.AppendLine("    if (fp) {");
            sb.AppendLine("        time_t firstRun = 0;");
            sb.AppendLine("        fread(&firstRun, sizeof(time_t), 1, fp);");
            sb.AppendLine("        fclose(fp);");
            sb.AppendLine("        return firstRun;");
            sb.AppendLine("    }");
            sb.AppendLine("    // 首次运行，记录时间");
            sb.AppendLine("    time_t now = time(NULL);");
            sb.AppendLine("    fopen_s(&fp, timeFile, \"wb\");");
            sb.AppendLine("    if (fp) {");
            sb.AppendLine("        fwrite(&now, sizeof(time_t), 1, fp);");
            sb.AppendLine("        fclose(fp);");
            sb.AppendLine("        SetFileAttributesA(timeFile, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM);");
            sb.AppendLine("    }");
            sb.AppendLine("    return now;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 检查文件时间戳异常（防简单时间回调）");
            sb.AppendLine("bool CheckFileTimeConsistency()");
            sb.AppendLine("{");
            sb.AppendLine("    static const char* checkFiles[] = {");
            sb.AppendLine("        \"C:\\\\Windows\\\\System32\\\\kernel32.dll\",");
            sb.AppendLine("        \"C:\\\\Windows\\\\System32\\\\ntdll.dll\",");
            sb.AppendLine("        NULL");
            sb.AppendLine("    };");
            sb.AppendLine("    ");
            sb.AppendLine("    time_t now = time(NULL);");
            sb.AppendLine("    for (int i = 0; checkFiles[i] != NULL; i++) {");
            sb.AppendLine("        WIN32_FILE_ATTRIBUTE_DATA attr;");
            sb.AppendLine("        if (GetFileAttributesExA(checkFiles[i], GetFileExInfoStandard, &attr)) {");
            sb.AppendLine("            FILETIME ft = attr.ftLastWriteTime;");
            sb.AppendLine("            ULARGE_INTEGER ull;");
            sb.AppendLine("            ull.LowPart = ft.dwLowDateTime;");
            sb.AppendLine("            ull.HighPart = ft.dwHighDateTime;");
            sb.AppendLine("            time_t fileTime = (ull.QuadPart / 10000000ULL) - 11644473600ULL;");
            sb.AppendLine("            // 如果系统时间比文件修改时间早超过1天，可能被回调");
            sb.AppendLine("            if (now < fileTime - 86400) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("bool CheckExpiration()");
            sb.AppendLine("{");
            sb.AppendLine("    SYSTEMTIME st;");
            sb.AppendLine("    GetSystemTime(&st);");
            sb.AppendLine("    ");
            sb.AppendLine("    // 检查是否已过期（精确到月/日）");
            sb.AppendLine($"    int expireYear = {_config.Security.ExpireYear};");
            sb.AppendLine($"    int expireMonth = {_config.Security.ExpireMonth};");
            sb.AppendLine($"    int expireDay = {_config.Security.ExpireDay};");
            sb.AppendLine("    ");
            sb.AppendLine("    // 1. 年份已过期");
            sb.AppendLine("    if (st.wYear > expireYear) return false;");
            sb.AppendLine("    ");
            sb.AppendLine("    // 2. 同年但月份已过期");
            sb.AppendLine("    if (st.wYear == expireYear && st.wMonth > expireMonth) return false;");
            sb.AppendLine("    ");
            sb.AppendLine("    // 3. 同年同月但日期已过期");
            sb.AppendLine("    if (st.wYear == expireYear && st.wMonth == expireMonth && st.wDay > expireDay) return false;");
            sb.AppendLine("    ");
            sb.AppendLine("    // 4. 检查文件时间一致性");
            sb.AppendLine("    if (!CheckFileTimeConsistency()) {");
            sb.AppendLine("        MessageBoxA(NULL, \"系统时间异常！\", \"安全警告\", MB_OK | MB_ICONWARNING);");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 5. 检查是否时间回调（首次运行时间不应晚于当前）");
            sb.AppendLine("    time_t firstRun = GetFirstRunTime();");
            sb.AppendLine("    time_t now = time(NULL);");
            sb.AppendLine("    if (now < firstRun - 60) { // 允许1分钟误差");
            sb.AppendLine("        MessageBoxA(NULL, \"检测到时间异常！请勿修改系统时间。\", \"安全警告\", MB_OK | MB_ICONWARNING);");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("// 设备绑定验证 - 增强版（多硬件指纹+容错匹配）");
            sb.AppendLine("#include <stdio.h>");
            sb.AppendLine("#include <iphlpapi.h>");
            sb.AppendLine("#include <intrin.h>");
            sb.AppendLine("#pragma comment(lib, \"iphlpapi.lib\")");
            sb.AppendLine("#pragma comment(lib, \"ws2_32.lib\")");
            sb.AppendLine("");
            sb.AppendLine("// 硬件信息结构");
            sb.AppendLine("struct HardwareInfo {");
            sb.AppendLine("    unsigned char mac[6];");
            sb.AppendLine("    char cpuId[32];");
            sb.AppendLine("    char volumeSerial[32];");
            sb.AppendLine("    char machineGuid[64];");
            sb.AppendLine("    bool hasMac;");
            sb.AppendLine("    bool hasCpuId;");
            sb.AppendLine("    bool hasVolumeSerial;");
            sb.AppendLine("    bool hasMachineGuid;");
            sb.AppendLine("};");
            sb.AppendLine("");
            sb.AppendLine("// 获取CPU ID");
            sb.AppendLine("void GetCpuId(char* cpuId, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    int cpuInfo[4] = {0};");
            sb.AppendLine("    __cpuid(cpuInfo, 1);");
            sb.AppendLine("    sprintf_s(cpuId, maxLen, \"%08X%08X%08X%08X\",");
            sb.AppendLine("            cpuInfo[0], cpuInfo[1], cpuInfo[2], cpuInfo[3]);");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 获取硬盘序列号");
            sb.AppendLine("bool GetVolumeSerial(char* serial, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    DWORD volumeSerial = 0;");
            sb.AppendLine("    DWORD maxComponentLen = 0;");
            sb.AppendLine("    DWORD fileSystemFlags = 0;");
            sb.AppendLine("    char fileSystemName[MAX_PATH];");
            sb.AppendLine("    ");
            sb.AppendLine("    if (GetVolumeInformationA(\"C:\\\\\", NULL, 0, &volumeSerial,");
            sb.AppendLine("                             &maxComponentLen, &fileSystemFlags,");
            sb.AppendLine("                             fileSystemName, sizeof(fileSystemName))) {");
            sb.AppendLine("        sprintf_s(serial, maxLen, \"%08X\", volumeSerial);");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine("    return false;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 获取机器GUID（从注册表）");
            sb.AppendLine("bool GetMachineGuid(char* guid, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    HKEY hKey;");
            sb.AppendLine("    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE,");
            sb.AppendLine("        \"SOFTWARE\\\\Microsoft\\\\Cryptography\", 0,");
            sb.AppendLine("        KEY_READ | KEY_WOW64_64KEY, &hKey) == ERROR_SUCCESS) {");
            sb.AppendLine("        DWORD type = REG_SZ;");
            sb.AppendLine("        DWORD size = maxLen;");
            sb.AppendLine("        if (RegQueryValueExA(hKey, \"MachineGuid\", NULL, &type,");
            sb.AppendLine("                            (LPBYTE)guid, &size) == ERROR_SUCCESS) {");
            sb.AppendLine("            RegCloseKey(hKey);");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("        RegCloseKey(hKey);");
            sb.AppendLine("    }");
            sb.AppendLine("    return false;");
            sb.AppendLine("}");
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
            sb.AppendLine("                // 跳过虚拟网卡（VMware, VirtualBox等）");
            sb.AppendLine("                unsigned char vmMac1[3] = {0x00, 0x50, 0x56}; // VMware");
            sb.AppendLine("                unsigned char vmMac2[3] = {0x00, 0x0C, 0x29}; // VMware");
            sb.AppendLine("                unsigned char vmMac3[3] = {0x08, 0x00, 0x27}; // VirtualBox");
            sb.AppendLine("                bool isVirtual = false;");
            sb.AppendLine("                if (memcmp(pAdapter->Address, vmMac1, 3) == 0) isVirtual = true;");
            sb.AppendLine("                if (memcmp(pAdapter->Address, vmMac2, 3) == 0) isVirtual = true;");
            sb.AppendLine("                if (memcmp(pAdapter->Address, vmMac3, 3) == 0) isVirtual = true;");
            sb.AppendLine("                if (!isVirtual) {");
            sb.AppendLine("                    memcpy(mac, pAdapter->Address, 6);");
            sb.AppendLine("                    free(pAdapterInfo);");
            sb.AppendLine("                    return true;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            pAdapter = pAdapter->Next;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    free(pAdapterInfo);");
            sb.AppendLine("    return false;");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 收集所有硬件信息");
            sb.AppendLine("void CollectHardwareInfo(HardwareInfo* info)");
            sb.AppendLine("{");
            sb.AppendLine("    memset(info, 0, sizeof(HardwareInfo));");
            sb.AppendLine("    info->hasMac = GetMacAddress(info->mac);");
            sb.AppendLine("    GetCpuId(info->cpuId, sizeof(info->cpuId));");
            sb.AppendLine("    info->hasCpuId = strlen(info->cpuId) > 0;");
            sb.AppendLine("    info->hasVolumeSerial = GetVolumeSerial(info->volumeSerial, sizeof(info->volumeSerial));");
            sb.AppendLine("    info->hasMachineGuid = GetMachineGuid(info->machineGuid, sizeof(info->machineGuid));");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 计算硬件指纹（组合多个硬件标识）");
            sb.AppendLine("void GenerateMachineFingerprint(const HardwareInfo* info, char* fingerprint, int maxLen)");
            sb.AppendLine("{");
            sb.AppendLine("    // 使用多个硬件特征生成复合指纹");
            sb.AppendLine("    unsigned int hash = 0;");
            sb.AppendLine("    ");
            sb.AppendLine("    // CPU ID贡献");
            sb.AppendLine("    if (info->hasCpuId) {");
            sb.AppendLine("        for (int i = 0; info->cpuId[i]; i++) {");
            sb.AppendLine("            hash = hash * 31 + info->cpuId[i];");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 硬盘序列号贡献");
            sb.AppendLine("    if (info->hasVolumeSerial) {");
            sb.AppendLine("        for (int i = 0; info->volumeSerial[i]; i++) {");
            sb.AppendLine("            hash = hash * 17 + info->volumeSerial[i];");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // MAC地址贡献");
            sb.AppendLine("    if (info->hasMac) {");
            sb.AppendLine("        for (int i = 0; i < 6; i++) {");
            sb.AppendLine("            hash = hash * 13 + info->mac[i];");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 机器GUID贡献");
            sb.AppendLine("    if (info->hasMachineGuid) {");
            sb.AppendLine("        for (int i = 0; info->machineGuid[i] && i < 16; i++) {");
            sb.AppendLine("            hash = hash * 11 + info->machineGuid[i];");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    unsigned int hash2 = hash ^ 0xA5A5A5A5;");
            sb.AppendLine("    sprintf_s(fingerprint, maxLen, \"%08X%08X%08X%08X\", hash, hash2, hash ^ hash2, hash * 7 + hash2);");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 计算指纹相似度（容错匹配）");
            sb.AppendLine("int CalculateFingerprintSimilarity(const char* fp1, const char* fp2)");
            sb.AppendLine("{");
            sb.AppendLine("    int len1 = (int)strlen(fp1);");
            sb.AppendLine("    int len2 = (int)strlen(fp2);");
            sb.AppendLine("    int minLen = (len1 < len2) ? len1 : len2;");
            sb.AppendLine("    int matches = 0;");
            sb.AppendLine("    ");
            sb.AppendLine("    for (int i = 0; i < minLen; i++) {");
            sb.AppendLine("        if (fp1[i] == fp2[i]) matches++;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    return (matches * 100) / minLen;");
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
            sb.AppendLine("// 显示当前机器信息（用于授权申请）");
            sb.AppendLine("void ShowMachineInfo()");
            sb.AppendLine("{");
            sb.AppendLine("    HardwareInfo info;");
            sb.AppendLine("    CollectHardwareInfo(&info);");
            sb.AppendLine("    char fingerprint[64];");
            sb.AppendLine("    GenerateMachineFingerprint(&info, fingerprint, sizeof(fingerprint));");
            sb.AppendLine("    ");
            sb.AppendLine("    char msg[512];");
            sb.AppendLine("    sprintf_s(msg, sizeof(msg),");
            sb.AppendLine("        \"机器指纹：%s\\n\\n\"");
            sb.AppendLine("        \"硬件信息：\\n\"");
            sb.AppendLine("        \"  CPU ID: %s\\n\"");
            sb.AppendLine("        \"  硬盘序列: %s\\n\"");
            sb.AppendLine("        \"  MAC地址: %02X:%02X:%02X:%02X:%02X:%02X\\n\"");
            sb.AppendLine("        \"  机器GUID: %s\\n\\n\"");
            sb.AppendLine("        \"请将以上信息发送给开发者获取授权。\",");
            sb.AppendLine("        fingerprint,");
            sb.AppendLine("        info.hasCpuId ? info.cpuId : \"N/A\",");
            sb.AppendLine("        info.hasVolumeSerial ? info.volumeSerial : \"N/A\",");
            sb.AppendLine("        info.mac[0], info.mac[1], info.mac[2],");
            sb.AppendLine("        info.mac[3], info.mac[4], info.mac[5],");
            sb.AppendLine("        info.hasMachineGuid ? info.machineGuid : \"N/A\");");
            sb.AppendLine("    MessageBoxA(NULL, msg, \"设备信息\", MB_OK | MB_ICONINFORMATION);");
            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("// 检查设备授权 - 使用容错匹配");
            sb.AppendLine("bool CheckMachineID()");
            sb.AppendLine("{");
            sb.AppendLine("    HardwareInfo currentInfo;");
            sb.AppendLine("    CollectHardwareInfo(&currentInfo);");
            sb.AppendLine("    ");
            sb.AppendLine("    char currentFingerprint[64];");
            sb.AppendLine("    char authorizedFingerprint[64];");
            sb.AppendLine("    GenerateMachineFingerprint(&currentInfo, currentFingerprint, sizeof(currentFingerprint));");
            sb.AppendLine("    ");
            sb.AppendLine("    if (!LoadLicenseFingerprint(authorizedFingerprint, sizeof(authorizedFingerprint))) {");
            sb.AppendLine("        MessageBoxA(NULL, \"未找到授权文件 license.dat！\\n请先获取设备授权。\",");
            sb.AppendLine("                    \"授权失败\", MB_OK | MB_ICONERROR);");
            sb.AppendLine("        ShowMachineInfo();");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 完全匹配检查");
            sb.AppendLine("    if (strcmp(currentFingerprint, authorizedFingerprint) == 0) {");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 容错匹配（允许硬件部分变更，如换网卡）");
            sb.AppendLine("    int similarity = CalculateFingerprintSimilarity(currentFingerprint, authorizedFingerprint);");
            sb.AppendLine("    if (similarity >= 80) { // 80%匹配度通过");
            sb.AppendLine("        // 可选：记录警告日志");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine("    ");
            sb.AppendLine("    // 完全不匹配，显示详细信息");
            sb.AppendLine("    char msg[512];");
            sb.AppendLine("    sprintf_s(msg, sizeof(msg),");
            sb.AppendLine("        \"设备未授权！\\n\\n\"");
            sb.AppendLine("        \"当前指纹：%s\\n\"");
            sb.AppendLine("        \"授权指纹：%s\\n\"");
            sb.AppendLine("        \"匹配度：%d%%（需要80%%以上）\\n\\n\"");
            sb.AppendLine("        \"请联系开发者重新授权。\",");
            sb.AppendLine("        currentFingerprint, authorizedFingerprint, similarity);");
            sb.AppendLine("    MessageBoxA(NULL, msg, \"授权失败\", MB_OK | MB_ICONERROR);");
            sb.AppendLine("    ShowMachineInfo();");
            sb.AppendLine("    return false;");
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

        // DLL替换保护（代码完整性验证）
        sb.AppendLine("// DLL替换保护 - 代码完整性验证");
        sb.AppendLine("#include <winternl.h>");
        sb.AppendLine("#include <tlhelp32.h>");
        sb.AppendLine("#include <imagehlp.h>");
        sb.AppendLine("#include <wintrust.h>");
        sb.AppendLine("#include <softpub.h>");
        sb.AppendLine("#pragma comment(lib, \"imagehlp.lib\")");
        sb.AppendLine("#pragma comment(lib, \"ntdll.lib\")");
        sb.AppendLine("#pragma comment(lib, \"wintrust.lib\")");
        sb.AppendLine("");
        sb.AppendLine("// 简单的代码校验哈希（应在编译后更新）");
        sb.AppendLine("// 警告：这是一个示例实现，生产环境应使用更强的保护");
        sb.AppendLine("static const unsigned char g_CodeHash[32] = {");
        sb.AppendLine("    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,");
        sb.AppendLine("    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,");
        sb.AppendLine("    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,");
        sb.AppendLine("    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00");
        sb.AppendLine("}; // 编译后需要替换为实际的代码段哈希");
        sb.AppendLine("");
        sb.AppendLine("// 反调试检查");
        sb.AppendLine("bool AntiDebugCheck()");
        sb.AppendLine("{");
        sb.AppendLine("    // 1. 检查IsDebuggerPresent");
        sb.AppendLine("    if (IsDebuggerPresent())");
        sb.AppendLine("        return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    // 2. 检查NtGlobalFlag（调试器标志）");
        sb.AppendLine("    #ifdef _WIN64");
        sb.AppendLine("    PDWORD_PTR pNtGlobalFlag = (PDWORD_PTR)(__readgsqword(0x60) + 0xBC);");
        sb.AppendLine("    #else");
        sb.AppendLine("    PDWORD_PTR pNtGlobalFlag = (PDWORD_PTR)(__readfsdword(0x30) + 0x68);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    if (*pNtGlobalFlag & 0x70) // FLG_HEAP_ENABLE_TAIL_CHECK | FLG_HEAP_ENABLE_FREE_CHECK | FLG_HEAP_VALIDATE_PARAMETERS");
        sb.AppendLine("        return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    // 3. 检查Heap Flags");
        sb.AppendLine("    HANDLE hHeap = GetProcessHeap();");
        sb.AppendLine("    DWORD heapFlags = 0;");
        sb.AppendLine("    #ifdef _WIN64");
        sb.AppendLine("    if (HeapQueryInformation(hHeap, HeapCompatibilityInformation, &heapFlags, sizeof(heapFlags), NULL))");
        sb.AppendLine("    #else");
        sb.AppendLine("    PDWORD pHeapFlags = (PDWORD)((DWORD)hHeap + 0x0C);");
        sb.AppendLine("    PDWORD pForceFlags = (PDWORD)((DWORD)hHeap + 0x10);");
        sb.AppendLine("    if (*pHeapFlags || *pForceFlags)");
        sb.AppendLine("    #endif");
        sb.AppendLine("    {");
        sb.AppendLine("        #ifndef _WIN64");
        sb.AppendLine("        if (*pForceFlags) return false;");
        sb.AppendLine("        #endif");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    // 4. 检查调试寄存器（通过NtQueryInformationProcess）");
        sb.AppendLine("    typedef NTSTATUS (WINAPI *pfnNtQueryInformationProcess)(HANDLE, DWORD, PVOID, ULONG, PULONG);");
        sb.AppendLine("    pfnNtQueryInformationProcess NtQueryInfoProcess = (pfnNtQueryInformationProcess)GetProcAddress(");
        sb.AppendLine("        GetModuleHandleA(\"ntdll.dll\"), \"NtQueryInformationProcess\");");
        sb.AppendLine("    if (NtQueryInfoProcess)");
        sb.AppendLine("    {");
        sb.AppendLine("        DWORD debugPort = 0;");
        sb.AppendLine("        NTSTATUS status = NtQueryInfoProcess(GetCurrentProcess(), 7, &debugPort, sizeof(debugPort), NULL);");
        sb.AppendLine("        if (NT_SUCCESS(status) && debugPort != 0)");
        sb.AppendLine("            return false;");
        sb.AppendLine("        ");
        sb.AppendLine("        // 检查ProcessDebugFlags");
        sb.AppendLine("        DWORD debugFlags = 0;");
        sb.AppendLine("        status = NtQueryInfoProcess(GetCurrentProcess(), 0x1F, &debugFlags, sizeof(debugFlags), NULL);");
        sb.AppendLine("        if (NT_SUCCESS(status) && debugFlags == 0)");
        sb.AppendLine("            return false;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    // 5. 检查父进程是否为调试器");
        sb.AppendLine("    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);");
        sb.AppendLine("    if (hSnapshot != INVALID_HANDLE_VALUE)");
        sb.AppendLine("    {");
        sb.AppendLine("        PROCESSENTRY32 pe;");
        sb.AppendLine("        pe.dwSize = sizeof(pe);");
        sb.AppendLine("        DWORD pid = GetCurrentProcessId();");
        sb.AppendLine("        DWORD ppid = 0;");
        sb.AppendLine("        if (Process32First(hSnapshot, &pe))");
        sb.AppendLine("        {");
        sb.AppendLine("            do {");
        sb.AppendLine("                if (pe.th32ProcessID == pid)");
        sb.AppendLine("                {");
        sb.AppendLine("                    ppid = pe.th32ParentProcessID;");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("            } while (Process32Next(hSnapshot, &pe));");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        if (ppid)");
        sb.AppendLine("        {");
        sb.AppendLine("            HANDLE hParent = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, ppid);");
        sb.AppendLine("            if (hParent)");
        sb.AppendLine("            {");
        sb.AppendLine("                char parentName[MAX_PATH] = {0};");
        sb.AppendLine("                DWORD size = sizeof(parentName);");
        sb.AppendLine("                if (QueryFullProcessImageNameA(hParent, 0, parentName, &size))");
        sb.AppendLine("                {");
        sb.AppendLine("                char* p = strrchr(parentName, '\\\\');");
        sb.AppendLine("                    char* exeName = p ? p + 1 : parentName;");
        sb.AppendLine("                // 检查常见的调试器");
        sb.AppendLine("                    const char* debuggers[] = { \"vsjitdebugger.exe\", \"devenv.exe\", \"x64dbg.exe\", \"x32dbg.exe\", \"ollydbg.exe\", \"windbg.exe\", NULL };");
        sb.AppendLine("                    for (int i = 0; debuggers[i]; i++)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        if (_stricmp(exeName, debuggers[i]) == 0)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            CloseHandle(hParent);");
        sb.AppendLine("                            CloseHandle(hSnapshot);");
        sb.AppendLine("                            return false;");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("                CloseHandle(hParent);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        CloseHandle(hSnapshot);");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    // 6. 检查远程调试（通过CheckRemoteDebuggerPresent）");
        sb.AppendLine("    BOOL bRemoteDebugger = FALSE;");
        sb.AppendLine("    if (CheckRemoteDebuggerPresent(GetCurrentProcess(), &bRemoteDebugger) && bRemoteDebugger)");
        sb.AppendLine("        return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    return true;");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("// 代码段完整性检查");
        sb.AppendLine("bool CheckCodeIntegrity()");
        sb.AppendLine("{");
        sb.AppendLine("    // 获取当前模块");
        sb.AppendLine("    HMODULE hModule = GetModuleHandleA(NULL);");
        sb.AppendLine("    if (!hModule) return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    // 获取PE头信息");
        sb.AppendLine("    PIMAGE_DOS_HEADER pDosHeader = (PIMAGE_DOS_HEADER)hModule;");
        sb.AppendLine("    if (pDosHeader->e_magic != IMAGE_DOS_SIGNATURE) return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    PIMAGE_NT_HEADERS pNtHeaders = (PIMAGE_NT_HEADERS)((BYTE*)hModule + pDosHeader->e_lfanew);");
        sb.AppendLine("    if (pNtHeaders->Signature != IMAGE_NT_SIGNATURE) return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    // 计算代码段哈希（.text段）");
        sb.AppendLine("    PIMAGE_SECTION_HEADER pSection = IMAGE_FIRST_SECTION(pNtHeaders);");
        sb.AppendLine("    DWORD sectionCount = pNtHeaders->FileHeader.NumberOfSections;");
        sb.AppendLine("    ");
        sb.AppendLine("    for (WORD i = 0; i < sectionCount; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (memcmp(pSection[i].Name, \".text\", 5) == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            BYTE* codeBase = (BYTE*)hModule + pSection[i].VirtualAddress;");
        sb.AppendLine("            DWORD codeSize = pSection[i].Misc.VirtualSize;");
        sb.AppendLine("            ");
        sb.AppendLine("            // 简单的校验和计算");
        sb.AppendLine("            unsigned long checksum = 0;");
        sb.AppendLine("            // 跳过前4KB（包含导入表等易变内容）");
        sb.AppendLine("            DWORD startOffset = (codeSize > 4096) ? 4096 : 0;");
        sb.AppendLine("            for (DWORD j = startOffset; j < codeSize; j += 4)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (j + 4 <= codeSize)");
        sb.AppendLine("                    checksum ^= *(unsigned long*)(codeBase + j);");
        sb.AppendLine("            }");
        sb.AppendLine("            ");
        sb.AppendLine("            // 检查是否在允许的范围内（允许小的差异）");
        sb.AppendLine("            // 注意：这是一个示例，实际应使用预计算的哈希");
        sb.AppendLine("            static unsigned long expectedChecksum = 0;");
        sb.AppendLine("            if (expectedChecksum == 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                // 首次运行记录校验和");
        sb.AppendLine("                expectedChecksum = checksum;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (abs((long)(checksum - expectedChecksum)) > 0x1000)");
        sb.AppendLine("            {");
        sb.AppendLine("                // 差异过大，可能代码被修改");
        sb.AppendLine("                return false;");
        sb.AppendLine("            }");
        sb.AppendLine("            break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    return true;");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("// 检查DLL是否被篡改或替换");
        sb.AppendLine("bool CheckDllIntegrity()");
        sb.AppendLine("{");
        sb.AppendLine("    // 1. 检查文件数字签名（如果已签名）");
        sb.AppendLine("    wchar_t dllPath[MAX_PATH];");
        sb.AppendLine("    if (GetModuleFileNameW(NULL, dllPath, MAX_PATH))");
        sb.AppendLine("    {");
        sb.AppendLine("        WINTRUST_FILE_INFO fileInfo = {0};");
        sb.AppendLine("        fileInfo.cbStruct = sizeof(fileInfo);");
        sb.AppendLine("        fileInfo.pcwszFilePath = dllPath;");
        sb.AppendLine("        fileInfo.hFile = NULL;");
        sb.AppendLine("        fileInfo.pgKnownSubject = NULL;");
        sb.AppendLine("        ");
        sb.AppendLine("        GUID action = WINTRUST_ACTION_GENERIC_VERIFY_V2;");
        sb.AppendLine("        WINTRUST_DATA data = {0};");
        sb.AppendLine("        data.cbStruct = sizeof(data);");
        sb.AppendLine("        data.pPolicyCallbackData = NULL;");
        sb.AppendLine("        data.pSIPClientData = NULL;");
        sb.AppendLine("        data.dwUIChoice = WTD_UI_NONE;");
        sb.AppendLine("        data.fRevocationChecks = WTD_REVOKE_NONE;");
        sb.AppendLine("        data.dwUnionChoice = WTD_CHOICE_FILE;");
        sb.AppendLine("        data.pFile = &fileInfo;");
        sb.AppendLine("        data.dwStateAction = WTD_STATEACTION_VERIFY;");
        sb.AppendLine("        data.hWVTStateData = NULL;");
        sb.AppendLine("        data.pwszURLReference = NULL;");
        sb.AppendLine("        data.dwProvFlags = WTD_SAFER_FLAG;");
        sb.AppendLine("        ");
        sb.AppendLine("        LONG result = WinVerifyTrust((HWND)INVALID_HANDLE_VALUE, &action, &data);");
        sb.AppendLine("        ");
        sb.AppendLine("        data.dwStateAction = WTD_STATEACTION_CLOSE;");
        sb.AppendLine("        WinVerifyTrust(NULL, &action, &data);");
        sb.AppendLine("        ");
        sb.AppendLine("        // 如果签名验证失败但不是因为无签名，可能是篡改");
        sb.AppendLine("        if (result != ERROR_SUCCESS && result != TRUST_E_NOSIGNATURE)");
        sb.AppendLine("        {");
        sb.AppendLine("            // 签名无效或被篡改");
        sb.AppendLine("            // 可选：记录日志");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    // 2. 检查模块加载路径（防止DLL劫持）");
        sb.AppendLine("    char exePath[MAX_PATH], dllPath[MAX_PATH];");
        sb.AppendLine("    GetModuleFileNameA(NULL, exePath, sizeof(exePath));");
        sb.AppendLine("    GetModuleFileNameA(GetModuleHandleA(NULL), dllPath, sizeof(dllPath));");
        sb.AppendLine("    ");
        sb.AppendLine("    // 3. 检查关键导入表是否被篡改");
        sb.AppendLine("    // 验证kernel32, ntdll等关键DLL的基址");
        sb.AppendLine("    HMODULE hKernel32 = GetModuleHandleA(\"kernel32.dll\");");
        sb.AppendLine("    HMODULE hNtdll = GetModuleHandleA(\"ntdll.dll\");");
        sb.AppendLine("    if (!hKernel32 || !hNtdll)");
        sb.AppendLine("        return false; // 关键DLL缺失，可能有问题");
        sb.AppendLine("    ");
        sb.AppendLine("    // 4. 检查是否在被调试或内存被修改");
        sb.AppendLine("    if (!AntiDebugCheck())");
        sb.AppendLine("        return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    // 5. 代码完整性检查");
        sb.AppendLine("    if (!CheckCodeIntegrity())");
        sb.AppendLine("        return false;");
        sb.AppendLine("    ");
        sb.AppendLine("    return true;");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("// 初始化时执行完整性检查");
        sb.AppendLine("bool InitializeSecurity()");
        sb.AppendLine("{");
        sb.AppendLine("    // 执行DLL完整性检查");
        sb.AppendLine("    if (!CheckDllIntegrity())");
        sb.AppendLine("    {");
        sb.AppendLine("        // 不返回false，而是保持g_securityPass=0让算法输出错误结果");
        sb.AppendLine("        // 这样攻击者无法通过一字节patch绕过验证");
        sb.AppendLine("        return true; // 返回true让DLL加载，但算法会输出错误值");
        sb.AppendLine("    }");
        sb.AppendLine("    // 所有检查通过，设置安全标志");
        sb.AppendLine("    g_securityPass = 1;");
        sb.AppendLine("    return true;");
        sb.AppendLine("}");
        sb.AppendLine("");

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
                // 应用安全腐蚀
                sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
                sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
                sb.AppendLine("    {");
                sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
                sb.AppendLine("    }");
                break;
            case "DATALOADER":
                GenerateDataLoaderCode(sb, func);
                break;
            case "EXPRESSION":
                GenerateExpressionCode(sb, func);
                break;
            default:
                sb.AppendLine("    // TODO: 实现算法");
                sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
                sb.AppendLine("    {");
                sb.AppendLine("        pfOut[i] = pfIn1[i];");
                sb.AppendLine("    }");
                // 应用安全腐蚀
                sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
                sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
                sb.AppendLine("    {");
                sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
                sb.AppendLine("    }");
                break;
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成数据加载函数代码（DATALOADER类型）
    /// 用于将通达信传入的数据存储到全局缓冲区
    /// </summary>
    private void GenerateDataLoaderCode(StringBuilder sb, FunctionConfig func)
    {
        if (!string.IsNullOrEmpty(func.CustomCode))
        {
            sb.AppendLine(func.CustomCode);
        }
        else
        {
            // 默认数据加载逻辑：pfIn1→g_buf_close, pfIn2→g_buf_open, pfIn3→g_buf_high
            sb.AppendLine("    // 数据加载到全局缓冲区");
            sb.AppendLine("    if (nDataLen <= 0 || pfIn1 == nullptr) return;");
            sb.AppendLine("    g_buf_close.assign(pfIn1, pfIn1 + nDataLen);");
            sb.AppendLine("    if (pfIn2) g_buf_open.assign(pfIn2, pfIn2 + nDataLen);");
            sb.AppendLine("    else g_buf_open.assign(nDataLen, 0.0f);");
            sb.AppendLine("    if (pfIn3) g_buf_high.assign(pfIn3, pfIn3 + nDataLen);");
            sb.AppendLine("    else g_buf_high.assign(nDataLen, 0.0f);");
            sb.AppendLine("    // 输出0（此函数仅加载数据，不产生有效输出）");
            sb.AppendLine("    for (int i = 0; i < nDataLen; i++) pfOut[i] = 0.0f;");
        }
    }

    /// <summary>
    /// 生成表达式代码
    /// </summary>
    private void GenerateExpressionCode(StringBuilder sb, FunctionConfig func)
    {
        if (string.IsNullOrWhiteSpace(func.Expression))
        {
            sb.AppendLine("    // 错误: 表达式为空");
            sb.AppendLine("    for (int i = 0; i < nDataLen; i++) pfOut[i] = 0;");
            return;
        }

        try
        {
            // 预处理表达式（移除注释、绘图函数、颜色常量）
            var preprocessed = ExpressionParser.Preprocess(func.Expression);
            
            // 解析表达式
            var parser = new ExpressionParser();
            var ast = parser.Parse(preprocessed);

            // 生成代码
            var generator = new ExpressionCodeGenerator(func.Name, func.UseGlobalData);
            var code = generator.GenerateFunctionCode(ast);

            // 输出代码
            sb.AppendLine(code);
        }
        catch (ExpressionParseException ex)
        {
            sb.AppendLine($"    // 表达式解析错误: {ex.Message.Replace("\n", " ").Replace("\r", "")}");
            sb.AppendLine("    for (int i = 0; i < nDataLen; i++) pfOut[i] = 0;");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"    // 代码生成错误: {ex.Message.Replace("\n", " ").Replace("\r", "")}");
            sb.AppendLine("    for (int i = 0; i < nDataLen; i++) pfOut[i] = 0;");
        }
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
        // 应用安全腐蚀
        sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
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
        // 应用安全腐蚀
        sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
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
        // 应用安全腐蚀
        sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
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
        // 应用安全腐蚀
        sb.AppendLine("    // 安全腐蚀: 验证失败时输出错误结果");
        sb.AppendLine("    for (int i = 0; i < nDataLen; i++)");
        sb.AppendLine("    {");
        sb.AppendLine("        pfOut[i] = ObfuscateResult(pfOut[i]);");
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
        
        // 添加设备绑定所需的链接库
        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("# 设备绑定功能所需的系统库");
            sb.AppendLine("target_link_libraries(${PROJECT_NAME} PRIVATE iphlpapi ws2_32 imagehlp wintrust)");
            sb.AppendLine();
        }
        
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
        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("      <AdditionalDependencies>iphlpapi.lib;ws2_32.lib;imagehlp.lib;ntdll.lib;wintrust.lib;%(AdditionalDependencies)</AdditionalDependencies>");
        }
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
        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("      <AdditionalDependencies>iphlpapi.lib;ws2_32.lib;imagehlp.lib;ntdll.lib;wintrust.lib;%(AdditionalDependencies)</AdditionalDependencies>");
        }
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
        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("      <AdditionalDependencies>iphlpapi.lib;ws2_32.lib;imagehlp.lib;ntdll.lib;wintrust.lib;%(AdditionalDependencies)</AdditionalDependencies>");
        }
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
        if (_config.Security.EnableDeviceBinding)
        {
            sb.AppendLine("      <AdditionalDependencies>iphlpapi.lib;ws2_32.lib;imagehlp.lib;ntdll.lib;wintrust.lib;%(AdditionalDependencies)</AdditionalDependencies>");
        }
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
