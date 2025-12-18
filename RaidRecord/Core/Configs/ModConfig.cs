using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;

namespace RaidRecord.Core.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class ModConfig(ModHelper modHelper,
    ISptLogger<ModConfig> logger): IOnLoad
{
    public required ModConfigData Configs;
    public required StreamWriter? LogFile;
    private readonly Lock _logLock = new();


    public Task OnLoad()
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // logger.Info($"pathToMod: {pathToMod}");
        Configs = modHelper.GetJsonDataFromFile<ModConfigData>(pathToMod, Path.Combine("db", "config.json"));
        string logPath = Path.Combine(pathToMod, Configs.LogPath);
        try
        {
            lock (_logLock)
            {
                LogFile = new StreamWriter(logPath);
            }
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
            lock (_logLock)
            {
                using var sw = new StreamWriter(LogFile.BaseStream, LogFile.Encoding, 1024, true);
                sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mode} - {message}");
                // using 语句结束时自动调用 Dispose 和 Flush
            }
        }
    }
    
    public void Info(string message)
    {
        Log("Info", message);
        logger.Info($"[RaidRecord] {message}");
    }
    
    public void Debug(string message)
    {
        Log("Debug", message);
        logger.Debug($"[RaidRecord] {message}");
    }
    
    public void Error(string message)
    {
        Log("Error", message);
        logger.Error($"[RaidRecord] {message}");
    }

    public void LogError(Exception e, string where, string? message = null)
    {
        Log("Error", $"where: {where}\nmessage: {e.Message}\nstack: {e.StackTrace}\nLogMessage: {message}");
    }
}