using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RaidRecord.Core.Models;

public record RaidArchive
{
    // 对局ID
    [JsonPropertyName("serverId")] public string ServerId { get; set; } = string.Empty;
    // 玩家ID
    [JsonPropertyName("playerId")] public string PlayerId { get; set; } = string.Empty;
    // 对局创建时间
    [JsonPropertyName("createTime")] public long CreateTime { get; set; }
    // 存档状态
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    // 玩家阵营(PMC, SCAV)
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    // 带入物品
    [JsonPropertyName("itemsTakeIn")] public Dictionary<MongoId, double> ItemsTakeIn { get; set; } = new();
    // 带出物品
    [JsonPropertyName("itemsTakeOut")]
    public Dictionary<MongoId, double> ItemsTakeOut { get; set; } = new();
    // // 对局结束后带出的物品
    // [JsonPropertyName("addition")] public readonly Dictionary<MongoId, double> Addition = new Dictionary<MongoId, double> {};
    // // 对局结束后移除的物品
    // [JsonPropertyName("remove")] public readonly Dictionary<MongoId, double> Remove = new Dictionary<MongoId, double> {};
    // // 对局结束后变化的物品
    // [JsonPropertyName("changed")] public readonly Dictionary<MongoId, double> Changed = new Dictionary<MongoId, double> {};
    /// <summary> 入场总价值 </summary>
    [JsonPropertyName("preRaidValue")] public long PreRaidValue { get; set; }
    /// <summary> 装备价值 | 战备 </summary>
    [JsonPropertyName("equipmentValue")] public long EquipmentValue { get; set; }
    /// <summary> 装备列表 | 战备 </summary>
    [JsonPropertyName("equipmentItems")] public Item[]? EquipmentItems { get; set; }
    /// <summary> 安全箱价值 </summary>
    [JsonPropertyName("securedValue")] public long SecuredValue { get; set; }
    /// <summary> 安全箱物资列表 </summary>
    [JsonPropertyName("securedItems")] public Dictionary<MongoId, double>? SecuredItems { get; set; }
    /// <summary> 毛收益 </summary>
    [JsonPropertyName("grossProfit")] public long GrossProfit { get; set; }
    /// <summary> 战损 </summary>
    [JsonPropertyName("combatLosses")] public long CombatLosses { get; set; }
    /// <summary> 战局结果状态(包括详细击杀数据) </summary>
    [JsonPropertyName("eftStats")] public EftStats? EftStats { get; set; }
    /// <summary> 突袭实际结果 </summary>
    [JsonPropertyName("results")] public RaidResultData? Results { get; set; }
}