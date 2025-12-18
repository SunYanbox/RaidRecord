using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Enums;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RaidRecord.Core.Models;

public record RaidResultData 
{
    // 结果字符串
    [JsonPropertyName("result")]
    public ExitStatus? Result { get;  set; } = null;
    // 击杀玩家的ID
    [JsonPropertyName("killerId")] public string? KillerId { get; set; } = null;
    // 击杀玩家的名称
    [JsonPropertyName("killerAid")] public string? KillerAid { get; set; } = null;
    // 撤离点名称
    [JsonPropertyName("exitName")] public string? ExitName { get; set; } = null;
    // 本局游玩时间
    [JsonPropertyName("playTime")] public long PlayTime { get; set; } = 0;
}