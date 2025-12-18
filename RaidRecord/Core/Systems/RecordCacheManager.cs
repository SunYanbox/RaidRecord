using System.Reflection;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Systems;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public class RecordCacheManager(
    ISptLogger<RecordCacheManager> logger,
    LocalizationManager localManager,
    JsonUtil jsonUtil,
    ModConfig modConfig,
    ModHelper modHelper
): IOnLoad
{
    private string? _recordDbPath;
    private readonly Dictionary<MongoId, List<RaidDataWrapper>> _raidRecordCache = new();

    public void Info(string message)
    {
        modConfig.Log("Info", message);
        logger.Info($"[RaidRecord] {message}");
    }
    public void Error(string message)
    {
        modConfig.Log("Error", message);
        logger.Error($"[RaidRecord] {message}");
    }

    public Task OnLoad()
    {

        string localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _recordDbPath = Path.Combine(localsDir, "db\\records");

        if (!Directory.Exists(_recordDbPath)) Directory.CreateDirectory(_recordDbPath);

        foreach (string file in Directory.GetFiles(_recordDbPath))
        {
            string fileName = Path.GetFileName(file);
            if (!fileName.EndsWith(".json")) continue;
            try
            {
                var data = jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(file);
                if (data == null) throw new Exception($"反序列化文件{file}时获取不到数据");
                _raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), data);
            }
            catch (Exception e)
            {
                modConfig.LogError(e, "RaidRecordManager.OnLoad.foreach.try-catch", file);
                // 备份原文件为 .err，带序号避免重复
                string originalFilePath = Path.Combine(_recordDbPath, fileName);
                string backupBaseName = Path.GetFileNameWithoutExtension(fileName) + ".json.err";
                string backupDir = _recordDbPath; // 备份在同一目录下，也可指定其他路径
                string backupPath = Path.Combine(backupDir, backupBaseName);

                int counter = 0;
                while (File.Exists(backupPath))
                {
                    backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(fileName)}.json.err.{counter}");
                    counter++;
                }

                try
                {
                    File.Copy(originalFilePath, backupPath);
                    Info($"序列化记录时出现问题: {e.Message}, 已备份损坏文件至: {backupPath}");
                    // Console.WriteLine($"[RaidRecord] DEBUG: {e.StackTrace}");
                }
                catch (Exception copyEx)
                {
                    modConfig.LogError(e, "RaidRecordManager.OnLoad.foreach.try-catch.try-catch", file);
                    Error($"备份文件过程中发生错误: {copyEx.Message}");
                }
                //
                // Console.WriteLine($"[RaidRecord] 记录文件{fileName}的Json格式错误");

                _raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), []);
            }

        }
        return Task.CompletedTask;
    }

    public void SaveRecord(MongoId playerId)
    {
        if (!_raidRecordCache.ContainsKey(playerId))
        {
            Create(playerId);
        }

        string jsonString = jsonUtil.Serialize(_raidRecordCache[playerId]) ?? "[]";

        if (_recordDbPath == null)
        {
            Error("保存记录数据库时数据库文件路径意外不存在, 请确保`db\\records`文件夹路径存在");
            return;
        }
        File.WriteAllTextAsync(Path.Combine(_recordDbPath, $"{playerId}.json"), jsonString);
    }

    public List<RaidDataWrapper> GetRecord(MongoId playerId)
    {
        if (!_raidRecordCache.ContainsKey(playerId))
        {
            if (_recordDbPath == null)
            {
                Error("记录数据库文件路径未正确获取, 请确保`db\\records`文件夹路径存在");
                return [];
            }

            if (Path.Exists(Path.Combine(_recordDbPath, $"{playerId}.json")))
            {
                var raidDataWrappers = jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(Path.Combine(_recordDbPath, $"{playerId.ToString()}.json"));
                if (raidDataWrappers == null)
                {
                    logger.Warning($"RecordCacheManager.GetRecord > 玩家{playerId}的记录文件{playerId}.json反序列化失败");
                }
                else
                {
                    _raidRecordCache.Add(playerId, raidDataWrappers);
                }
            }
            else
            {
                _raidRecordCache.Add(playerId, []);
            }
        }
        List<RaidDataWrapper> records = _raidRecordCache[playerId];
        // Console.WriteLine($"DEBUG RecordCacheManager.GetRecord > 玩家{playerId}的记录有{records.Count}条");
        return records;
    }

    public void Create(MongoId playerId)
    {
        if (!_raidRecordCache.ContainsKey(playerId))
        {
            _raidRecordCache.Add(playerId, []);
        }
        SaveRecord(playerId);
    }

    public RaidDataWrapper CreateRecord(MongoId playerId)
    {
        try
        {
            if (playerId == null!)
            {
                Error($"RecordCacheManager.CreateRecord > 玩家playerId为null");
                throw new Exception($"{nameof(playerId)} is null");
            }
            List<RaidDataWrapper> records = GetRecord(playerId);
            // Console.WriteLine($"DEBUG RecordCacheManager.CreateRecord > 玩家{playerId}的记录records为{records}, {records?.Count}条");
            // 检查 records 是否为 null
            if (records == null)
            {
                throw new Exception($"GetRecord中获取的records为null playId: {playerId}");
            }
            RaidDataWrapper wrapper = new();
            records.Add(wrapper);
            wrapper.Info = new RaidInfo();
            // Console.WriteLine($"DEBUG RecordCacheManager.CreateRecord > 返回值: {wrapper}, Info: {wrapper.Info}, Archive:  {wrapper.Archive}");
            return wrapper;
        }
        catch (Exception e)
        {
            Console.WriteLine($"RecordCacheManager.CreateRecord: {e.Message}\nstack: {e.StackTrace}");
            modConfig.LogError(e, "RaidRecordManager.CreateRecord.try-catch", "创建记录实例时出错");
            throw;
        }
    }

    public void Delete(MongoId playerId)
    {
        if (_raidRecordCache.Remove(playerId))
            SaveRecord(playerId);
    }

    public void ZipAll(ItemHelper itemHelper)
    {
        foreach (MongoId playerId in _raidRecordCache.Keys)
        {
            ZipAll(itemHelper, playerId);
        }
    }

    public void ZipAll(ItemHelper itemHelper, MongoId playerId)
    {
        foreach (RaidDataWrapper raidDataWrapper in _raidRecordCache.GetValueOrDefault(playerId, []))
        {
            raidDataWrapper.Zip(itemHelper);
        }
    }
}