using System.Collections.ObjectModel;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Models.BaseModels;
using RaidRecord.Core.Models.Services;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace RaidRecord.Core.Services;

/// <summary>
/// 封装常用的数据获取方法
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class DataGetterService(
    DialogueHelper dialogueHelper,
    RecordManager recordManager,
    ProfileHelper profileHelper,
    ItemHelper itemHelper,
    I18N i18N)
{
    /// <summary>
    /// 获取Pmc存档数据
    /// </summary>
    public PmcData? GetPmcData(MongoId session)
    {
        return profileHelper.GetPmcProfile(session);
    }

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
        return i18N.SptLocals;
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
    public async Task<List<RaidArchive>> GetArchivesBySession(string sessionId)
    {
        List<RaidArchive> result = [];
        MongoId? account = GetAccountBySession(sessionId);
        if (account == null) return result;
        EFTCombatRecord records = await recordManager.GetRecord(account.Value);
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
        List<RaidArchive> records = GetArchivesBySession(sessionId).Result;
        RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
        return record;
    }

    /// <summary>
    /// 尝试通过index配合sessionId获取准确存档
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">索引超出范围时报错</exception>
    public RaidArchive GetArchiveWithIndex(int index, string sessionId)
    {
        List<RaidArchive> records = GetArchivesBySession(sessionId).Result;
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
        List<RaidArchive> records = GetArchivesBySession(session).Result;
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

    /// <summary>
    /// 获取所有存档的索引与存档信息
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    public List<ArchiveIndexed> GetArchivesIndexed(string session)
    {
        
        return GetArchivesBySession(session).Result
            .Select((x, i) => new ArchiveIndexed(x, i))
            .ToList();
    }

    /// <summary> 获取已存在的所有账户ID </summary>
    public ReadOnlySet<MongoId> GetAllAccounts()
    {
        return recordManager.AccountIds;
    }

    public readonly RangeTuple<int> PageSizeRange = new(1, 50);

    /// <summary> 被视为战备的基类(枪械配件) </summary>
    public readonly IReadOnlySet<MongoId> WeaponModClassesAll = (HashSet<MongoId>) [
        BaseClasses.MAGAZINE, // 弹匣
        BaseClasses.MOD, // 模组
        BaseClasses.ASSAULT_SCOPE, // 突击瞄准镜
        BaseClasses.AUXILIARY_MOD, // 辅助模组
        BaseClasses.BARREL, // 枪管
        BaseClasses.BIPOD, // 两脚架
        BaseClasses.COLLIMATOR, // 准直瞄具
        BaseClasses.COMPACT_COLLIMATOR, // 紧凑型准直瞄具
        BaseClasses.COMPENSATOR, // 制退器
        BaseClasses.FLASH_HIDER, // 消焰器
        BaseClasses.FLASHLIGHT, // 手电筒
        BaseClasses.FOREGRIP, // 前握把
        BaseClasses.FUNCTIONAL_MOD, // 功能模组
        BaseClasses.GASBLOCK, // 导气箍
        BaseClasses.HANDGUARD, // 护木
        BaseClasses.IRON_SIGHT, // 机械瞄具
        BaseClasses.LIGHT_LASER, // 激光指示灯
        BaseClasses.MASTER_MOD, // 主模组
        BaseClasses.MOUNT, // 导轨/支架
        BaseClasses.MUZZLE, // 枪口装置
        BaseClasses.MUZZLE_COMBO, // 枪口组合装置
        BaseClasses.OPTIC_SCOPE, // 光学瞄准镜
        BaseClasses.PISTOL_GRIP, // 手枪式握把
        BaseClasses.RAIL_COVERS, // 导轨护盖
        BaseClasses.RECEIVER, // 机匣
        BaseClasses.SHAFT, // 轴杆
        BaseClasses.SIGHTS, // 瞄具
        BaseClasses.SILENCER, // 消音器
        BaseClasses.SPECIAL_SCOPE, // 特殊瞄准镜
        BaseClasses.STOCK, // 枪托
        BaseClasses.TACTICAL_COMBO // 战术组合装置
    ];
    
    /// <summary> 被视为战备的基类(装备配件) </summary>
    public readonly IReadOnlySet<MongoId> EquipmentModClassesAll = (HashSet<MongoId>) [
        BaseClasses.BUILT_IN_INSERTS, // 内置插件
        BaseClasses.ARMOR_PLATE, // 装甲板
        BaseClasses.VISORS // 面罩/护目镜
    ];

    /// <summary> 被视为战备的基类(枪械) </summary>
    public readonly IReadOnlySet<MongoId> WeaponClassesAlls = (HashSet<MongoId>) [
        BaseClasses.WEAPON, // 武器
        BaseClasses.ASSAULT_CARBINE, // 突击卡宾枪
        BaseClasses.ASSAULT_RIFLE, // 突击步枪
        BaseClasses.GRENADE_LAUNCHER, // 榴弹发射器
        BaseClasses.MACHINE_GUN, // 机枪
        BaseClasses.MARKSMAN_RIFLE, // 精确射手步枪
        BaseClasses.PISTOL, // 手枪
        BaseClasses.REVOLVER, // 左轮手枪
        BaseClasses.ROCKET_LAUNCHER, // 火箭发射器
        BaseClasses.ROCKET, // 火箭弹(必须和ROCKET_LAUNCHER在一起)
        BaseClasses.SHOTGUN, // 霰弹枪
        BaseClasses.SMG, // 冲锋枪
        BaseClasses.SNIPER_RIFLE, // 狙击步枪
        BaseClasses.SPECIAL_WEAPON, // 特殊武器
        BaseClasses.KNIFE, // 刀
        BaseClasses.THROW_WEAP, // 投掷武器
        BaseClasses.LAUNCHER // 发射器
    ];

    /// <summary> 被视为战备的基类(头部护甲) </summary>
    public readonly IReadOnlySet<MongoId> HeadClassesAlls = (HashSet<MongoId>) [
        BaseClasses.FACE_COVER, // 面部防护
        BaseClasses.HEADPHONES, // 耳机
        BaseClasses.HEADWEAR // 头饰
    ];

    /// <summary> 被视为战备的基类(护甲) </summary>
    public readonly IReadOnlySet<MongoId> ArmorClassesAlls = (HashSet<MongoId>) [
        BaseClasses.ARMOR // 护甲
    ];
    
    /// <summary> 被视为战备的基类(胸挂) </summary>
    public readonly IReadOnlySet<MongoId> VestClassesAlls = (HashSet<MongoId>) [
        BaseClasses.VEST // 胸挂/背心
    ];
    
    /// <summary> 被视为战备的基类(背包) </summary>
    public readonly IReadOnlySet<MongoId> BackpackClassesAlls = (HashSet<MongoId>) [
        BaseClasses.BACKPACK // 背包
    ];

    private HashSet<MongoId>? _equipmentClassesAlls;

    /// <summary> 被视为战备的基类(枪械, 胸挂, 背包, 护甲, 头盔等) </summary>
    public IReadOnlySet<MongoId> EquipmentClassesAlls
    {
        get
        {
            _equipmentClassesAlls ??= WeaponClassesAlls
                .Union(WeaponModClassesAll)
                .Union(EquipmentModClassesAll)
                .Union(HeadClassesAlls)
                .Union(ArmorClassesAlls)
                .Union(VestClassesAlls)
                .Union(BackpackClassesAlls).ToHashSet();
            return _equipmentClassesAlls;
        }
    }

    private void InitName2Id()
    {
        if (_name2Id != null) return;
        _name2Id ??= new Dictionary<string, string>();
        Dictionary<string, string>? sptLocals = GetSptLocals();
        if (sptLocals == null) return;
        foreach (KeyValuePair<string, string> kv in sptLocals.AsReadOnly()) // 不需要更改spt的数据库, 只读限定一下
        {
            if (kv.Key.EndsWith(" ShortName")
                || kv.Key.EndsWith(" Description")
                || kv.Key.Length != _itemNameLen)
                continue;
            string tpl = kv.Key.Replace(" Name", "").Replace(" name", "");
            if (tpl.Length != 24 || !itemHelper.IsValidItem(tpl)) continue;
            int retryTimes = 0;
            while (retryTimes < 10)
            {
                // 后缀
                string retrySuffix = retryTimes > 0 ? $"_{retryTimes}" : "";
                if (_name2Id.TryAdd(kv.Value, $"{tpl}{retrySuffix}"))
                    break;
                retryTimes++;
            }
        }
    }

    /// <summary>
    /// 重新初始化Name2Id
    /// </summary>
    public void ReInitName2Id()
    {
        _name2Id = null;
    }

    public Dictionary<string, string> Name2Id
    {
        get
        {
            if (_name2Id == null) InitName2Id();
            return _name2Id!;
        }
    }

    /// <summary>
    /// 用来缓存名称和id的映射关系, 只有调用过任意一次price命令后才会初始化
    /// </summary>
    private Dictionary<string, string>? _name2Id;

    private readonly int _itemNameLen = "5422acb9af1c889c16000029 Name".Length;
}