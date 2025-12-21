using System.Reflection;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Systems;

/// <summary>
/// 管理每个账户的战绩数据
/// <br />
/// 用于加载, 保存, 获取账户战绩数据
/// <br />
/// 提供获取PmcData的辅助方法
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class RecordManager(
    LocalizationManager localManager,
    JsonUtil jsonUtil,
    ModConfig modConfig,
    ModHelper modHelper,
    ProfileHelper profileHelper,
    ItemHelper itemHelper,
    SaveServer saveServer
): IOnLoad
{
    private string? _recordDbPath;

    /// <summary> 账户id -> 账户历史战绩 </summary>
    private readonly Dictionary<MongoId, EFTCombatRecord> _eftCombatRecords = new();

    /// <summary> Pmc/Scav id 到账户id的映射 </summary>
    private readonly Dictionary<MongoId, MongoId> _playerId2Account = new();

    /// <summary> 所有账号id的映射 </summary>
    private HashSet<MongoId> _accountIds = [];

    /// <summary> 维护常用Id映射 </summary>
    public void UpdateAccountData()
    {
        Dictionary<MongoId, SptProfile> profiles = saveServer.GetProfiles();
        HashSet<MongoId> accounts = profiles.Keys.ToHashSet();
        _accountIds = accounts;
        // 更新Pmc/Scav id 到账户id的映射
        string msg = localManager.GetText("RecordManager-Debug.从SPT加载账户数据.标题");
        // string msg = "从SPT加载账户数据: ";
        foreach (MongoId accountId in accounts)
        {
            SptProfile sptProfile = profiles[accountId];
            MongoId? pmcId = sptProfile.CharacterData?.PmcData?.Id;
            MongoId? scavId = sptProfile.CharacterData?.ScavData?.Id;
            if (pmcId is not null) _playerId2Account[pmcId.Value] = accountId;
            if (scavId is not null) _playerId2Account[scavId.Value] = accountId;
            // msg += $"\n\tAccount: {accountId}, PmcId: {pmcId}, ScavId: {scavId}";
            msg += localManager.GetText("RecordManager-Debug.从SPT加载账户数据.内容", new
            {
                Account = accountId,
                PmcId = pmcId,
                ScavId = scavId
            });
        }
        modConfig.Debug(msg);
    }

    /// <summary>
    /// 根据已有的账号Id, 初始化全部账号的EFTCombatRecord数据
    /// <br />
    /// 注意: 必须在LoadAllRecords之后使用, 避免覆盖账户数据
    /// </summary>
    public void InitAllEFTCombatRecord()
    {
        Dictionary<MongoId, SptProfile> profiles = saveServer.GetProfiles();
        foreach (MongoId accountId in _accountIds)
        {
            if (_eftCombatRecords.ContainsKey(accountId)) continue;
            _eftCombatRecords.Add(accountId, new EFTCombatRecord(accountId));
        }
    }

    public Task OnLoad()
    {
        UpdateAccountData();
        LoadAllRecords();
        InitAllEFTCombatRecord();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 加载指定数据文件加载
    /// </summary>
    /// <param name="file">文件绝对路径</param>
    private void LoadFile(string file)
    {
        if (_recordDbPath == null)
        {
            modConfig.Error($"加载记录数据库时数据库文件路径\"{_recordDbPath}\"意外不存在, 请确保`db\\records`文件夹路径存在, 保存失败");
            return;
        }
        string fileName = Path.GetFileName(file);
        string fileBaseName = Path.GetFileNameWithoutExtension(fileName);
        if (!fileName.EndsWith(".json")) return;

        try
        {
            try
            {
                // 0.6.2开始
                var data = jsonUtil.DeserializeFromFile<EFTCombatRecord>(file);
                if (data == null) throw new Exception($"反序列化文件{file}时获取不到数据");
                _eftCombatRecords.Add(data.AccountId, data);
            }
            catch (Exception e)
            {
                modConfig.Error($"尝试以0.6.2+版本反序列化数据文件{file}时发生错误: {e.Message}, 将尝试迁移0.6.1版本数据库", e);
                // 暂时兼容0.6.1版本的数据库
                var data = jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(file);
                if (data == null) throw new Exception($"反序列化文件{file}时获取不到数据");
                MongoId account = _playerId2Account[fileBaseName];
                _eftCombatRecords.Add(account, new EFTCombatRecord(account, data));
                // 重命名文件, 避免重复迁移
                string newFile = file.Replace(fileBaseName, account.ToString());
                modConfig.Info($"正在将旧数据库文件{file}迁移为新版本格式: {newFile}");
                File.Move(file, newFile);
                SaveEFTRecord(account);
            }
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
                modConfig.Info($"序列化记录时出现问题: {e.Message}, 已备份损坏文件至: {backupPath}");
                // Console.WriteLine($"[RaidRecord] DEBUG: {e.StackTrace}");
            }
            catch (Exception copyEx)
            {
                modConfig.Error($"备份文件过程中发生错误: {copyEx.Message}", copyEx);
            }
        }
    }

    /// <summary> 读取所有本地账户历史战绩 </summary>
    public void LoadAllRecords()
    {
        string localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _recordDbPath = Path.Combine(localsDir, "db\\records");

        if (!Directory.Exists(_recordDbPath)) Directory.CreateDirectory(_recordDbPath);

        foreach (string file in Directory.GetFiles(_recordDbPath))
        {
            if (Path.Exists(file))
                LoadFile(file);
        }
    }

    /// <summary> 通过Pmc或Scav Id获取账号Id </summary>
    public MongoId? GetAccount(MongoId playerId)
    {
        return _playerId2Account.GetValueOrDefault(playerId);
    }

    /// <summary> 通过Account, Pmc, Session或Scav Id获取PmcData实例 </summary>
    public PmcData GetPmcDataByPlayerId(MongoId playerId)
    {
        if (_playerId2Account.TryGetValue(playerId, out MongoId account))
        {
            SptProfile sptProfile = profileHelper.GetFullProfile(account);
            /*
             * SessionId, AccountId和PmcId相同, ScavId为PmcId+1
             */
            return (playerId == account
                ? sptProfile.CharacterData!.PmcData
                : sptProfile.CharacterData!.ScavData)!;
        }
        throw new Exception($"未找到{playerId}对应的PmcData实例");
    }

    /// <summary> 保存指定账户和的历史战绩 </summary>
    public void SaveEFTRecord(MongoId account)
    {
        if (!_eftCombatRecords.TryGetValue(account, out EFTCombatRecord? eftRecord))
        {
            modConfig.Error($"保存记录数据库时账户Id: {account}未找到, 请确保已保存过该账户的记录");
            return;
        }
        if (eftRecord.AccountId != account)
        {
            MongoId oldId = eftRecord.AccountId;
            eftRecord.AccountId = account;
            modConfig.Warn($"保存记录数据库时账户Id不一致, 已将数据库账户Id从{oldId}修改为: {account}");
        }
        if (_recordDbPath == null)
        {
            modConfig.Error($"保存记录数据库时数据库文件路径\"{_recordDbPath}\"意外不存在, 请确保`db\\records`文件夹路径存在, 保存失败");
            return;
        }
        string path = Path.Combine(_recordDbPath, $"{account}.json");

        File.WriteAllTextAsync(path, jsonUtil.Serialize(eftRecord));
    }

    /// <summary> 通过账号获取历史记录 </summary>
    public EFTCombatRecord GetRecord(MongoId account)
    {
        if (_accountIds.Contains(account)) return _eftCombatRecords[account];
        // 重新加载数据库
        LoadAllRecords();

        if (_recordDbPath == null)
        {
            modConfig.Error("记录数据库文件路径未正确获取, 请确保`db\\records`文件夹路径存在");
            throw new InvalidDataException($"记录数据库文件路径({nameof(_recordDbPath)})未正确获取, 请确保`db\\records`文件夹路径存在");
        }

        string file = Path.Combine(_recordDbPath, $"{account.ToString()}.json");
        if (Path.Exists(file))
        {
            LoadFile(file);
        }
        else
        {
            _eftCombatRecords.Add(account, new EFTCombatRecord(account));
        }
        return _eftCombatRecords[account];
    }

    /// <summary> 为将进入对局的Pmc或Scav创建RaidDataWrapper记录 </summary>
    public RaidDataWrapper CreateRecord(MongoId playerId)
    {
        try
        {
            MongoId? account = GetAccount(playerId);
            if (account == null!)
            {
                throw new Exception($"创建记录时未找到玩家{playerId}的账户Id, 请确保已存在过该玩家账户的记录");
            }
            EFTCombatRecord records = GetRecord(account.Value);
            // Console.WriteLine($"DEBUG RecordManager.CreateRecord > 玩家{playerId}的记录records为{records}, {records?.Count}条");
            // 检查 records 是否为 null
            if (records == null)
            {
                throw new Exception($"GetRecord中获取的records为null playId: {playerId}");
            }
            RaidDataWrapper wrapper = new();
            if (records.InfoRecordCache != null)
            {
                records.Records.Add(records.InfoRecordCache.Zip(itemHelper));
                modConfig.Warn($"玩家{playerId}的战绩记录缓存不为空, 已直接归档缓存");
            }
            records.InfoRecordCache = wrapper;
            wrapper.Info = new RaidInfo();
            // Console.WriteLine($"DEBUG RecordManager.CreateRecord > 返回值: {wrapper}, Info: {wrapper.Info}, Archive:  {wrapper.Archive}");
            return wrapper;
        }
        catch (Exception e)
        {
            Console.WriteLine($"RecordManager.CreateRecord: {e.Message}\nstack: {e.StackTrace}");
            modConfig.LogError(e, "RaidRecordManager.CreateRecord.try-catch", "创建记录实例时出错");
            throw;
        }
    }

    /// <summary>
    /// 删除一个账号下的所有历史记录数据(慎用!!!)
    /// </summary>
    /// <param name="account"></param>
    public void Delete(MongoId account)
    {
        if (_eftCombatRecords.Remove(account))
            SaveEFTRecord(account);
    }

    public void ZipAll()
    {
        foreach (MongoId playerId in _eftCombatRecords.Keys)
        {
            ZipAccount(playerId);
        }
    }

    /// <summary>
    /// 压缩指定账户下的所有历史记录
    /// <br />
    /// 会同时压缩未归档缓存
    /// </summary>
    public void ZipAccount(MongoId account)
    {
        if (_eftCombatRecords.TryGetValue(account, out EFTCombatRecord? combatRecord))
        {
            foreach (RaidDataWrapper wrapper in combatRecord.Records)
            {
                wrapper.Zip(itemHelper);
            }
            if (combatRecord.InfoRecordCache != null)
            {
                combatRecord.Records.Add(combatRecord.InfoRecordCache.Zip(itemHelper));
                combatRecord.InfoRecordCache = null;
            }
        }
        SaveEFTRecord(account);
    }
}