using System.Text.Json.Serialization;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable once ClassNeverInstantiated.Global

namespace RaidRecord.Core.Locals;

/// <summary>
/// 主本地化数据类
/// </summary>
public class LocalizationData
{
    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = new();
    [JsonPropertyName("armorZone")]
    public Dictionary<string, string> ArmorZone { get; set; } = new();
    [JsonPropertyName("roleNames")]
    public Dictionary<string, string> RoleNames { get; set; } = new();
}