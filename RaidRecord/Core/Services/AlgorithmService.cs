using MudBlazor;
using RaidRecord.Core.Locals;
using SPTarkov.DI.Annotations;
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

    /// <summary> 将Item[]构建为TreeItemData树 </summary>
    public List<TreeItemData<string>> GetTreeItems(Item[] items)
    {
        if (items.Length == 0) return [];
        List<Item> rootItems = GetRootItems(items);

        List<TreeItemData<string>> treeItems = [];
        foreach (Item root in rootItems)
        {
            TreeItemData<string>? node = BuildTreeNode(root, 0, items, 15);
            if (node != null) treeItems.Add(node);
        }
        return treeItems;
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

    /// <summary>
    /// 递归构建树节点
    /// </summary>
    private TreeItemData<string>? BuildTreeNode(Item item, int depth, Item[] items, int maxDepth = 15)
    {
        if (depth >= maxDepth) return null;

        string value = item.Template.ToString();
        string text = i18N.GetItemName(item.Template);

        var node = new TreeItemData<string>
        {
            Value = value,
            Text = text,
            Children = []
        };

        List<Item> children = items.Where(x => x.ParentId != null && x.ParentId == item.Id).ToList();
        foreach (Item child in children)
        {
            TreeItemData<string>? childNode = BuildTreeNode(child, depth + 1, items, maxDepth);
            if (childNode != null)
            {
                node.Children?.Add(childNode);
            }
        }

        return node;
    }
}