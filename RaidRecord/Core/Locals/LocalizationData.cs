using System.Text.Json.Serialization;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable once ClassNeverInstantiated.Global

namespace RaidRecord.Core.Locals;

/// <summary>
/// 主本地化数据类
/// </summary>
public class LocalizationData
{
    /// <summary>
    /// 负责服务端日志的本地化
    /// </summary>
    [JsonPropertyName("serverMessage")]
    public Dictionary<string, string> ServerMessage { get; set; } = new();
    /// <summary>
    /// 非日志的模组出现的文本的本地化
    /// </summary>
    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = new();
    /// <summary>
    /// 命中区域的本地化
    /// </summary>
    [JsonPropertyName("armorZone")]
    public Dictionary<string, string> ArmorZone { get; set; } = new();
    /// <summary>
    /// 角色名称
    /// </summary>
    [JsonPropertyName("roleNames")]
    public Dictionary<string, string> RoleNames { get; set; } = new();
}