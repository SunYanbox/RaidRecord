using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;

namespace RaidRecord;

public record ModConfigData
{
    // 本地语言
    [JsonPropertyName("local")]
    public required string Local { get; set; }
    // 清除程序数据
    [JsonPropertyName("clearData")]
    public required bool ClearData { get; set; }
    // 清除程序缓存
    [JsonPropertyName("clearCaches")]
    public required bool ClearCaches { get; set; }
    // 模组日志位置
    [JsonPropertyName("logPath")]
    public required string LogPath { get; set; }
}

// [Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class ModConfig(ModHelper modHelper, 
    ISptLogger<ModConfig> logger): IOnLoad
{
    public required ModConfigData Configs;
    public required StreamWriter? LogFile;
    
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // logger.Info($"pathToMod: {pathToMod}");
        Configs = modHelper.GetJsonDataFromFile<ModConfigData>(pathToMod, "db\\config.json");
        var logPath = Path.Combine(pathToMod, Configs.LogPath);
        try
        {
            LogFile = new StreamWriter(logPath);
        }
        catch (Exception ex)
        {
            LogFile = null;
            logger.Error($"由于{ex.Message}, 无法获取模组日志流");
        }
        
        // logger.Info($"读取到的配置: {jsonUtil.Serialize(_configs)}");
        return Task.CompletedTask;
    }

    public void Log(string mode, string message)
    {
        if (LogFile == null)
        {
            
        }
        else
        {
            LogFile.WriteLine($"[{mode}] {message}");
        }
    }
}