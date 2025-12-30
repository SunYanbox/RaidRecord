using SPTarkov.Server.Core.Models.Common;

namespace RaidRecord.Core.Models.Tree;

public class TreeNode<T>
{
    public MongoId Id = new();
    public MongoId? ParentId = null;
    public required T Data;
    public required List<TreeNode<T>> Children;
}