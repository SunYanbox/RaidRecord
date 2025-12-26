namespace RaidRecord.Core.Models.Services;

public interface IDataGetterResult
{
    /// <summary> 出现的错误 </summary>
    List<string>? Errors { get; set; }
}