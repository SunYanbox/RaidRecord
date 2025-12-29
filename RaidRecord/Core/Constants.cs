using System.Reflection;
using SPTarkov.Server.Core.Helpers;

namespace RaidRecord.Core;

/// <summary>
/// 常量配置
/// </summary>
public static class Constants
{
    /// <summary> 允许的数据误差范围 </summary>
    public const double Epsilon = 1e-9;

    /// <summary> 发送信息单条长度限制 </summary>
    public const int SendLimit = 491;

    /// <summary>
    /// 获取数据库子文件夹路径（如 records 或 locals）
    /// </summary>
    /// <param name="subFolderName">子文件夹名称，例如 "records" 或 "locals"</param>
    /// <param name="modPath">模组路径</param>
    /// <param name="modHelper">模组路径为空时的备选</param>
    /// <returns>db//{subFolderName} 的绝对路径</returns>
    /// <exception cref="ArgumentException">参数为空或者路径不存在</exception>
    private static string GetDBSubFolderPath(string subFolderName, string? modPath = null, ModHelper? modHelper = null)
    {
        if (string.IsNullOrEmpty(subFolderName))
            throw new ArgumentException("subFolderName 不能为空", nameof(subFolderName));

        if (modPath == null && modHelper == null)
            throw new ArgumentException("modPath 或 modHelper 不能同时为 null");

        string dbSubPath = Path.Combine("db", subFolderName);

        if (modPath != null && Path.Exists(modPath))
            return Path.Combine(modPath, dbSubPath);

        if (modHelper != null)
            return Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), dbSubPath);

        throw new ArgumentException($"无法获取到合适的数据库路径：{subFolderName}");
    }

    /// <summary>
    /// 获取数据库记录文件夹路径
    /// </summary>
    /// <param name="modPath">模组路径</param>
    /// <param name="modHelper">模组路径为空时的备选</param>
    /// <returns>db//records 的绝对路径</returns>
    /// <exception cref="ArgumentException">参数为空或者路径不存在</exception>
    public static string DBRecordsFolderPath(string? modPath = null, ModHelper? modHelper = null) => GetDBSubFolderPath("records", modPath, modHelper);

    /// <summary>
    /// 获取本地化数据库文件夹路径
    /// </summary>
    /// <param name="modPath">模组路径</param>
    /// <param name="modHelper">模组路径为空时的备选</param>
    /// <returns>db//locals 的绝对路径</returns>
    /// <exception cref="ArgumentException">参数为空或者路径不存在</exception>
    public static string DBLocalsFolderPath(string? modPath = null, ModHelper? modHelper = null) => GetDBSubFolderPath("locals", modPath, modHelper);
    
    /// <summary>
    /// 获取数据库配置文件路径
    /// </summary>
    /// <param name="modPath">模组路径</param>
    /// <param name="modHelper">模组路径为空时的备选</param>
    /// <returns>db//config.json 的绝对路径</returns>
    /// <exception cref="ArgumentException">参数为空或者路径不存在</exception>
    public static string DBConfigPath(string? modPath = null, ModHelper? modHelper = null) => GetDBSubFolderPath("config.json", modPath, modHelper);
}