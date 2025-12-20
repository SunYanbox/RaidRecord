using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace RaidRecord.Core.Utils;

[Injectable]
public class CmdUtil(
    ProfileHelper profileHelper,
    LocalizationManager localizationManager,
    RecordCacheManager recordCacheManager,
    ModConfig modConfig
)
{
    #region 提供依赖给工具调用 | 只放大部分命令需要的依赖
    public readonly LocalizationManager? LocalizationManager = localizationManager;
    public readonly RecordCacheManager? RecordCacheManager = recordCacheManager;
    public readonly ModConfig? ModConfig = modConfig;
    public readonly ParaInfoBuilder ParaInfoBuilder = new();
    #endregion

    /// <summary>
    /// 从解析的参数字典中获取指定类型的参数的值
    /// </summary>
    /// <param name="parameters">参数字典</param>
    /// <param name="key">参数键</param>
    /// <param name="defaultValue">默认值</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>获取到的参数值或者默认值</returns>
    /// <exception cref="ArgumentException">传入的参数键为空</exception>
    public static T GetParameter<T>(Dictionary<string, string> parameters,
        string key,
        T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Para key cannot be null or empty", nameof(key));

        // 如果字典中不存在该键，返回默认值
        if (!parameters.TryGetValue(key.ToLower(), out string? stringValue))
            return defaultValue;

        // 如果值为空，返回默认值
        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        try
        {
            // 使用Convert.ChangeType进行转换
            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch (Exception ex) when (
            ex is InvalidCastException or FormatException or OverflowException)
        {
            // 转换失败时返回默认值
            return defaultValue;
        }
    }

    public UserDialogInfo GetChatBot()
    {
        return new UserDialogInfo
        {
            Id = "68e2d45e17ea301214c2596d",
            Aid = 8100860,
            Info = new UserDialogDetails
            {
                Nickname = GetLocalText("RC MC.ChatBot.NickName"),
                Side = "Usec",
                Level = 69,
                MemberCategory = MemberCategory.Sherpa,
                SelectedMemberCategory = MemberCategory.Sherpa
            }
        };
    }

    public string DateFormatterFull(long timestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        int year = date.Year;
        int month = date.Month;
        int day = date.Day;
        string time = date.ToShortTimeString();
        return GetLocalText("RC MC.Chat.Format.Time", year, month, day, time);
    }

    public string GetLocalText(string msgId, params object?[] args)
    {
        return LocalizationManager?.GetTextFormat(msgId, args) ?? $"[Cant find text: {msgId}]";
    }

    /// <summary>  Command验证参数的工具 </summary>
    public string? VerifyIParametric(Parametric parametric)
    {
        if (string.IsNullOrEmpty(parametric.SessionId))
        {
            return GetLocalText("RC MC.Chat.verify.error0");
        }

        try
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(parametric.SessionId!);
            if (pmcData?.Id == null)
            {
                throw new NullReferenceException($"{nameof(pmcData)} or {nameof(pmcData.Id)}");
            }
            string playerId = pmcData.Id;
            if (string.IsNullOrEmpty(playerId)) throw new Exception(GetLocalText("RC MC.Chat.verify.error1"));
        }
        catch (Exception e)
        {
            ModConfig?.LogError(e, "RaidRecordManagerChat.VerifyIParametric", GetLocalText("RC MC.Chat.verify.error2", e.Message));
            return GetLocalText("RC MC.Chat.verify.error2", e.Message);
        }

        return parametric.ManagerChat == null ? GetLocalText("RC MC.Chat.verify.error3") : null;

    }

    public MongoId? GetAccountBySession(string sessionId)
    {
        return RecordCacheManager!.GetAccount(profileHelper.GetPmcProfile(sessionId)?.Id ?? new MongoId());
    }

    public List<RaidArchive> GetArchivesBySession(string sessionId)
    {
        List<RaidArchive> result = [];
        MongoId? account = GetAccountBySession(sessionId);
        if (account == null) return result;
        EFTCombatRecord records = RecordCacheManager!.GetRecord(account.Value);
        foreach (RaidDataWrapper record in records.Records)
        {
            if (record.IsArchive)
            {
                result.Add(record.Archive!);
            }
        }
        return result;
    }

    /// <summary>
    /// 尝试通过serverId配合sessionId获取准确存档
    /// </summary>
    public RaidArchive? GetArchiveWithServerId(string serverId, string sessionId)
    {
        if (string.IsNullOrEmpty(serverId)) return null;
        List<RaidArchive> records = GetArchivesBySession(sessionId);
        RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
        return record;
    }

    /// <summary>
    /// 尝试通过index配合sessionId获取准确存档
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">索引超出范围时报错</exception>
    public RaidArchive GetArchiveWithIndex(int index, string sessionId)
    {
        List<RaidArchive> records = GetArchivesBySession(sessionId);
        if (index >= records.Count || index < 0)
            throw new IndexOutOfRangeException($"index {index} out of range: [0, {records.Count})");
        return records[index];
    }
}