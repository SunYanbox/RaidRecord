namespace RaidRecord.Core.Configs;

/// <summary>
/// 基础价格计算方式常量
/// </summary>
internal static class PriceModeEnum
{
    /// <summary>
    /// 仅手册价格
    /// </summary>
    public const string Handbook = "Handbook";
    
    /// <summary>
    /// 平均跳蚤价格
    /// </summary>
    public const string AvgRagfair = "AvgRagfair";
    
    /// <summary>
    /// 自动选择：Min(手册价格, 平均跳蚤价格)
    /// </summary>
    public const string Auto = "Auto";
}