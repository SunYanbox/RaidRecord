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
}