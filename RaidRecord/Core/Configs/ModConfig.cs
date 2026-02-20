using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SuntionCore.Services.LogUtils;

namespace RaidRecord.Core.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// 提供配置管理与本地日志
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ModConfig(ModHelper modHelper,
    ISptLogger<ModConfig> sptLogger,
    JsonUtil jsonUtil,
    ModMetadata modMetadata): IOnLoad
{
    public required ModConfigData Configs;
    private string? _configPath;

    /// <summary>
    /// 本模组元数据
    /// </summary>
    public readonly ModMetadata Metadata = modMetadata;
    
    public readonly ModLogger Logger = ModLogger.GetOrCreateLogger("RaidRecord");

    /// <summary> 保存配置 </summary>
    public async Task SaveConfig()
    {
        _configPath ??= Constants.DBConfigPath(modHelper: modHelper);
        if (Path.Exists(_configPath))
        {
            string? serialize = jsonUtil.Serialize(Configs);
            if (serialize != null)
            {
                await File.WriteAllTextAsync(_configPath, serialize);
            }
        }
    }

    public Task OnLoad()
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // sptLogger.Info($"pathToMod: {pathToMod}");
        Configs = modHelper.GetJsonDataFromFile<ModConfigData>(pathToMod, Path.Combine("db", "config.json"));

        // sptLogger.Info($"读取到的配置: {jsonUtil.Serialize(_configs)}");
        return Task.CompletedTask;
    }

    public void Info(string message, bool enableSPTLog = true)
    {
        string msg = Logger.Info(message);
        if (enableSPTLog) sptLogger.Info(msg);
    }

    public void Debug(string message, bool enableSPTLog = true)
    {
        string msg = Logger.Debug(message);
        if (enableSPTLog) sptLogger.Debug(msg);
    }

    public void Warn(string message, bool enableSPTLog = true)
    {
        string msg = Logger.Warn(message);
        if (enableSPTLog) sptLogger.Warning(msg);
    }

    public void Error(string message, Exception? ex = null, bool enableSPTLog = true)
    {
        string msg = Logger.Error(message);
        if (enableSPTLog) sptLogger.Error(msg);
    }

    public void LogError(Exception e, string where, string? message = null)
    {
        Logger.Error($"where: {where}\nmessage: {e.GetType().Name}({e.Message})\nstack: {e.StackTrace}\nLogMessage: {message}\n", e);
    }
}