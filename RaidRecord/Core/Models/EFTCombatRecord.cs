using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace RaidRecord.Core.Models;

/// <summary> 账户历史战绩 </summary>
public class EFTCombatRecord
{
    /// <summary> 账户ID </summary>
    [JsonPropertyName("accountId")]
    public MongoId AccountId { get; set; }
    /// <summary> 战绩 </summary>
    [JsonPropertyName("records")]
    public List<RaidDataWrapper> Records { get; set; }
    /// <summary> 未归档的战绩缓存(开始游戏到结束游戏期间使用) </summary>
    [JsonPropertyName("infoRecordCache")]
    public RaidDataWrapper? InfoRecordCache { get; set; }

    #region 构造函数
    public EFTCombatRecord(MongoId accountId): this(accountId, []) {}

    /// <summary> 账户历史战绩 </summary>
    [JsonConstructor]
    public EFTCombatRecord(MongoId accountId, List<RaidDataWrapper> records, RaidDataWrapper? infoRecordCache = null)
    {
        AccountId = accountId;
        Records = records;
        InfoRecordCache = infoRecordCache;
    }
    #endregion

    #region 不参与序列化的属性
    /// <summary> 归档了的战绩 </summary>
    [JsonIgnore]
    public List<RaidDataWrapper> ArchivedRecords => Records.Where(x => x.IsArchive).ToList();
    #endregion
}