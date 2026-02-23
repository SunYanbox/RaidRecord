using RaidRecord.Core.Configs;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Utils.Cloners;
using SuntionCore.SPTExtensions.Services;

namespace RaidRecord.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class RaidUtil(
    ICloner cloner,
    ItemUtil itemUtil,
    ModConfig modConfig,
    ItemHelper itemHelper,
    PriceSystem priceSystem,
    ProfileHelper profileHelper,
    DataGetterService dataGetter,
    ProfileAndAccountService profileAndAccountService)
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

        Item[] equipments = itemUtil.GetItemsWithBaseClasses(itemsTakeIn, dataGetter.EquipmentClassesAlls);
        Item[] itemsInSecured = itemUtil.GetAllItemsInContainer("SecuredContainer", itemsTakeIn);
        equipments = equipments.Except(itemsInSecured).ToArray(); // 安全箱内的装备不支持也不应该是战备

        raidInfo.EquipmentValue = itemUtil.GetItemsValueAll(equipments);
        raidInfo.EquipmentItems = cloner.Clone(equipments);

        Item[] secured = itemUtil.GetAllItemsInContainer("SecuredContainer", itemsTakeIn);
        raidInfo.SecuredValue = itemUtil.GetItemsValueAll(secured);
        raidInfo.SecuredItems = cloner.Clone(secured);

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
            throw new NullReferenceException("获取到的结束请求数据的结果为null, 忽略此请求");
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
        PmcData pmcProfile = profileAndAccountService.GetPmcDataByPlayerId(raidInfo.PlayerId);

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
                    modConfig.Warn($"从字典删除{oldId}的过程中出错");
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
            modConfig.Error($"尝试获取对局结束数据时, 获取到的数据({nameof(pmcData.Stats)}和{nameof(pmcData.Stats.Eft)})全部为null");
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
        UpdateItemsChanged(raidInfo.Addition,
        raidInfo.Remove,
        raidInfo.Changed,
        raidInfo.ItemsTakeIn,
        raidInfo.ItemsTakeOut);
        // 收益, 战损记录
        raidInfo.GrossProfit = itemUtil.CalculateInventoryValue(raidInfo.ItemsTakeOut, raidInfo.Addition.ToArray());
        raidInfo.CombatLosses = itemUtil.CalculateInventoryValue(raidInfo.ItemsTakeIn, raidInfo.Remove.ToArray());
        foreach ((MongoId itemId, Item oldItem) in DataUtil.GetSubDict(raidInfo.ItemsTakeIn, raidInfo.Changed))
        {
            double oldValue = priceSystem.GetItemValueWithCache(oldItem);
            if (raidInfo.ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                double newValue = priceSystem.GetItemValueWithCache(newItem);
                if (!(Math.Abs(newValue - oldValue) > Constants.Epsilon)) continue;
                double delta = newValue - oldValue;
                if (delta > 0)
                    raidInfo.GrossProfit += Convert.ToInt64(delta);
                else
                    raidInfo.CombatLosses += Convert.ToInt64(-delta);
            }
            else
            {
                modConfig.Warn($"本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
        }
    }

    /// <summary>
    /// 重新计算Archive的收益, 战损等数据
    /// </summary>
    /// <returns>修复前后的 (收益变化量, 战损变化量)</returns>
    public (long grossProfitDelta, long combatLossesDelta)  ReCalculateArchive(RaidArchive archive)
    {
        long grossProfitOld = archive.GrossProfit, combatLossesOld = archive.CombatLosses;
        if (archive.ItemsTakeIn.Count == 0 && archive.ItemsTakeOut.Count == 0)
        {
            archive.GrossProfit = archive.CombatLosses = 0;
            return (grossProfitDelta: archive.GrossProfit - grossProfitOld, combatLossesDelta: archive.CombatLosses - combatLossesOld);
        }
        List<MongoId> addition = [], remove = [], change = [];
        UpdateItemsChanged(addition,
        remove,
        change,
        archive.ItemsTakeIn,
        archive.ItemsTakeOut);
        HashSet<MongoId> additionSet = [
                ..addition
            ],
            removeSet = [
                ..remove
            ];
        // 收益, 战损记录
        archive.GrossProfit = Convert.ToInt64(archive.ItemsTakeOut
            .Where(x => additionSet.Contains(x.Key))
            .Sum(x => priceSystem.GetItemValueWithCache(x.Key) * x.Value));
        archive.CombatLosses = Convert.ToInt64(archive.ItemsTakeIn
            .Where(x => removeSet.Contains(x.Key))
            .Sum(x => priceSystem.GetItemValueWithCache(x.Key) * x.Value));
        foreach ((MongoId itemId, double oldModify) in DataUtil.GetSubDict(archive.ItemsTakeIn, change))
        {
            double oldValue = priceSystem.GetItemValueWithCache(itemId) * oldModify;
            if (archive.ItemsTakeOut.TryGetValue(itemId, out double newModify))
            {
                double newValue = priceSystem.GetItemValueWithCache(itemId) * newModify;
                if (!(Math.Abs(newValue - oldValue) > Constants.Epsilon)) continue;
                double delta = newValue - oldValue;
                if (delta > 0)
                    archive.GrossProfit += Convert.ToInt64(delta);
                else
                    archive.CombatLosses += Convert.ToInt64(-delta);
            }
            else
            {
                modConfig.Warn($"本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
        }
        return (grossProfitDelta: archive.GrossProfit - grossProfitOld, combatLossesDelta: archive.CombatLosses - combatLossesOld);
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
                if (Math.Abs(modIn - modOut) < Constants.Epsilon)
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
                if (Math.Abs(modIn - modOut) < Constants.Epsilon)
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
}