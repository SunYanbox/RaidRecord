namespace RaidRecord.Core.Models.BaseModels;

/// <summary> 表示一个范围 </summary>
public class RangeTuple<T>(T left, T right) where T : IComparable<T>
{
    /// <summary> 范围的左边界 </summary>
    public T Left { get; set; } = left;
    /// <summary> 范围的右边界 </summary>
    public T Right { get; set; } = right;
}