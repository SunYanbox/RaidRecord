using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Services;
using RaidRecord.Core.Systems;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class PriceCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly I18N _i18N;
    private readonly PriceSystem _priceSystem;
    private readonly ItemHelper _itemHelper;
    private readonly DataGetterService _dataGetter;
    private readonly int _itemNameLen = "5422acb9af1c889c16000029 Name".Length;

    /// <summary>
    /// 用来缓存名称和id的映射关系, 只有调用过任意一次price命令后才会初始化
    /// </summary>
    private Dictionary<string, string>? _name2Id;

    public PriceCmd(CmdUtil cmdUtil, I18N i18N, ItemHelper itemHelper, PriceSystem priceSystem,
        DataGetterService dataGetter)
    {
        _cmdUtil = cmdUtil;
        _i18N = i18N;
        _priceSystem = priceSystem;
        _itemHelper = itemHelper;
        _dataGetter = dataGetter;
        Key = "price";
        Desc = i18N.GetText("Cmd-Price.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("tpl", "string", i18N.GetText("Cmd-参数简述.tpl"))
            .AddParam("name", "string", i18N.GetText("Cmd-参数简述.name"))
            .AddParam("top", "int", i18N.GetText("Cmd-参数简述.top"))
            .SetOptional(["tpl", "name", "top"])
            .Build();
    }

    private void InitName2Id()
    {
        if (_name2Id != null) return;
        _name2Id ??= new Dictionary<string, string>();
        Dictionary<string, string>? sptLocals = _dataGetter.GetSptLocals();
        if (sptLocals == null) return;
        foreach (KeyValuePair<string, string> kv in sptLocals.AsReadOnly()) // 不需要更改spt的数据库, 只读限定一下
        {
            if (kv.Key.EndsWith(" ShortName")
                || kv.Key.EndsWith(" Description")
                || kv.Key.Length != _itemNameLen)
                continue;
            string tpl = kv.Key.Replace(" Name", "").Replace(" name", "");
            if (tpl.Length != 24 || !_itemHelper.IsValidItem(tpl)) continue;
            int retryTimes = 0;
            while (retryTimes < 10)
            {
                // 后缀
                string retrySuffix = retryTimes > 0 ? $"_{retryTimes}" : "";
                if (_name2Id.TryAdd(kv.Value, $"{tpl}{retrySuffix}"))
                    break;
                retryTimes++;
            }
        }
    }

    public override string Execute(Parametric parametric)
    {
        InitName2Id();
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        string tpl = _cmdUtil.GetParameter(parametric.Paras, "tpl", string.Empty);
        string name = _cmdUtil.GetParameter(parametric.Paras, "name", string.Empty);
        int top = _cmdUtil.GetParameter(parametric.Paras, "top", 10);
        top = Math.Max(1, top);

        if (string.IsNullOrEmpty(tpl) && string.IsNullOrEmpty(name))
        {
            return _i18N.GetText("Cmd-Price.Error.缺少参数");
        }

        if (!string.IsNullOrEmpty(tpl))
        {
            if (!_itemHelper.IsValidItem(tpl))
                return _i18N.GetText("Cmd-Price.Error.无效的tpl", new { TplId = tpl });
            return _i18N.GetText("Cmd-Price.tpl结果", new
            {
                TplId = tpl,
                Name = _itemHelper.GetItemName(tpl),
                AvgPrice = _priceSystem.GetNewestPrice(tpl),
                DynPrice = _itemHelper.GetDynamicItemPrice(tpl),
                HandbookPrice = _itemHelper.GetStaticItemPrice(tpl)
            }) + "\n";
            // "Cmd-Price.tpl结果": "物品模板ID: {{TplId}} 物品名称: {{Name}} 物品市场平均单价: {{AvgPrice}}rub 动态价格: {{DynPrice}}rub 手册价格: {{HandbookPrice}}rub"
        }

        PriorityQueue<(string name, double similarity), double> pq = AlgorithmService.Search(name, _name2Id, top);

        string returnResult = "";
        while (pq.Count > 0)
        {
            (string nameResult, double similarityResult) = pq.Dequeue();
            // "Cmd-Price.name结果": "物品名称: {{Name}} 物品模板ID: {{TplId}} 物品市场平均单价: {{AvgPrice}}rub 动态价格: {{DynPrice}}rub 手册价格: {{HandbookPrice}}rub 相似得分: {{Similarity}}"
            string? tplResult = _name2Id?[nameResult];

            if (tplResult == null) continue;

            returnResult += _i18N.GetText("Cmd-Price.name结果", new
            {
                Name = nameResult,
                TplId = tplResult,
                AvgPrice = _priceSystem.GetNewestPrice(tplResult),
                DynPrice = _itemHelper.GetDynamicItemPrice(tplResult),
                HandbookPrice = _itemHelper.GetStaticItemPrice(tplResult),
                Similarity = $"{similarityResult:F4}"
            }) + "\n\n";
        }

        return returnResult;
    }
}