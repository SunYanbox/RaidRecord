using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RaidRecord.Core.Utils;

public static class ItemUtil
{
    /// <summary>
    /// 获取物品价值(排除默认物品栏的容器, 如安全箱)
    /// </summary>
    public static long GetItemValue(Item item, ItemHelper itemHelper)
    {
        // TODO: 更完善的无效物品判断
        // 刀, 安全箱(安全箱不能用parentId, 因为那个是所有容器的基类), 口袋可能很贵, 会影响入场价值
        if (item.SlotId is "SecuredContainer" or "Scabbard" or "Dogtag") return 0;
        // 父类是口袋的所有口袋
        HashSet<string> parentIds =
        [
            "557596e64bdc2dc2118b4571" // 口袋基类
        ];
        if (parentIds.Contains(item.ParentId ?? "")) return 0;

        double? price = itemHelper.GetItemPrice(item.Template);
        if (price == null)
        {
            Console.WriteLine($"[RaidRecord] Warning: {item.Template}没有价格");
            return 0;
        }
        // Console.WriteLine($"\t{item.Template}价格: {price.Value} "
        //                   + $"修正: {itemHelper.GetItemQualityModifier(item)} "
        //                   + $"返回值: {Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price.Value))}");
        // 修复了错误计算护甲值为0的物品的价值的问题
        return Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price.Value));
    }

    /// <summary>
    /// 获取物资列表内所有物资的总价值
    /// </summary>
    public static long GetItemsValueAll(Item[] items, ItemHelper itemHelper)
    {
        long value = 0;
        foreach (Item item in items.Where(i => i.ParentId != "68e2c9a23d4d3dc9e403545f"))
        {
            value += GetItemValue(item, itemHelper);
        }
        return value;
    }

    /// <summary>
    /// 计算库存inventory中所有id处于filter中物品价格
    /// </summary>
    public static long CalculateInventoryValue(Dictionary<MongoId, Item> inventory, MongoId[] filter, ItemHelper itemHelper)
    {
        return GetItemsValueAll(
            inventory.Values.Where(x => filter.Contains(x.Id)).ToArray(),
            itemHelper);
    }

    /// <summary>
    /// 计算具有提供的任何基类的所有物品价值
    /// </summary>
    /// <param name="items">物品列表</param>
    /// <param name="baseClasses">基类列表</param>
    /// <param name="itemHelper">物品助手</param>
    /// <returns>物品价值</returns>
    public static long GetItemsValueWithBaseClasses(Item[] items, IEnumerable<MongoId> baseClasses, ItemHelper itemHelper)
    {
        Item[] filteredItems = items.Where(x => itemHelper.IsOfBaseclasses(x.Template, baseClasses)).ToArray();
        return Convert.ToInt64(GetItemsValueAll(filteredItems, itemHelper));
    }

    /// <summary>
    /// 获取物品列表中所有处于指定槽位下的所有物品
    /// </summary>
    /// <param name="desiredContainerSlotId">所希望的容器槽ID</param>
    /// <param name="items">物品列表</param>
    /// <returns>指定容器槽内的所有物品</returns>
    public static Item[] GetAllItemsInContainer(string desiredContainerSlotId, Item[] items)
    {
        List<Item> containerItems = [];
        var pushTag = new HashSet<string>();

        foreach (Item item in items)
        {
            Item currentItem = item;

            // 递归向上查找父级
            while (currentItem.ParentId != null)
            {
                Item? parent = Array.Find(items, x => x.Id == currentItem.ParentId);
                // var parent = items.FirstOrDefault(i => i != null && i.Id == currentItem.ParentId, null);

                // 如果找不到父级，跳出循环
                if (parent == null) break;

                if (parent.SlotId == desiredContainerSlotId)
                {
                    if (!pushTag.Contains(item.Id))
                    {
                        containerItems.Add(item);
                        pushTag.Add(item.Id);
                    }
                    break;
                }

                currentItem = parent;
            }
        }

        return containerItems.ToArray();
    }



    /// <summary>
    /// 根据进入/离开突袭前后的仓库, 返回所有物品
    /// </summary>
    public static Dictionary<MongoId, Item> GetInventoryInfo(PmcData pmcData, ItemHelper itemHelper)
    {
        var result = new Dictionary<MongoId, Item>();
        if (pmcData.Inventory == null || pmcData.Inventory.Equipment == null || pmcData.Inventory.Items == null) return result;
        BotBaseInventory? inventory = pmcData.Inventory;

        // 物品信息在仓库内是正确的, 但是使用JSON序列化和反序列化后不正确了
        // Console.WriteLine(
        //         $"When GetInventoryInfo \n\tpmcData.Inventory: {inventory}"
        //     );
        // var itemData = inventory.Items.Select<Item, string[]>(x =>
        //     [x.Id.ToString(), x.Template.ToString(), x.ParentId != null ? x.ParentId.ToString() : null]).ToArray();
        // foreach (var idtplparent in itemData)
        // {
        //     Console.WriteLine($"\t {string.Join(", ", idtplparent)}");
        // }

        // 获取玩家进入/离开突袭时的所有物品
        // List<Item> aroundRaidItems = itemHelper.FindAndReturnChildrenByAssort(inventory.Equipment.Value, inventory.Items);
        List<Item> aroundRaidItems = inventory.Items.GetItemWithChildren(inventory.Equipment.Value);
        string copyError = "";
        // 转换为映射
        foreach (Item item in aroundRaidItems)
        {
            try
            {
                result[item.Id] = item with {};
                if (item.Template != result[item.Id].Template) throw new Exception($"record类型使用with复制构造改变了Template的MongoId!!!: {item.Template} -> {result[item.Id].Template}");
            }
            catch (Exception e)
            {
                copyError += $"物品{itemHelper.GetItem(item.Template).Value?.Properties?.Name ?? item.Template}复制构造失败({e.Message}), ";
            }
        }
        if (copyError.Length > 0)
        {
            Console.WriteLine($"[RaidRecord] GetInventoryInfo过程中出现问题: {copyError}");
        }
        return result;
    }
}