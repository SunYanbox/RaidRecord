using System.Text.Json.Serialization;
using SPTarkov.Common.Extensions;
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
    [JsonIgnore]
    private Dictionary<string, string>? _allLocalizationsCache;
    /// <summary>
    /// 所有本地化的缓存
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, string> AllLocalizations {
        get
        {
            if (_allLocalizationsCache != null) return _allLocalizationsCache;
            _allLocalizationsCache = new Dictionary<string, string>();
            foreach ((string key, string value) in Translations)
            {
                _allLocalizationsCache.Add(key, value);
            }
            foreach ((string key, string value) in ServerMessage)
            {
                _allLocalizationsCache.Add(key, value);
            }
            foreach ((string key, string value) in ArmorZone)
            {
                _allLocalizationsCache.Add(key, value);
            }
            foreach ((string key, string value) in RoleNames)
            {
                _allLocalizationsCache.Add(key, value);
            }
            return _allLocalizationsCache;
        }}
}