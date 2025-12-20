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

[Injectable]
public class RaidUtil(
    ItemHelper itemHelper,
    ProfileHelper profileHelper,
    RecordCacheManager recordCacheManager)
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
        raidInfo.ItemsTakeIn = ItemUtil.GetInventoryInfo(raidProfile, itemHelper);
        // Console.WriteLine($"获取到的物品:");
        // foreach (var item in ItemsTakeIn.Values)
        // {
        //     Console.WriteLine($"\t{item}");
        // }

        Item[] itemsTakeIn = raidInfo.ItemsTakeIn.Values.ToArray();
        raidInfo.PreRaidValue = ItemUtil.GetItemsValueAll(itemsTakeIn, itemHelper);
        raidInfo.EquipmentValue = ItemUtil.GetItemsValueWithBaseClasses(itemsTakeIn, Equipments, itemHelper);
        raidInfo.SecuredValue = ItemUtil.GetItemsValueAll(ItemUtil.GetAllItemsInContainer("SecuredContainer", itemsTakeIn), itemHelper);
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

        // 参考InRaidHelper和LocationLifecycleService处理对局结束的存档
        if (request.Results.Profile != null)
        {
            // ServerId has various info stored in it, delimited by a period
            string[] serverDetails = raidInfo.ServerId.Split(".");
            string locationName = serverDetails[0].ToLowerInvariant();
            bool isPmc = serverDetails[1].ToLowerInvariant().Contains("pmc");
            bool isDead = request.Results.IsPlayerDead();
            bool isTransfer = request.Results.IsMapToMapTransfer();
            bool isSurvived = request.Results.IsPlayerSurvived();
            PmcData postRaidProfile = request.Results.Profile; // 战局后角色数据

            // Scav死亡时此处仍然能获取到物品(疑问: Pmc死亡时既无法断点到这里, 也没有else中的输出)
            raidInfo.ItemsTakeOut = ItemUtil.GetInventoryInfo(postRaidProfile, itemHelper);

            if (!isPmc && isDead)
            {
                // Scav死亡, 无法带出任何物品(由于Scav死亡时LocationLifecycleService会直接生成下一次存档, 直接清空字典保险一些)
                raidInfo.ItemsTakeOut = new Dictionary<MongoId, Item>();
            }

            HandleRaidEndInventoryAndValue(raidInfo, postRaidProfile);
        }
        else
        {
            Console.WriteLine($"[RaidInfo] 警告: 获取对局结束数据时, 获取到的PostRaidProfile数据({nameof(request.Results.Profile)})为null");
            try
            {
                PmcData pmcProfile = recordCacheManager.GetPmcDataByPlayerId(raidInfo.PlayerId);
                raidInfo.ItemsTakeOut = ItemUtil.GetInventoryInfo(pmcProfile, itemHelper);
                HandleRaidEndInventoryAndValue(raidInfo, pmcProfile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[RaidInfo] Error 推测对局结束数据时, 无法正确获取PMC存档数据: \n\tMessage: {e.Message}\n\tStackTrace: {e.StackTrace}");
            }
        }

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
        raidInfo.Addition.Clear();
        raidInfo.Remove.Clear();
        raidInfo.Changed.Clear();
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
        // 记录获取/变化的物资
        foreach ((MongoId itemId, Item item) in raidInfo.ItemsTakeIn)
        {
            if (raidInfo.ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                if (Math.Abs(itemHelper.GetItemQualityModifier(item) - itemHelper.GetItemQualityModifier(newItem)) < Constants.ArchiveCheckJudgeError) continue;
                raidInfo.Changed.Add(itemId);
            }
            else
            {
                raidInfo.Remove.Add(itemId);
            }
        }
        foreach ((MongoId itemId, Item _) in raidInfo.ItemsTakeOut)
        {
            if (!raidInfo.ItemsTakeIn.ContainsKey(itemId)) raidInfo.Addition.Add(itemId);
        }
        // 收益, 战损记录
        raidInfo.GrossProfit = ItemUtil.CalculateInventoryValue(raidInfo.ItemsTakeOut, raidInfo.Addition.ToArray(), itemHelper);
        raidInfo.CombatLosses = ItemUtil.CalculateInventoryValue(raidInfo.ItemsTakeIn, raidInfo.Remove.ToArray(), itemHelper);
        foreach ((MongoId itemId, Item oldItem) in DataUtil.GetSubDict(raidInfo.ItemsTakeIn, raidInfo.Changed))
        {
            long oldValue = ItemUtil.GetItemValue(oldItem, itemHelper);
            if (raidInfo.ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                long newValue = ItemUtil.GetItemValue(newItem, itemHelper);
                if (newValue > oldValue) raidInfo.GrossProfit += newValue - oldValue;
                else raidInfo.CombatLosses += oldValue - newValue;
            }
            else
            {
                Console.WriteLine($"[RaidRecord] 警告: 本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
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