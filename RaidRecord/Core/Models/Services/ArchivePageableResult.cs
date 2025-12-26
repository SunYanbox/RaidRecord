using RaidRecord.Core.Models.BaseModels;

namespace RaidRecord.Core.Models.Services;

public record ArchiveIndexed(RaidArchive Archive, int Index);

/// <summary> 以分页形式获取存档列表的结果 </summary>
public record ArchivePageableResult: IDataGetterResult
{
    /// <summary> 获取到的存档列表 </summary>
    public List<ArchiveIndexed> Archives { get; set; } = [];
    public List<string>? Errors { get; set; }
    /// <summary> 由于数据无效等原因跳过的数据条数 </summary>
    public int JumpData { get; set; }
    /// <summary> 获取到的数据索引范围 </summary>
    public RangeTuple<int>? IndexRange { get; set; }
    /// <summary> 数据对应的页码 </summary>
    public int Page { get; set; }
    /// <summary> 数据对应的最大页码 </summary>
    public int PageMax { get; set; }
}