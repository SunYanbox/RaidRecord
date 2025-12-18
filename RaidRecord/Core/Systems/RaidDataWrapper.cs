using System.Text.Json.Serialization;
using RaidRecord.Core.Models;
using SPTarkov.Server.Core.Helpers;

namespace RaidRecord.Core.Systems;

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

    public void Zip(ItemHelper itemHelper)
    {
        if (!IsInfo) return;
        Archive = new RaidArchive();
        Archive.Zip(Info!, itemHelper);
        Info = null;
    }
}