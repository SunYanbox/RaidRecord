using MudBlazor;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Models.Tree;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RaidRecord.Core.Services;

/// <summary>
/// 算法相关服务
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class AlgorithmService(I18N i18N)
{
    public static readonly string[] HeadshotBodyPart = ["Head", "Ears", "Eyes"];
    /// <summary>
    /// 判断命中区域是否为爆头击杀
    /// </summary>
    public static bool IsBodyPartHeadshotKill(string? bodyPart)
    {
        return !string.IsNullOrEmpty(bodyPart) && HeadshotBodyPart.Any(bodyPart.Contains);
    }

    /// <summary>
    /// 在字典中搜索与query最相近的topN个结果
    /// <br />
    /// 最小堆，保存相似度最低的项在顶部
    /// </summary>
    public static PriorityQueue<(string name, double similarity), double>
        Search(string query, Dictionary<string, string>? targetDict, int topN = 10)
    {
        var pq = new PriorityQueue<(string name, double similarity), double>();

        if (targetDict == null || targetDict.Count == 0) return pq;

        foreach (KeyValuePair<string, string> kv in targetDict.AsReadOnly())
        {
            double similarity = JacquardSimilarityNGram(query, kv.Key);

            string kvKeyLower = kv.Key.ToLower();
            string nameLower = query.ToLower();

            if (kvKeyLower.Contains(nameLower) || nameLower.Contains(kvKeyLower))
            {
                if (kv.Key == query)
                {
                    similarity += 1; // 完全相等，最高加分
                }
                else if (kv.Key.Contains(query))
                {
                    similarity += 0.75; // 完全包含，加高
                }
                else if (query.Contains(kv.Key))
                {
                    similarity += 0.5;
                }
                else if (kvKeyLower.Contains(nameLower))
                {
                    similarity += 0.25; // 大小写不敏感包含，加高
                }
                else if (nameLower.Contains(kvKeyLower))
                {
                    similarity += 0.1;
                }
                similarity += 0.05;
            }

            if (pq.Count < topN)
            {
                pq.Enqueue((kv.Key, similarity), similarity);
            }
            else if (similarity > pq.Peek().similarity)
            {
                pq.Dequeue(); // 移除相似度最低的
                pq.Enqueue((kv.Key, similarity), similarity);
            }
        }

        return pq;
    }

    /// <summary>
    /// 构建泛型树
    /// </summary>
    /// <param name="item">根物品</param>
    /// <param name="items">所有物品</param>
    /// <param name="getId">获取物品ID的函数</param>
    /// <param name="judgeIsParent">判断目标物品是否为父物品的子物品的函数</param>
    /// <param name="nodeMap">ID->每一个节点的映射</param>
    /// <param name="depth">树深度</param>
    /// <param name="maxDepth">最大树深度</param>
    /// <typeparam name="T">树节点的数据类型</typeparam>
    /// <returns></returns>
    public TreeNode<T>? BuildTree<T>(T item, T[] items, 
        Func<T, MongoId> getId, 
        Func<(T father, T target), bool> judgeIsParent,
        Dictionary<MongoId, TreeNode<T>> nodeMap, int depth = 0, int maxDepth = 15)
    {
        if (depth >= maxDepth) return null;
        MongoId id = getId(item);
        TreeNode<T> node = new() {
            Id = id,
            ParentId = null,
            Data = item,
            Children = []
        };
        
        nodeMap[id] = node;

        List<T> children = items.Where(x => judgeIsParent((item, x))).ToList();

        foreach (T child in children)
        {
            TreeNode<T>? childNode = BuildTree(child, items, getId, judgeIsParent, nodeMap, depth + 1, maxDepth);
            if (childNode == null) continue;
            childNode.ParentId = id;
            node.Children.Add(childNode);
        }
        return node;
    }

    /// <summary> 将TreeNode树转换为TreeItemData树 </summary>
    public List<RRTreeItemData> ConvertToTreeItems(List<TreeNode<Item>> nodes) 
    {
        return nodes.Select(CreateTreeItem).ToList();

        RRTreeItemData CreateTreeItem(TreeNode<Item> node)
        {
            RRTreeItemData treeItem = new(node.Data.Id, node.Data.Template, i18N);
            if (node.Children.Count > 0)
            {
                treeItem.Children = node.Children.Select(CreateTreeItem).ToList();
            }
            
            return treeItem;
        }
    }
    
    /// <summary> 将Item[]构建为TreeNode树, 并获取维护的节点字典 </summary>
    public (List<TreeNode<Item>>, Dictionary<MongoId, TreeNode<Item>>) GetTreeItems(Item[] items)
    {
        if (items.Length == 0) return ([], []);
        List<Item> rootItems = GetRootItems(items);
        Dictionary<MongoId, TreeNode<Item>> nodeMap = [];

        List<TreeNode<Item>> treeItems = [];
        foreach (Item rootItem in rootItems)
        {
            TreeNode<Item>? node = BuildTree(rootItem, items, 
            i => i.Id, 
            pair 
                => pair.target.ParentId is { Length: 24 }
                   && pair.target.ParentId == pair.father.Id, 
            nodeMap);
            if (node == null) continue;
            treeItems.Add(node);
        }
        return (treeItems, nodeMap);
    }

    /// <summary> 获取Item[]中所有根物品 </summary>
    public static List<Item> GetRootItems(IEnumerable<Item> items)
    {
        Item[] enumerable = items as Item[] ?? items.ToArray();
        var itemIds = new HashSet<string>(enumerable.Select(i => i.Id.ToString()));
        var rootItems = new List<Item>();

        foreach (Item item in enumerable)
        {
            // 如果 ParentId 为空，或 ParentId 不在当前列表中，则是根物品
            if (string.IsNullOrEmpty(item.ParentId) || !itemIds.Contains(item.ParentId))
            {
                rootItems.Add(item);
            }
        }

        return rootItems;
    }

    /// <summary>
    /// n-gram的Jacquard计算相似度
    /// </summary>
    public static double JacquardSimilarityNGram(string str1, string str2, int n = 2)
    {
        HashSet<string> set1 = GetNGrams(str1, n).ToHashSet();
        HashSet<string> set2 = GetNGrams(str2, n).ToHashSet();

        int intersection = set1.Intersect(set2).Count();
        int union = set1.Union(set2).Count();

        return union == 0 ? 1.0 : (double)intersection / union;
    }

    private static IEnumerable<string> GetNGrams(string text, int n)
    {
        if (string.IsNullOrEmpty(text) || text.Length < n)
            return [];

        return Enumerable.Range(0, text.Length - n + 1)
            .Select(i => text.Substring(i, n));
    }
}