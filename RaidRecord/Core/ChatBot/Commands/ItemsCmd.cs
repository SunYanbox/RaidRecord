using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ItemsCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly ItemHelper _itemHelper;
    private readonly I18NMgr _i18NMgr;
    private readonly DataGetterService _dataGetter;

    public ItemsCmd(CmdUtil cmdUtil,
        I18NMgr i18NMgr,
        ItemHelper itemHelper,
        DataGetterService dataGetter)
    {
        _cmdUtil = cmdUtil;
        _itemHelper = itemHelper;
        _dataGetter = dataGetter;
        _i18NMgr = i18NMgr;
        Key = "items";
        Desc = "z2serverMessage.Cmd-Items.Desc".Translate(_i18NMgr.I18N!);
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", "z3translations.Cmd-参数简述.index".Translate(_i18NMgr.I18N!))
            .AddParam("mode", "string", "z3translations.Cmd-参数化简述.Items.mode".Translate(_i18NMgr.I18N!))
            .AddParam("ge", "double", "z3translations.Cmd-参数化简述.Items.ge".Translate(_i18NMgr.I18N!))
            .AddParam("le", "double", "z3translations.Cmd-参数化简述.Items.le".Translate(_i18NMgr.I18N!))
            .SetOptional(["index", "mode", "ge", "le"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = _cmdUtil.GetParameter(parametric.Paras, "index", -1);
        var mode = _cmdUtil.GetParameter<string>(parametric.Paras, "mode", "change");
        var ge = _cmdUtil.GetParameter<double>(parametric.Paras, "ge", 0);
        double le = _cmdUtil.GetParameter(parametric.Paras, "le", double.MaxValue);

        return GetItemsDetails(
            _dataGetter.GetArchiveWithIndex(index, parametric.SessionId),
            mode, ge, le);
    }

    private bool ShouldSkip(MongoId tpl, double modify, double ge, double le)
    {
        ge = Math.Abs(ge);
        le = Math.Abs(le);
        if (ge > le) (ge, le) = (le, ge);
        double priceValue = Math.Abs((_itemHelper.GetItemPrice(tpl) ?? 0) * modify);
        return !(priceValue >= ge && priceValue <= le);
    }

    protected string GetItemsDetails(RaidArchive archive, string mode, double ge, double le)
    {
        string msg = "";

        msg += _cmdUtil.GetArchiveMetadata(archive);

        // Dictionary<MongoId, TemplateItem> itemTpls = databaseService.GetTables().Templates.Items;
        Dictionary<string, string>? sptLocal = _i18NMgr.I18N?.SptLocals;

        if (sptLocal == null) return "无法显示属性, 这是由于SPT的本地化数据库加载失败";

        if (mode == "all")
        {
            if (archive is { ItemsTakeIn.Count: > 0 })
            {
                // "\n\n带入对局物品:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
                msg += "\n"
                       + "z2serverMessage.Cmd-Items.All.带入物品标题".Translate(_i18NMgr.I18N!)
                       + "z2serverMessage.Cmd-Items.物品表头".Translate(_i18NMgr.I18N!);

                foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
                {
                    if (ShouldSkip(tpl, modify, ge, le)) continue;
                    msg += $"\n\n - {GetItemDetails(tpl, modify, sptLocal)}";
                }
            }

            if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
            {
                msg += "z2serverMessage.Cmd-Items.All.带出物品标题".Translate(_i18NMgr.I18N!);
                foreach ((MongoId tpl, double modify) in archive.ItemsTakeOut)
                {
                    if (ShouldSkip(tpl, modify, ge, le)) continue;
                    msg += $"\n\n - {GetItemDetails(tpl, modify, sptLocal)}";
                }
            }

            return msg;
        }

        List<MongoId> add = [], remove = [], change = [];

        RaidUtil.UpdateItemsChanged(add, remove, change, archive.ItemsTakeIn, archive.ItemsTakeOut);

        // "\n\n物品变化:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
        msg += "\n" + "z2serverMessage.Cmd-Items.物品表头".Translate(_i18NMgr.I18N!);

        msg += "z2serverMessage.Cmd-Items.Change.获得的物品".Translate(_i18NMgr.I18N!);

        foreach (MongoId addTpl in add)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(addTpl, 0);
            if (!(Math.Abs(modify) > Constants.Epsilon)) continue;
            if (ShouldSkip(addTpl, modify, ge, le)) continue;
            msg += $"\n + {GetItemDetails(addTpl, modify, sptLocal)}";
        }

        msg += "z2serverMessage.Cmd-Items.Change.丢失的物品".Translate(_i18NMgr.I18N!);

        foreach (MongoId removeTpl in remove)
        {
            double modify = archive.ItemsTakeIn.GetValueOrDefault(removeTpl, 0);
            if (!(Math.Abs(modify) > Constants.Epsilon)) continue;
            if (ShouldSkip(removeTpl, modify, ge, le)) continue;
            msg += $"\n - {GetItemDetails(removeTpl, modify, sptLocal)}";
        }

        msg += "z2serverMessage.Cmd-Items.Change.变化的物品".Translate(_i18NMgr.I18N!);

        foreach (MongoId changeTpl in change)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(changeTpl, 0)
                            - archive.ItemsTakeIn.GetValueOrDefault(changeTpl, 0);
            if (!(Math.Abs(modify) > Constants.Epsilon)) continue;
            if (ShouldSkip(changeTpl, modify, ge, le)) continue;
            msg += $"\n ~ {GetItemDetails(changeTpl, modify, sptLocal)}";
        }

        return msg;
    }

    private string GetItemDetails(MongoId tpl, double modify, Dictionary<string, string>? sptLocal = null)
    {
        double price = _itemHelper.GetItemPrice(tpl) ?? 0;
        string name = sptLocal?.GetValueOrDefault($"{tpl} Name", tpl) ?? tpl;
        string desc = sptLocal?.GetValueOrDefault($"{tpl} Description", tpl) ?? tpl;

        // 截断描述，最多显示 30 个字符（可调），避免撑开行高
        if (!string.IsNullOrEmpty(desc))
        {
            desc = desc.Length > 30 ? desc[..27] + "..." : desc;
        }
        else
        {
            desc = ""; // 空描述留空
        }

        // 格式化输出：使用固定宽度对齐，确保列整齐
        return $"{name,-14} {price,6:F0} * {modify,6:F2} = {price * modify,8:F0}   {desc}";
    }
}