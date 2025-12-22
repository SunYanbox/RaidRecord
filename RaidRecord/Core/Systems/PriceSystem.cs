using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
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
    RagfairOfferService ragfairOfferService,
    I18N i18N,
    PaymentHelper paymentHelper,
    ItemHelper itemHelper)
{
    private readonly Dictionary<MongoId, PriceCache> _priceCache = new();

    /// <summary> 物品价值缓存 </summary>
    private class PriceCache
    {
        /// <summary> 物品价值 </summary>
        public double Price;

        /// <summary> 物品价值更新时间 / ms </summary>
        public long UpdateTime;
    }

    private double GetItemPrice(MongoId itemId)
    {
        double? price = itemHelper.GetItemPrice(itemId);

        if (price is > double.Epsilon) return price.Value;

        modConfig.Warn(i18N.GetText(
            "PriceSystem-Warn.ItemHelper.GetItemPrice.无法获取到价格",
            new { ItemId = itemId }));

        return 0;
    }

    /// <summary>
    /// 获取物品最新价值 | 报价中剔除异常值的三均值
    /// <br />
    /// 获取不到报价时返回Handbook或prices.json价格
    /// </summary>
    private double GetNewestPrice(MongoId itemId)
    {
        IEnumerable<RagfairOffer>? offers = ragfairOfferService.GetOffersOfType(itemId);
        IEnumerable<RagfairOffer> ragfairOffers = offers as RagfairOffer[] ?? (offers ?? []).ToArray();
        if (offers == null || !ragfairOffers.Any()) return GetItemPrice(itemId);
        // 筛选可计数的报价
        List<RagfairOffer> countableOffers =
        [
            .. ragfairOffers
                .Where(x => x.Requirements!.All(req => paymentHelper.IsMoneyTpl(req.TemplateId)) // 仅货币需求
                            && !x.IsTraderOffer() // 排除商人报价
                            && !x.IsPlayerOffer())
        ]; // 排除玩家报价

        if (countableOffers.Count <= 0) return GetItemPrice(itemId);
        List<double> offerPrices = [];

        foreach (RagfairOffer ragfairOffer in countableOffers)
        {
            Item firstItem = ragfairOffer.Items!.First();
            // 计算单件物品数量
            double itemCount = ragfairOffer.SellInOnePiece.GetValueOrDefault(false)
                ? firstItem.Upd?.StackObjectsCount
                  ?? 1
                : 1;

            // 计算单件价格
            double? perItemPrice = ragfairOffer.RequirementsCost / itemCount;
            if (perItemPrice is > double.Epsilon)
            {
                offerPrices.Add(perItemPrice.Value);
            }
        }

        return ProcessWithTrimmedTriMean(offerPrices.ToArray());
    }

    public double GetItemValue(MongoId itemId)
    {
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

    /// <summary>
    /// 获取物品价值(排除默认物品栏的容器, 如安全箱)
    /// </summary>
    public double GetItemValue(Item item)
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

        double price = GetItemValue(item.Template);

        // 修复了错误计算护甲值为0的物品的价值的问题
        return Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price));
    }

    /// <summary>
    /// 去除异常值, 并计算三均值
    /// </summary>
    public static double ProcessWithTrimmedTriMean(double[] data)
    {
        // 1. 快速排序（升序）
        double[] sorted = data.ToArray();
        Array.Sort(sorted);

        // 2. 计算 Q1, Median, Q3
        double q1 = Quantile(sorted, 0.25);
        double q3 = Quantile(sorted, 0.75);
        double iqr = q3 - q1;

        // 3. 去除异常值（IQR 方法）
        double lowerBound = q1 - 1.5 * iqr;
        double upperBound = q3 + 1.5 * iqr;
        double[] cleanedData = sorted.Where(x => x >= lowerBound && x <= upperBound).ToArray();

        if (cleanedData.Length == 0)
            return 0;

        // 4. 对清洗后的数据重新计算 Q1, Median, Q3（用于三均值）
        double newMedian = Quantile(cleanedData, 0.5);
        double newQ1 = Quantile(cleanedData, 0.25);
        double newQ3 = Quantile(cleanedData, 0.75);

        // 5. 计算三均值
        double triMean = (newQ1 + 2 * newMedian + newQ3) / 4.0;
        return triMean;
    }

    /// <summary>
    /// 辅助方法：计算任意分位数（0~1）
    /// </summary>
    public static double Quantile(double[] sortedData, double percentile)
    {
        if (percentile is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(percentile));

        int n = sortedData.Length;
        if (n == 1) return sortedData[0];

        double index = percentile * (n - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return sortedData[lower];

        double weight = index - lower;
        return sortedData[lower] * (1 - weight) + sortedData[upper] * weight;
    }
}