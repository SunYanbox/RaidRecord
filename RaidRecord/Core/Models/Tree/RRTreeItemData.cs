using MudBlazor;
using RaidRecord.Core.Locals;
using SPTarkov.Server.Core.Models.Common;

namespace RaidRecord.Core.Models.Tree;

/// <summary>
/// RaidRecord针对Item的TreeItemData
/// <remarks>扩展内容: 物品ID, 物品模板ID, Text初始化</remarks>
/// </summary>
public sealed class RRTreeItemData: TreeItemData<MongoId>
{
    public RRTreeItemData(MongoId itemId, MongoId tplId, I18N i18N)
    {
        ItemId = itemId;
        TplId = tplId;
        Text = i18N.GetItemName(tplId);
        Value = itemId;
    }
    /// <summary> 物品 id </summary>
    public MongoId ItemId;
    /// <summary> 物品模板 id </summary>
    public MongoId TplId;
    /// <summary> 覆盖Children属性 </summary>
    public new List<RRTreeItemData>? Children { get; set; }
}