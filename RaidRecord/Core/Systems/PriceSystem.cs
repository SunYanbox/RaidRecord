using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.Systems;

/// <summary>
/// 在模组中提供价值计算功能
/// </summary>
[Injectable(InjectionType.Singleton)]
public class PriceSystem(
    ModConfig modConfig,
    ItemHelper itemHelper,
    RagfairController ragfairController)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<MongoId, PriceCache> _priceCache = new();

    /// <summary> 物品价值缓存 </summary>
    private class PriceCache
    {
        /// <summary> 物品价值 </summary>
        public double Price;

        /// <summary> 物品价值更新时间 / ms </summary>
        public long UpdateTime;
    }

    /// <summary>
    /// 根据配置选择手册、平均市场价等方法计算价格
    /// </summary>
    public double GetNewestPrice(MongoId itemId)
    {
        double? handbookPrice = itemHelper.GetItemPrice(itemId);
        double? ragfairPrice = ragfairController.GetItemMinAvgMaxFleaPriceValues(new GetMarketPriceRequestData
        {
            TemplateId = itemId
        }).Avg;
        
        double? basePrice = modConfig.Configs.PriceMode switch
        {
            "Handbook" => handbookPrice,
            "AvgRagfair" => ragfairPrice,
            _ => GetMinValue(handbookPrice, ragfairPrice)  // 默认模式：取最小值
        };
        
        return (basePrice ?? 0) * 1.0;
        
        // 获取两个可空值中的最小值，如果其中一个为 null 则返回另一个，都为 null 则返回 null
        double? GetMinValue(double? a, double? b)
        {
            return a switch
            {
                null when b == null => null,
                null => b,
                _ => b == null ? a : Math.Min(a.Value, b.Value)
            };
        }
    }

    public double GetItemValueWithCache(MongoId itemId)
    {
        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.BUILT_IN_INSERTS))
            return 0;
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            if (_priceCache.TryGetValue(itemId, out PriceCache? priceCache))
            {
                if (now - priceCache.UpdateTime <= modConfig.Configs.PriceCacheUpdateMinTime)
                    return priceCache.Price;

                priceCache.Price = GetNewestPrice(itemId);
                priceCache.UpdateTime = now;
                return priceCache.Price;
            }

            PriceCache newPriceCache = new()
            {
                Price = GetNewestPrice(itemId),
                UpdateTime = now
            };

            _priceCache.Add(itemId, newPriceCache);
            return newPriceCache.Price;
        }
    }

    /// <summary>
    /// 基于缓存获取物品价值(排除默认物品栏的容器, 如安全箱)
    /// </summary>
    public double GetItemValueWithCache(Item item)
    {
        // TODO: 更完善的无效物品判断
        // 安全箱(安全箱不能用parentId, 因为那个是所有容器的基类), 口袋可能很贵, 会影响入场价值
        if (item.SlotId is "SecuredContainer" or "Dogtag") return 0;
        // 父类是口袋的所有口袋
        HashSet<string> parentIds =
        [
            "557596e64bdc2dc2118b4571" // 口袋基类
        ];
        if (parentIds.Contains(item.ParentId ?? "")) return 0;

        double price = GetItemValueWithCache(item.Template);

        // 修复了错误计算护甲值为0的物品的价值的问题
        return Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price));
    }
}