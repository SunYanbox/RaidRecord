using RaidRecord.Core.Models;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace RaidRecord.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class RaidUtil(
    ItemUtil itemUtil,
    PriceSystem priceSystem,
    ItemHelper itemHelper,
    ProfileHelper profileHelper,
    RecordManager recordCacheManager)
{
    /// <summary>
    /// 根据开局请求初始化数据
    /// </summary>
    public void HandleRaidStart(RaidInfo raidInfo, string serverId, MongoId sessionId)
    {
        raidInfo.ServerId = serverId;
        raidInfo.State = "未归档";
        bool isPmc = raidInfo.ServerId.Contains("Pmc");
        raidInfo.Side = isPmc ? "Pmc" : "Savage";
        PmcData? pmcProfile = profileHelper.GetPmcProfile(sessionId);
        PmcData? scavProfile = profileHelper.GetScavProfile(sessionId);
        PmcData raidProfile = isPmc switch
        {
            true when pmcProfile is { Id: not null } => pmcProfile,
            false when scavProfile is { Id: not null } => scavProfile,
            _ => throw new InvalidDataException($"无法通过session\"{sessionId}\"获取到与存档数据[{raidInfo.ServerId}]一致的非空PMC存档或SCAV存档数据")
        };
        raidInfo.PlayerId = (isPmc ? pmcProfile?.Id : scavProfile?.Id) ?? throw new Exception("获取到的PMC或SCAV存档的Id为null; 这可能是session已失效, 存档文件损坏或者存档数据库被意外修改!!!");
        raidInfo.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        raidInfo.ItemsTakeIn = itemUtil.GetInventoryInfo(raidProfile);
        // Console.WriteLine($"获取到的物品:");
        // foreach (var item in ItemsTakeIn.Values)
        // {
        //     Console.WriteLine($"\t{item}");
        // }

        Item[] itemsTakeIn = raidInfo.ItemsTakeIn.Values.ToArray();
        raidInfo.PreRaidValue = itemUtil.GetItemsValueAll(itemsTakeIn);
        raidInfo.EquipmentValue = itemUtil.GetItemsValueWithBaseClasses(itemsTakeIn, Equipments);
        raidInfo.SecuredValue = itemUtil.GetItemsValueAll(itemUtil.GetAllItemsInContainer("SecuredContainer", itemsTakeIn));
        // Console.WriteLine($"itemsTakeIn.Length: {itemsTakeIn.Length}\n\tPreRaidValue: {PreRaidValue}\n\tEquipmentValue: {EquipmentValue}\n\tSecuredValue: {SecuredValue}");
    }

    /// <summary>
    /// 根据结束请求载入数据
    /// </summary>
    public void HandleRaidEnd(RaidInfo raidInfo, EndLocalRaidRequestData request, MongoId sessionId)
    {
        if (request == null) throw new NullReferenceException("HandleRaidEnd的EndLocalRaidRequestData类型参数data意外为null");
        if (request.Results == null)
        {
            throw new NullReferenceException($"获取到的结束请求数据的结果为null, 忽略此请求");
        }

        // 不能参考InRaidHelper和LocationLifecycleService处理对局结束的存档!!!

        // ServerId has various info stored in it, delimited by a period
        string[] serverDetails = raidInfo.ServerId.Split(".");
        string locationName = serverDetails[0].ToLowerInvariant();
        bool isPmc = serverDetails[1].ToLowerInvariant().Contains("pmc");
        bool isDead = request.Results.IsPlayerDead();
        bool isTransfer = request.Results.IsMapToMapTransfer();
        bool isSurvived = request.Results.IsPlayerSurvived();
        // PmcData postRaidProfile = request.Results.Profile; // 战局后角色数据(Pmc和战局进入时的物品相同)

        // 正确获取Scav或Pmc数据
        PmcData pmcProfile = recordCacheManager.GetPmcDataByPlayerId(raidInfo.PlayerId);

        if (isPmc)
        {
            raidInfo.ItemsTakeOut = itemUtil.GetInventoryInfo(pmcProfile);
        }
        else
        {
            // Scav模式
            PmcData postRaidProfile = request.Results.Profile!;

            raidInfo.ItemsTakeOut = itemUtil.GetInventoryInfo(postRaidProfile);

            if (isDead)
            {
                // Scav死亡, 无法带出任何物品(由于Scav死亡时LocationLifecycleService会直接生成下一次存档, 直接清空字典)
                raidInfo.ItemsTakeOut = new Dictionary<MongoId, Item>();
            }
        }

        HandleRaidEndInventoryAndValue(raidInfo, pmcProfile);

        raidInfo.Results = new RaidResultData
        {
            Result = request.Results.Result,
            KillerId = request.Results.KillerId,
            KillerAid = request.Results.KillerAid,
            ExitName = request.Results.ExitName,
            PlayTime = Convert.ToInt64(request.Results.PlayTime)
        };
    }

    /// <summary>
    /// 根据ID变换信息, 更新IRaidInfo
    ///
    /// <br />
    ///
    /// 将更新ItemsTakeIn, ItemsTakeOut字典
    ///
    /// <br />
    ///
    /// 与Addition, Remove, Changed列表的与id有关字段
    ///
    /// </summary>
    /// <param name="raidInfo"></param>
    /// <param name="replaceInfo">{ 旧ID -> 新ID }</param>
    public void UpdateByReplaceIDs(RaidInfo raidInfo, Dictionary<MongoId, MongoId> replaceInfo)
    {
        foreach (Dictionary<MongoId, Item> map in new[] { raidInfo.ItemsTakeIn, raidInfo.ItemsTakeOut })
        {
            foreach (MongoId oldId in map.Keys)
            {
                if (!replaceInfo.TryGetValue(new MongoId(oldId), out MongoId newId)) continue;
                if (newId == oldId) continue;
                Item itemInstance = map[oldId];
                if (!map.Remove(oldId))
                {
                    Console.WriteLine($"[RaidRecord] 警告 从字典删除{oldId}的过程中出错");
                }
                itemInstance.Id = newId;
                map[newId] = itemInstance;
            }
        }

        List<MongoId>[] lists = [raidInfo.Addition, raidInfo.Remove, raidInfo.Changed];
        foreach (List<MongoId> list in lists)
        {
            for (int i = 0; i < list.Count; i++)
            {
                MongoId oldId = list[i];
                MongoId newId = replaceInfo[oldId];
                if (newId != null! && oldId != newId)
                {
                    list[i] = newId;
                }
            }
        }
    }

    /// <summary>
    /// 根据对局结束的数据(变化量, 结果)归档到RaidInfo
    /// </summary>
    private void HandleRaidEndInventoryAndValue(RaidInfo raidInfo, PmcData pmcData)
    {
        if (pmcData.Stats == null || pmcData.Stats.Eft == null)
        {
            Console.WriteLine($"[RaidInfo] 错误尝试获取对局结束数据时, 获取到的数据({nameof(pmcData.Stats)}和{nameof(pmcData.Stats.Eft)})全部为null");
            return;
        }
        raidInfo.State = raidInfo.State == "推测对局" ? "推测对局" : "已归档";
        // 处理对局结果
        // var resultStats = Utils.Copy(pmcData.Stats.Eft);
        raidInfo.EftStats = pmcData.Stats.Eft with
        {
            SessionCounters = null,
            OverallCounters = null,
            DroppedItems = null,
            DamageHistory = null
        };
        // 处理价值相关数据
        if (raidInfo.ItemsTakeIn.Count == 0 && raidInfo.ItemsTakeOut.Count == 0)
        {
            raidInfo.PreRaidValue
                = raidInfo.EquipmentValue
                    = raidInfo.SecuredValue
                        = raidInfo.GrossProfit
                            = raidInfo.CombatLosses = 0;
            return;
        }
        UpdateItemsChanged(
            raidInfo.Addition,
            raidInfo.Remove,
            raidInfo.Changed,
            raidInfo.ItemsTakeIn,
            raidInfo.ItemsTakeOut
        );
        // 收益, 战损记录
        raidInfo.GrossProfit = itemUtil.CalculateInventoryValue(raidInfo.ItemsTakeOut, raidInfo.Addition.ToArray());
        raidInfo.CombatLosses = itemUtil.CalculateInventoryValue(raidInfo.ItemsTakeIn, raidInfo.Remove.ToArray());
        foreach ((MongoId itemId, Item oldItem) in DataUtil.GetSubDict(raidInfo.ItemsTakeIn, raidInfo.Changed))
        {
            double oldValue = priceSystem.GetItemValue(oldItem);
            if (raidInfo.ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                double newValue = priceSystem.GetItemValue(newItem);
                if (Math.Abs(newValue - oldValue) > Constants.ArchiveCheckJudgeError) 
                    raidInfo.GrossProfit += Convert.ToInt64(newValue - oldValue);
                else 
                    raidInfo.CombatLosses += Convert.ToInt64(oldValue - newValue);
            }
            else
            {
                Console.WriteLine($"[RaidRecord] 警告: 本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
        }
    }

    /// <summary>
    /// 获取对局结束时物品的变动信息（基于完整 Item 对象）
    /// </summary>
    public void UpdateItemsChanged(
        List<MongoId> add,
        List<MongoId> remove,
        List<MongoId> change,
        Dictionary<MongoId, Item> itemsTakeIn,
        Dictionary<MongoId, Item> itemsTakeOut)
    {
        add.Clear();
        remove.Clear();
        change.Clear();

        foreach ((MongoId itemId, Item itemIn) in itemsTakeIn)
        {
            if (itemsTakeOut.TryGetValue(itemId, out Item? itemOut))
            {
                double modIn = itemHelper.GetItemQualityModifier(itemIn);
                double modOut = itemHelper.GetItemQualityModifier(itemOut);
                if (Math.Abs(modIn - modOut) < Constants.ArchiveCheckJudgeError)
                    continue;
                change.Add(itemId);
            }
            else
            {
                remove.Add(itemId);
            }
        }

        foreach ((MongoId itemId, Item _) in itemsTakeOut)
        {
            if (!itemsTakeIn.ContainsKey(itemId))
                add.Add(itemId);
        }
    }

    /// <summary>
    /// 获取对局结束时物品的变动信息（基于预计算的 double 修正值）
    /// </summary>
    public static void UpdateItemsChanged(
        List<MongoId> add,
        List<MongoId> remove,
        List<MongoId> change,
        Dictionary<MongoId, double> itemsTakeIn,
        Dictionary<MongoId, double> itemsTakeOut)
    {
        add.Clear();
        remove.Clear();
        change.Clear();

        foreach ((MongoId itemId, double modIn) in itemsTakeIn)
        {
            if (itemsTakeOut.TryGetValue(itemId, out double modOut))
            {
                // 在带入且在带出就是改变了
                if (Math.Abs(modIn - modOut) < Constants.ArchiveCheckJudgeError)
                    continue;
                change.Add(itemId);
            }
            else
            {
                remove.Add(itemId);
            }
        }

        foreach ((MongoId itemId, double _) in itemsTakeOut)
        {
            if (!itemsTakeIn.ContainsKey(itemId))
                add.Add(itemId);
        }
    }


    // 被视为战备的基类(枪械, 胸挂, 背包, 护甲, 头盔等)
    private static readonly MongoId[] Equipments =
    [
        BaseClasses.WEAPON,
        // BaseClasses.UBGL,
        BaseClasses.ARMOR,
        BaseClasses.ARMORED_EQUIPMENT,
        BaseClasses.HEADWEAR,
        BaseClasses.FACE_COVER,
        BaseClasses.VEST,
        BaseClasses.BACKPACK,
        BaseClasses.VISORS,
        BaseClasses.GASBLOCK,
        BaseClasses.RAIL_COVERS,
        BaseClasses.MOD,
        BaseClasses.FUNCTIONAL_MOD,
        BaseClasses.GEAR_MOD,
        BaseClasses.STOCK,
        BaseClasses.FOREGRIP,
        BaseClasses.MASTER_MOD,
        BaseClasses.MOUNT,
        BaseClasses.MUZZLE,
        BaseClasses.SIGHTS,
        BaseClasses.ASSAULT_SCOPE,
        BaseClasses.TACTICAL_COMBO,
        BaseClasses.FLASHLIGHT,
        BaseClasses.MAGAZINE,
        BaseClasses.LIGHT_LASER,
        BaseClasses.FLASH_HIDER,
        BaseClasses.COLLIMATOR,
        BaseClasses.IRON_SIGHT,
        BaseClasses.COMPACT_COLLIMATOR,
        BaseClasses.COMPENSATOR,
        BaseClasses.OPTIC_SCOPE,
        BaseClasses.SPECIAL_SCOPE,
        BaseClasses.SILENCER,
        BaseClasses.AUXILIARY_MOD,
        BaseClasses.BIPOD,
        BaseClasses.BUILT_IN_INSERTS,
        BaseClasses.ARMOR_PLATE,
        BaseClasses.HANDGUARD,
        BaseClasses.PISTOL_GRIP,
        BaseClasses.RECEIVER,
        BaseClasses.BARREL,
        // BaseClasses.CHARGING_HANDLE,
        BaseClasses.MUZZLE_COMBO,
        BaseClasses.TACTICAL_COMBO
    ];
}