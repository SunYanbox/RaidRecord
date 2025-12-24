using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Helpers;

namespace RaidRecord.Core.Models;

public class RaidDataWrapper
{
    // 缓存
    [JsonPropertyName("info")]
    public RaidInfo? Info { get; set; }
    // 存档
    [JsonPropertyName("archive")]
    public RaidArchive? Archive { get; set; }

    public bool IsInfo => Info != null;
    public bool IsArchive => Archive != null;
}