using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
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
    PaymentHelper paymentHelper,
    HandbookHelper handbookHelper,
    DatabaseService databaseService,
    RagfairOfferService ragfairOfferService)
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
    /// 使用类似 RagfairController.GetItemMinAvgMaxFleaPriceValues 的方式获取价格均值
    /// </summary>
    public double GetNewestPrice(MongoId itemId)
    {
        // Get all items of tpl
        IEnumerable<RagfairOffer>? offers = ragfairOfferService.GetOffersOfType(itemId);

        // Offers exist for item, get averages of what's listed
        if (offers != null)
        {
            RagfairOffer[] ragfairOffers = offers as RagfairOffer[] ?? offers.ToArray();
            if (ragfairOffers.Length != 0)
            {
                // These get calculated while iterating through the list below
                var minMax = new MinMax<double>(int.MaxValue, 0);

                // Get the average offer price, excluding barter offers
                double average = GetAveragePriceFromOffers(ragfairOffers, minMax, true);

                return Math.Round(average);
            }
        }

        // No offers listed, get price from live ragfair price list prices.json
        // No flea price, get handbook price
        Dictionary<MongoId, double> fleaPrices = databaseService.GetPrices();
        if (!fleaPrices.TryGetValue(itemId, out double tplPrice))
        {
            tplPrice = handbookHelper.GetTemplatePrice(itemId);
        }

        return tplPrice;
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
        // 刀, 安全箱(安全箱不能用parentId, 因为那个是所有容器的基类), 口袋可能很贵, 会影响入场价值
        if (item.SlotId is "SecuredContainer" or "Scabbard" or "Dogtag") return 0;
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

    /// <summary>
    /// 复制 RagfairController.GetAveragePriceFromOffers
    /// </summary>
    /// <param name="offers"></param>
    /// <param name="minMax"></param>
    /// <param name="ignoreTraderOffers"></param>
    /// <returns></returns>
    protected double GetAveragePriceFromOffers(IEnumerable<RagfairOffer> offers, MinMax<double> minMax, bool ignoreTraderOffers)
    {
        double sum = 0d;
        int totalOfferCount = 0;

        foreach (RagfairOffer offer in offers)
        {
            // Exclude barter items, they tend to have outrageous equivalent prices
            if (offer.Requirements!.Any(req => !paymentHelper.IsMoneyTpl(req.TemplateId)))
            {
                continue;
            }

            if (ignoreTraderOffers && offer.IsTraderOffer())
            {
                continue;
            }

            // Figure out how many items the requirementsCost is applying to, and what the per-item price is
            double offerItemCount = offer.SellInOnePiece.GetValueOrDefault(false) ? offer.Items?.First().Upd?.StackObjectsCount ?? 1 : 1;
            double? perItemPrice = offer.RequirementsCost / offerItemCount;

            // Handle min/max calculations based on the per-item price
            if (perItemPrice < minMax.Min)
            {
                minMax.Min = perItemPrice.Value;
            }
            else if (perItemPrice > minMax.Max)
            {
                minMax.Max = perItemPrice.Value;
            }

            sum += perItemPrice!.Value;
            totalOfferCount++;
        }

        if (totalOfferCount == 0)
        {
            return -1d;
        }

        return sum / totalOfferCount;
    }
}