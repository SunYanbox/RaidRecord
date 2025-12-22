using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ItemsCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly DatabaseService _databaseService;
    private readonly ItemHelper _itemHelper;
    private readonly I18N _i18N;

    public ItemsCmd(CmdUtil cmdUtil,
        DatabaseService databaseService,
        I18N i18N,
        ItemHelper itemHelper)
    {
        _cmdUtil = cmdUtil;
        _databaseService = databaseService;
        _itemHelper = itemHelper;
        Key = "items";
        Desc = i18N.GetText("Cmd-Items.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", i18N.GetText("Cmd-参数简述.index"))
            .AddParam("mode", "string", i18N.GetText("Cmd-参数化简述.Items.mode"))
            .SetOptional(["index", "mode"])
            .Build();
        _i18N = i18N;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = _cmdUtil.GetParameter(parametric.Paras, "index", -1);
        string mode = _cmdUtil.GetParameter<string>(parametric.Paras, "mode", "change");

        return GetItemsDetails(_cmdUtil.GetArchiveWithIndex(index, parametric.SessionId), mode);
    }

    protected string GetItemsDetails(RaidArchive archive, string mode)
    {
        string msg = "";

        msg += _cmdUtil.GetArchiveMetadata(archive);

        // Dictionary<MongoId, TemplateItem> itemTpls = databaseService.GetTables().Templates.Items;
        Dictionary<string, string>? sptLocal = _databaseService.GetTables().Locales.Global[_cmdUtil.I18N!.CurrentLanguage].Value;

        if (sptLocal == null) return "无法显示属性, 这是由于SPT的本地化数据库加载失败";

        if (mode == "all")
        {
            if (archive is { ItemsTakeIn.Count: > 0 })
            {
                // "\n\n带入对局物品:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
                msg += "\n"
                       + _i18N.GetText("Cmd-Items.All.带入物品标题")
                       + _i18N.GetText("Cmd-Items.物品表头");

                foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
                {
                    msg += $"\n\n - {GetItemDetails(tpl, modify, sptLocal)}";
                }
            }

            if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
            {
                msg += _i18N.GetText("Cmd-Items.All.带出物品标题");
                foreach ((MongoId tpl, double modify) in archive.ItemsTakeOut)
                {
                    msg += $"\n\n - {GetItemDetails(tpl, modify, sptLocal)}";
                }
            }

            return msg;
        }

        List<MongoId> add = [], remove = [], change = [];

        RaidUtil.UpdateItemsChanged(add, remove, change, archive.ItemsTakeIn, archive.ItemsTakeOut);

        // "\n\n物品变化:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
        msg += "\n" + _i18N.GetText("Cmd-Items.物品表头");

        msg += _i18N.GetText("Cmd-Items.Change.获得的物品");

        foreach (MongoId addTpl in add)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(addTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
                msg += $"\n + {GetItemDetails(addTpl, modify, sptLocal)}";
        }

        msg += _i18N.GetText("Cmd-Items.Change.丢失的物品");

        foreach (MongoId removeTpl in remove)
        {
            double modify = archive.ItemsTakeIn.GetValueOrDefault(removeTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
                msg += $"\n - {GetItemDetails(removeTpl, modify, sptLocal)}";
        }

        msg += _i18N.GetText("Cmd-Items.Change.变化的物品");

        foreach (MongoId changeTpl in change)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(changeTpl, 0)
                            - archive.ItemsTakeIn.GetValueOrDefault(changeTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
                msg += $"\n ~ {GetItemDetails(changeTpl, modify, sptLocal)}";
        }

        return msg;
    }

    private string GetItemDetails(MongoId tpl, double modify, Dictionary<string, string>? sptLocal = null)
    {
        double price = _itemHelper.GetItemPrice(tpl) ?? 0;
        string name = sptLocal?.GetValueOrDefault($"{tpl} ShortName", tpl) ?? tpl;
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