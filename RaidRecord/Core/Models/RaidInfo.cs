using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

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
}