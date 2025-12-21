using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;

namespace RaidRecord.Core.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ModConfig(ModHelper modHelper,
    ISptLogger<ModConfig> logger,
    ModMetadata modMetadata): IOnLoad
{
    public required ModConfigData Configs;
    private StreamWriter? _logFile;

    /// <summary>
    /// 本模组元数据
    /// </summary>
    public readonly ModMetadata Metadata = modMetadata;

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
                _logFile = new StreamWriter(logPath);
            }
        }
        catch (Exception ex)
        {
            _logFile = null;
            logger.Error($"由于{ex.Message}, 无法获取模组日志流");
        }

        // logger.Info($"读取到的配置: {jsonUtil.Serialize(_configs)}");
        return Task.CompletedTask;
    }

    public void Log(string mode, string message)
    {
        if (_logFile == null)
        {

        }
        else
        {
            lock (_logLock)
            {
                using var sw = new StreamWriter(_logFile.BaseStream, _logFile.Encoding, 1024, true);
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

    public void Warn(string message)
    {
        Log("Warn", message);
        logger.Warning($"[RaidRecord] {message}");
    }

    public void Error(string message, Exception? ex = null)
    {
        string logMessage = message;
        if (ex != null)
        {
            // 可以控制堆栈信息的详细程度
            logMessage += $"\n\tException Type: {ex.GetType().Name}";
            logMessage += $"\n\tMessage: {ex.Message}";
            logMessage += $"\n\tStack Trace: {ex.StackTrace}";

            // 如果有内部异常
            if (ex.InnerException != null)
            {
                logMessage += $"\n\tInner Exception: {ex.InnerException.Message}";
            }
        }
        else
        {
            logMessage += $"\n\tStack Trace: {Environment.StackTrace}";
        }
        Log("Error", logMessage);
        logger.Error($"[RaidRecord] {logMessage}");
    }

    public void LogError(Exception e, string where, string? message = null)
    {
        Log("Error", $"where: {where}\nmessage: {e.GetType().Name}({e.Message})\nstack: {e.StackTrace}\nLogMessage: {message}\n");
    }
}