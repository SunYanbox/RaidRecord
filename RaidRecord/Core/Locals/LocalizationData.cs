using System.Text.Json.Serialization;

namespace RaidRecord.Core.Locals;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// 主本地化数据类
/// </summary>
public class LocalizationData
{
    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    [JsonPropertyName("armorZone")]
    public Dictionary<string, string> ArmorZone { get; set; } = new Dictionary<string, string>();
    [JsonPropertyName("roleNames")]
    public Dictionary<string, string> RoleNames { get; set; } = new Dictionary<string, string>();
}