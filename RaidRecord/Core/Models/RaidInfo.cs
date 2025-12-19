using System.Text.Json.Serialization;
using RaidRecord.Core.Utils;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace RaidRecord.Core.Models;

public record RaidInfo
{
    // 对局ID
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;
    // 玩家ID
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
    // 对局创建时间
    [JsonPropertyName("createTime")] public long CreateTime { get; set; }
    // 存档状态
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    // 玩家阵营(PMC, SCAV)
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    // 带入物品
    [JsonPropertyName("itemsTakeIn")]
    public Dictionary<MongoId, Item> ItemsTakeIn { get; set; } = new();
    // 带出物品
    [JsonPropertyName("itemsTakeOut")]
    public Dictionary<MongoId, Item> ItemsTakeOut { get; set; } = new();
    // 对局结束后带出的物品
    [JsonPropertyName("addition")] public List<MongoId> Addition { get; set; } = [];
    // 对局结束后移除的物品
    [JsonPropertyName("remove")] public List<MongoId> Remove { get; set; } = [];
    // 对局结束后变化的物品
    [JsonPropertyName("changed")] public List<MongoId> Changed { get; set; } = [];
    // 入场价值
    [JsonPropertyName("preRaidValue")] public long PreRaidValue { get; set; }
    // 装备价值
    [JsonPropertyName("equipmentValue")] public long EquipmentValue { get; set; }
    // 安全箱价值
    [JsonPropertyName("securedValue")] public long SecuredValue { get; set; }
    // 毛收益
    [JsonPropertyName("grossProfit")] public long GrossProfit { get; set; }
    // 战损
    [JsonPropertyName("combatLosses")] public long CombatLosses { get; set; }
    // 战局结果状态(包括详细击杀数据
    [JsonPropertyName("eftStats")] public EftStats? EftStats { get; set; }
    // 突袭实际结果
    [JsonPropertyName("results")] public RaidResultData? Results { get; set; }

