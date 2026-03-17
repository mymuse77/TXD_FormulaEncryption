using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TdxDllGenerator;

/// <summary>
/// 授权文件生成工具
/// 用于开发者根据用户机器指纹生成授权文件
/// </summary>
public class LicenseGenerator
{
    /// <summary>
    /// 生成授权文件
    /// </summary>
    /// <param name="machineFingerprint">机器指纹</param>
    /// <param name="userName">用户名</param>
    /// <param name="email">邮箱</param>
    /// <param name="expireDate">过期日期</param>
    /// <param name="outputPath">输出路径</param>
    public static void GenerateLicense(
        string machineFingerprint,
        string userName,
        string email,
        DateTime expireDate,
        string outputPath)
    {
        var license = new LicenseInfo
        {
            Version = "1.0",
            UserName = userName,
            Email = email,
            MachineFingerprint = machineFingerprint,
            IssueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            ExpireDate = expireDate.ToString("yyyy-MM-dd"),
            MaxDevices = 1,
            Features = new[] { "MA", "EMA", "RSI", "MACD", "CUSTOM" }
        };

        // 序列化为JSON
        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // 添加简单校验（Base64编码）
        var hash = ComputeHash(json);
        var licenseData = new LicenseFile
        {
            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
            Signature = hash
        };

        // 写入文件
        var output = JsonSerializer.Serialize(licenseData, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        File.WriteAllText(outputPath, output);
        Console.WriteLine($"授权文件已生成: {outputPath}");
        Console.WriteLine($"授权用户: {userName}");
        Console.WriteLine($"机器指纹: {machineFingerprint}");
        Console.WriteLine($"过期日期: {expireDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// 生成多设备授权
    /// </summary>
    public static void GenerateMultiDeviceLicense(
        string[] machineFingerprints,
        string userName,
        string email,
        DateTime expireDate,
        string outputPath)
    {
        var license = new MultiDeviceLicenseInfo
        {
            Version = "1.0",
            UserName = userName,
            Email = email,
            MachineFingerprints = machineFingerprints,
            IssueDate = DateTime.Now.ToString("yyyy-MM-dd"),
            ExpireDate = expireDate.ToString("yyyy-MM-dd"),
            MaxDevices = machineFingerprints.Length,
            Features = new[] { "MA", "EMA", "RSI", "MACD", "CUSTOM" }
        };

        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var hash = ComputeHash(json);
        var licenseData = new LicenseFile
        {
            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
            Signature = hash
        };

        var output = JsonSerializer.Serialize(licenseData);
        File.WriteAllText(outputPath, output);

        Console.WriteLine($"多设备授权文件已生成: {outputPath}");
        Console.WriteLine($"授权用户: {userName}");
        Console.WriteLine($"授权设备数: {machineFingerprints.Length}");
        Console.WriteLine($"过期日期: {expireDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// 验证授权文件
    /// </summary>
    public static bool ValidateLicense(string licensePath, string machineFingerprint)
    {
        try
        {
            var content = File.ReadAllText(licensePath);
            var licenseData = JsonSerializer.Deserialize<LicenseFile>(content);

            if (licenseData == null) return false;

            // 解码数据
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(licenseData.Data));

            // 验证签名
            var computedHash = ComputeHash(json);
            if (computedHash != licenseData.Signature) return false;

            // 解析授权信息
            var license = JsonSerializer.Deserialize<LicenseInfo>(json);
            if (license == null) return false;

            // 检查过期日期
            if (DateTime.TryParse(license.ExpireDate, out var expireDate))
            {
                if (expireDate < DateTime.Now)
                {
                    Console.WriteLine("授权已过期");
                    return false;
                }
            }

            // 验证机器指纹
            if (license.MachineFingerprint != machineFingerprint)
            {
                Console.WriteLine("机器指纹不匹配");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证授权失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 计算简单哈希
    /// </summary>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    // 授权信息类
    private class LicenseInfo
    {
        public string Version { get; set; } = "1.0";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string MachineFingerprint { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string ExpireDate { get; set; } = "";
        public int MaxDevices { get; set; } = 1;
        public string[] Features { get; set; } = Array.Empty<string>();
    }

    private class MultiDeviceLicenseInfo
    {
        public string Version { get; set; } = "1.0";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string[] MachineFingerprints { get; set; } = Array.Empty<string>();
        public string IssueDate { get; set; } = "";
        public string ExpireDate { get; set; } = "";
        public int MaxDevices { get; set; } = 1;
        public string[] Features { get; set; } = Array.Empty<string>();
    }

    private class LicenseFile
    {
        public string Data { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}
