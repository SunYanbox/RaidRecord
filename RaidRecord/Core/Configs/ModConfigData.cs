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
    /// 在LocalizationManager加载完毕后卸载除了当前语言和ch的其他语言数据, 节省内存
    /// </summary>
    [JsonPropertyName("autoUnloadOtherLanguages")]
    public bool AutoUnloadOtherLanguages { get; set; } = true;
}