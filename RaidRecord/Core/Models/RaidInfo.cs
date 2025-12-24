using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RaidRecord.Core.Models;

public record RaidInfo
{
    /// <summary> 对局ID </summary>
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;
    /// <summary> 玩家ID </summary>
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
    /// <summary> 创建时间 </summary>
    [JsonPropertyName("createTime")] public long CreateTime { get; set; }
    /// <summary> 存档状态 </summary>
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    /// <summary> 玩家阵营(PMC, SCAV) </summary>
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    /// <summary> 带入物品 </summary>
    [JsonPropertyName("itemsTakeIn")]
    public Dictionary<MongoId, Item> ItemsTakeIn { get; set; } = new();
    /// <summary> 带出物品 </summary>
    [JsonPropertyName("itemsTakeOut")]
    public Dictionary<MongoId, Item> ItemsTakeOut { get; set; } = new();
    /// <summary> 对局结束后带出的物品 </summary>
    [JsonPropertyName("addition")] public List<MongoId> Addition { get; set; } = [];
    /// <summary> 对局结束后移除的物品 </summary>
    [JsonPropertyName("remove")] public List<MongoId> Remove { get; set; } = [];
    /// <summary> 对局结束后变化的物品 </summary>
    [JsonPropertyName("changed")] public List<MongoId> Changed { get; set; } = [];
    /// <summary> 入场总价值 </summary>
    [JsonPropertyName("preRaidValue")] public long PreRaidValue { get; set; }
    /// <summary> 装备价值 | 战备 </summary>
    [JsonPropertyName("equipmentValue")] public long EquipmentValue { get; set; }
    /// <summary> 装备列表 | 战备 </summary>
    [JsonPropertyName("equipmentItems")] public Item[]? EquipmentItems { get; set; }
    /// <summary> 安全箱价值 </summary>
    [JsonPropertyName("securedValue")] public long SecuredValue { get; set; }
    /// <summary> 安全箱物资列表 </summary>
    [JsonPropertyName("securedItems")] public Item[]? SecuredItems { get; set; }
    /// <summary> 毛收益 </summary>
    [JsonPropertyName("grossProfit")] public long GrossProfit { get; set; }
    /// <summary> 战损 </summary>
    [JsonPropertyName("combatLosses")] public long CombatLosses { get; set; }
    /// <summary> 战局结果状态(包括详细击杀数据) </summary>
    [JsonPropertyName("eftStats")] public EftStats? EftStats { get; set; }
    /// <summary> 突袭实际结果 </summary>
    [JsonPropertyName("results")] public RaidResultData? Results { get; set; }
}