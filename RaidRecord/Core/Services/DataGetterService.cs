using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Models.BaseModels;
using RaidRecord.Core.Models.Services;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.Services;

/// <summary>
/// 封装常用的数据获取方法
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class DataGetterService(
    DialogueHelper dialogueHelper,
    DatabaseService databaseService,
    RecordManager recordManager,
    ProfileHelper profileHelper,
    I18N i18N)
{
    /// <summary>
    /// 获取聊天机器人信息
    /// </summary>
    public UserDialogInfo GetChatBotInfo()
    {
        return new UserDialogInfo
        {
            Id = "68e2d45e17ea301214c2596d",
            Aid = 8100860,
            Info = new UserDialogDetails
            {
                Nickname = i18N.GetText("ChatBotName"),
                Side = "Usec",
                Level = 69,
                MemberCategory = MemberCategory.Sherpa,
                SelectedMemberCategory = MemberCategory.Sherpa
            }
        };
    }

    /// <summary>
    /// 获取指定存档所有聊天记录
    /// </summary>
    /// <param name="id">sessionId或者PmcId或者ProfileId</param>
    /// <returns></returns>
    public Dictionary<MongoId, Dialogue> GetDialogsForProfile(MongoId id)
    {
        return dialogueHelper.GetDialogsForProfile(id);
    }

    /// <summary>
    /// 获取SPT globals下的本地化数据字典
    /// </summary>
    public Dictionary<string, string>? GetSptLocals()
    {
        return i18N.GetSptLocals();
    }

    /// <summary>
    /// 获取指定会话的存档账户
    /// </summary>
    /// <param name="sessionId">Pmc或Scav Id</param>
    /// <returns></returns>
    public MongoId? GetAccountBySession(string sessionId)
    {
        return recordManager.GetAccount(profileHelper.GetPmcProfile(sessionId)?.Id ?? new MongoId());
    }

    /// <summary>
    /// 获取指定会话下的所有存档
    /// </summary>
    /// <param name="sessionId">sessionId或者PmcId或者ProfileId</param>
    public List<RaidArchive> GetArchivesBySession(string sessionId)
    {
        List<RaidArchive> result = [];
        MongoId? account = GetAccountBySession(sessionId);
        if (account == null) return result;
        EFTCombatRecord records = recordManager.GetRecord(account.Value);
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
        {
            throw new IndexOutOfRangeException(i18N.GetText(
                "DataGetter-Error.索引超出记录数量范围",
                new
                {
                    Index = index,
                    RecordCount = records.Count
                }));
        }
        // throw new IndexOutOfRangeException($"index {index} out of range: [0, {records.Count})");
        return records[index];
    }

    /// <summary>
    /// 获取分页的存档 | 会在内部调整page和pageSize的值
    /// </summary>
    /// <param name="session">sessionId</param>
    /// <param name="page">页码(从1开始)</param>
    /// <param name="pageSize">每页数量(默认10)</param>
    /// <param name="filter">筛选存档的函数</param>
    /// <returns>分页结果</returns>
    public ArchivePageableResult GetArchivesPageable(string session, int page = -1, int pageSize = 10,
        Func<RaidArchive, bool>? filter = null)
    {
        ArchivePageableResult result = new();
        filter ??= x => !string.IsNullOrEmpty(x.ServerId);
        List<RaidArchive> records = GetArchivesBySession(session);
        pageSize = Math.Min(PageSizeRange.Right, Math.Max(PageSizeRange.Left, pageSize));

        int totalCount = records.Count;
        int pageTotal = (int)Math.Ceiling((double)totalCount / pageSize);

        page = Math.Min(Math.Max(1, page > 0 ? page : pageTotal + page + 1), pageTotal);

        int indexLeft = Math.Max(pageSize * (page - 1), 0);
        int indexRight = Math.Min(pageSize * page, totalCount);
        if (totalCount <= 0)
        {
            result.Errors = [$"您没有任何历史战绩, 请至少对局一次后再来查询吧"];
            return result;
        }
        List<ArchiveIndexed> results = [];
        for (int i = indexLeft; i < indexRight; i++)
        {
            results.Add(new ArchiveIndexed(records[i], i));
        }
        int countBeforeCheck = results.Count;
        if (countBeforeCheck <= 0)
        {
            result.Errors = [$"未查询到您第{indexLeft + 1}到{indexRight}条历史战绩, 支持的页码范围是[0, {totalCount})"];
            return result;
        }

        results.RemoveAll(x => !filter(x.Archive));
        int countAfterCheck = results.Count;

        result.Archives = results.ToList();
        result.IndexRange = new RangeTuple<int>(indexLeft, indexRight);
        result.Page = page;
        result.PageMax = pageTotal;
        result.JumpData = countBeforeCheck - countAfterCheck;

        return result;
    }



    public readonly RangeTuple<int> PageSizeRange = new(1, 50);
}