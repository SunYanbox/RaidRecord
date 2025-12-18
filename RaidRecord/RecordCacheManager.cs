using System.Reflection;
using System.Text.Json.Serialization;
using RaidRecord.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord;

public class RaidDataWrapper
{
    // 缓存
    [JsonPropertyName("info")]
    public RaidInfo? Info { get; set; }
    // 存档
    [JsonPropertyName("archive")]
    public RaidArchive? Archive { get; set; }
    
    public bool IsInfo => Info != null;
    public bool IsArchive => Archive != null;

    public void Zip(ItemHelper itemHelper)
    {
        if (IsInfo)
        {
            Archive = new RaidArchive();
            Archive.Zip(Info, itemHelper);
            Info = null;
        }
    }
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class RecordCacheManager(
    ISptLogger<RecordCacheManager>  logger,
    JsonUtil jsonUtil,
    ModHelper modHelper
    ): IOnLoad
{
    private string? _RecordDbPath;
    private Dictionary<MongoId, List<RaidDataWrapper>> raidRecordCache = new Dictionary<MongoId, List<RaidDataWrapper>>();
    
    public void Info(string message) { logger.Info($"[RaidRecord] {message}"); }
    public void Error(string message) { logger.Error($"[RaidRecord] 错误: {message}"); }
     
    public Task OnLoad()
    {
        
        var localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _RecordDbPath = Path.Combine(localsDir, "db\\records");

        if (!Directory.Exists(_RecordDbPath)) Directory.CreateDirectory(_RecordDbPath);
        
        foreach (var file in Directory.GetFiles(_RecordDbPath))
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.EndsWith(".json")) continue;
            try
            {
                var data = jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(file);
                if (data == null) throw new Exception($"Error in deserializing file: {file}");
                raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), data);
            }
            catch (Exception e)
            {
                // 备份原文件为 .err，带序号避免重复
                string originalFilePath = Path.Combine(_RecordDbPath, fileName);
                string backupBaseName = Path.GetFileNameWithoutExtension(fileName) + ".json.err";
                string backupDir = _RecordDbPath; // 备份在同一目录下，也可指定其他路径
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
                    Console.WriteLine($"[RaidRecord] 序列化记录时出现问题: {e.Message}, 已备份损坏文件至：{backupPath}");
                    Console.WriteLine($"[RaidRecord] DEBUG: {e.StackTrace}");
                }
                catch (Exception copyEx)
                {
                    Console.WriteLine($"[RaidRecord] 备份文件失败：{copyEx.Message}");
                }
                
                Console.WriteLine($"[RaidRecord] 记录文件{fileName}的Json格式错误");
                
                
                raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), []);
            }
            
        }
        return Task.CompletedTask;
    }

    public void SaveRecord(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            Create(playerId);
        }
        
        string jsonString = jsonUtil.Serialize<List<RaidDataWrapper>>(raidRecordCache[playerId]);
        
        if (_RecordDbPath == null)
        {
            Error($"SaveRecord时属性_RecordDbPath意外不存在");
            return;
        }
        File.WriteAllTextAsync(Path.Combine(_RecordDbPath, $"{playerId}.json"), jsonString);
    }

    public List<RaidDataWrapper> GetRecord(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            if (_RecordDbPath == null)
            {
                Error($"属性_RecordDbPath意外不存在");
                return [];
            }

            if (Path.Exists(Path.Combine(_RecordDbPath, $"{playerId}.json")))
            {
                raidRecordCache.Add(playerId, jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(Path.Combine(_RecordDbPath, $"{playerId.ToString()}.json")));
            }
            else
            {
                raidRecordCache.Add(playerId, new List<RaidDataWrapper>());
            }
        }
        return raidRecordCache[playerId];
    }

    public void Create(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            raidRecordCache.Add(playerId, new List<RaidDataWrapper>());
        }
        SaveRecord(playerId);
    }

    public RaidDataWrapper CreateRecord(MongoId playerId)
    {
        var records = GetRecord(playerId);
        var wrapper = new RaidDataWrapper();
        records.Add(wrapper);
        wrapper.Info = new RaidInfo();
        return wrapper;
    }

    public void Delete(MongoId playerId)
    {
        if (raidRecordCache.ContainsKey(playerId))
        {
            raidRecordCache.Remove(playerId);
        }
        
        SaveRecord(playerId);
    }

    public void ZipAll(ItemHelper itemHelper)
    {
        foreach (var playerId in raidRecordCache.Keys)
        {
            ZipAll(itemHelper, playerId);
        }
    }
    
    public void ZipAll(ItemHelper itemHelper, MongoId playerId)
    {
        foreach (var raidDataWrapper in raidRecordCache.GetValueOrDefault(playerId, []))
        {
            raidDataWrapper.Zip(itemHelper);
        }
    }
}