    /**
     * 根据ID变换信息, 更新IRaidInfo
     * 将更新ItemsTakeIn, ItemsTakeOut字典
     * 与Addition, Remove, Changed列表的与id有关字段
     * @param replaceInfo { 旧ID -> 新ID }
     */
    public void UpdateByReplaceIDs(Dictionary<MongoId, MongoId> replaceInfo)
    {
        foreach (Dictionary<MongoId, Item> map in new[] { ItemsTakeIn, ItemsTakeOut })
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

        List<MongoId>[] lists = [Addition, Remove, Changed];
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

    // 根据开局请求初始化数据
    public void HandleRaidStart(string serverId, MongoId sessionId, ItemHelper itemHelper, ProfileHelper profileHelper)
    {
        ServerId = serverId;
        State = "未归档";
        Side = ServerId.Contains("Pmc") ? "Pmc" : "Savage";
        PmcData? pmcProfile = profileHelper.GetPmcProfile(sessionId);
        PlayerId = pmcProfile?.Id ?? throw new Exception("profileHelper.GetPmcProfile(sessionId).Id为null; 这可能是session已失效, 存档文件损坏或者存档数据库被意外修改!!!");
        CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        ItemsTakeIn = ItemUtil.GetInventoryInfo(pmcProfile, itemHelper);
        // Console.WriteLine($"获取到的物品:");
        // foreach (var item in ItemsTakeIn.Values)
        // {
        //     Console.WriteLine($"\t{item}");
        // }

        Item[] itemsTakeIn = ItemsTakeIn.Values.ToArray();
        PreRaidValue = ItemUtil.GetItemsValueAll(itemsTakeIn, itemHelper);
        EquipmentValue = ItemUtil.GetItemsValueWithBaseClasses(itemsTakeIn, Equipments, itemHelper);
        SecuredValue = ItemUtil.GetItemsValueAll(ItemUtil.GetAllItemsInContainer("SecuredContainer", itemsTakeIn), itemHelper);
        // Console.WriteLine($"itemsTakeIn.Length: {itemsTakeIn.Length}\n\tPreRaidValue: {PreRaidValue}\n\tEquipmentValue: {EquipmentValue}\n\tSecuredValue: {SecuredValue}");
    }

    // 根据结束请求载入数据
    public void HandleRaidEnd(EndLocalRaidRequestData data, MongoId sessionId, ItemHelper itemHelper,
        ProfileHelper profileHelper)
    {
        PmcData? pmcProfile = profileHelper.GetPmcProfile(sessionId);
        if (PlayerId != pmcProfile?.Id)
        {
            Console.WriteLine($"[RaidRecord] 错误: 尝试修改不属于{pmcProfile?.Id}的对局数据");
            return;
        }
        ItemsTakeOut = ItemUtil.GetInventoryInfo(pmcProfile, itemHelper);
        HandleRaidEndInventoryAndValue(pmcProfile, itemHelper);

        if (data == null) throw new Exception("HandleRaidEnd的参数data意外为null");

        if (data.Results == null) return;
        Results = new RaidResultData
        {
            Result = data.Results.Result,
            KillerId = data.Results.KillerId,
            KillerAid = data.Results.KillerAid,
            ExitName = data.Results.ExitName,
            PlayTime = Convert.ToInt64(data.Results.PlayTime)
        };
    }

    /// <summary>
    /// 根据对局结束的数据(变化量, 结果)归档到本RaidInfo
    /// </summary>
    private void HandleRaidEndInventoryAndValue(PmcData pmcData, ItemHelper itemHelper)
    {
        Addition.Clear();
        Remove.Clear();
        Changed.Clear();
        if (pmcData.Stats == null || pmcData.Stats.Eft == null)
        {
            Console.WriteLine("[RaidInfo] 错误尝试获取对局结束数据时, 获取到的数据全部为null");
            return;
        }
        State = State == "推测对局" ? "推测对局" : "已归档";
        // 处理对局结果
        // var resultStats = Utils.Copy(pmcData.Stats.Eft);
        EftStats = pmcData.Stats.Eft with
        {
            SessionCounters = null,
            OverallCounters = null,
            DroppedItems = null,
            DamageHistory = null
        };
        // 处理价值相关数据
        if (ItemsTakeIn.Count == 0 && ItemsTakeOut.Count == 0)
        {
            PreRaidValue = EquipmentValue = SecuredValue = GrossProfit = CombatLosses = 0;
            return;
        }
        // 记录获取/变化的物资
        foreach ((MongoId itemId, Item item) in ItemsTakeIn)
        {
            if (ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                if (Math.Abs(itemHelper.GetItemQualityModifier(item) - itemHelper.GetItemQualityModifier(newItem)) < 1e-6) continue;
                Changed.Add(itemId);
            }
            else
            {
                Remove.Add(itemId);
            }
        }
        foreach ((MongoId itemId, Item _) in ItemsTakeOut)
        {
            if (!ItemsTakeIn.ContainsKey(itemId)) Addition.Add(itemId);
        }
        // 收益, 战损记录
        GrossProfit = ItemUtil.CalculateInventoryValue(ItemsTakeOut, Addition.ToArray(), itemHelper);
        CombatLosses = ItemUtil.CalculateInventoryValue(ItemsTakeIn, Remove.ToArray(), itemHelper);
        foreach ((MongoId itemId, Item oldItem) in DataUtil.GetSubDict(ItemsTakeIn, Changed))
        {
            long oldValue = ItemUtil.GetItemValue(oldItem, itemHelper);
            if (ItemsTakeOut.TryGetValue(itemId, out Item? newItem))
            {
                long newValue = ItemUtil.GetItemValue(newItem, itemHelper);
                if (newValue > oldValue) GrossProfit += newValue - oldValue;
                else CombatLosses += oldValue - newValue;
            }
            else
            {
                Console.WriteLine($"[RaidRecord] 警告: 本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
        }
    }

    // 被视为战备的基类(枪械, 胸挂, 背包, 护甲, 头盔等)
    [JsonIgnore]
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