using System.Text.Json.Serialization;

namespace RaidRecord.Core.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public record ModConfigData
{
    /// <summary>
    /// 本地语言
    /// </summary>
    [JsonPropertyName("local")]
    public required string Local { get; set; }
    /// <summary>
    /// 模组日志位置
    /// </summary>
    [JsonPropertyName("logPath")]
    public required string LogPath { get; set; }
    /// <summary>
    /// 在I18NManager加载完毕后卸载除了当前语言和ch的其他语言数据, 节省内存
    /// </summary>
    [JsonPropertyName("autoUnloadOtherLang")]
    public bool AutoUnloadOtherLang { get; set; } = true;
    /// <summary>
    /// 价格缓存更新的最低时间
    /// </summary>
    [JsonPropertyName("priceCacheUpdateMinTime")]
    public long PriceCacheUpdateMinTime { get; set; } = 6 * 60 * 1000;
    /// <summary>
    /// 模组给的物资(在模组购买物品, 快速起装)是否是FIR(对局中发现)状态
    /// </summary>
    [JsonPropertyName("modGiveIsFIR")]
    public bool ModGiveIsFIR { get; set; } = true;
    /// <summary>
    /// 是否是暗色模式
    /// </summary>
    [JsonPropertyName("darkMode")]
    public bool DarkMode { get; set; } = true;
    /// <summary>
    /// 是否修复物品耐久
    /// </summary>
    [JsonPropertyName("isRepairDurability")]
    public bool IsRepairDurability { get; set; } = true;
}