using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RaidRecord.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class ItemUtil(ItemHelper itemHelper, PriceSystem priceSystem)
{
    /// <summary>
    /// 获取物资列表内所有物资的总价值(求和后再转long, 降低误差)
    /// </summary>
    public long GetItemsValueAll(Item[] items)
    {
        double value = 0;
        foreach (Item item in items.Where(i => i.ParentId != "68e2c9a23d4d3dc9e403545f"))
        {
            value += priceSystem.GetItemValueWithCache(item);
        }
        return Convert.ToInt64(value);
    }

    /// <summary>
    /// 计算库存inventory中所有id处于filter中物品价格
    /// </summary>
    public long CalculateInventoryValue(Dictionary<MongoId, Item> inventory, MongoId[] filter)
    {
        return GetItemsValueAll(
            inventory.Values.Where(x => filter.Contains(x.Id)).ToArray());
    }

    /// <summary>
    /// 获取具有提供的任何基类的所有物品列表
    /// </summary>
    /// <param name="items">物品列表</param>
    /// <param name="baseClasses">基类列表</param>
    /// <returns>物品列表</returns>
    public Item[] GetItemsWithBaseClasses(Item[] items, IEnumerable<MongoId> baseClasses)
    {
        return items.Where(x => itemHelper.IsOfBaseclasses(x.Template, baseClasses)).ToArray();
    }

    /// <summary>
    /// 计算具有提供的任何基类的所有物品价值
    /// </summary>
    /// <param name="items">物品列表</param>
    /// <param name="baseClasses">基类列表</param>
    /// <returns>物品价值</returns>
    public long GetItemsValueWithBaseClasses(Item[] items, IEnumerable<MongoId> baseClasses)
    {
        return Convert.ToInt64(GetItemsValueAll(GetItemsWithBaseClasses(items, baseClasses)));
    }

    /// <summary>
    /// 获取物品列表中所有处于指定槽位下的所有物品
    /// </summary>
    /// <param name="desiredContainerSlotId">所希望的容器槽ID</param>
    /// <param name="items">物品列表</param>
    /// <returns>指定容器槽内的所有物品</returns>
    public Item[] GetAllItemsInContainer(string desiredContainerSlotId, Item[] items)
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
    public Dictionary<MongoId, Item> GetInventoryInfo(PmcData pmcData)
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