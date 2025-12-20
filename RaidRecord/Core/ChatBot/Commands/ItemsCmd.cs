using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ItemsCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly DatabaseService _databaseService;
    private readonly ItemHelper _itemHelper;

    public ItemsCmd(CmdUtil cmdUtil,
        DatabaseService databaseService,
        ItemHelper itemHelper)
    {
        _cmdUtil = cmdUtil;
        _databaseService = databaseService;
        _itemHelper = itemHelper;
        Key = "items";
        Desc = cmdUtil.GetLocalText("Command.Items.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("serverId", "string", cmdUtil.GetLocalText("Command.Para.ServerId.Desc"))
            .AddParam("index", "int", cmdUtil.GetLocalText("Command.Para.Index.Desc"))
            .AddParam("mode", "string", cmdUtil.GetLocalText("Command.Items.Para.Mode.Desc"))
            .SetOptional(["serverId", "index", "mode"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        string serverId = CmdUtil.GetParameter<string>(parametric.Paras, "serverId", "");
        int index = CmdUtil.GetParameter(parametric.Paras, "index", -1);
        string mode = CmdUtil.GetParameter<string>(parametric.Paras, "mode", "change");

        RaidArchive? archive = _cmdUtil.GetArchiveWithServerId(serverId, parametric.SessionId);

        // TODO: 显示新获得/遗失/更改的物品

        if (archive != null)
        {
            return GetItemsDetails(archive, mode);
        }
        return index == -1 
            ? _cmdUtil.GetLocalText("Command.Para.ServerId.NotExist", serverId) 
            : GetItemsDetails(_cmdUtil.GetArchiveWithIndex(index, parametric.SessionId), mode);
    }

    protected string GetItemsDetails(RaidArchive archive, string mode)
    {
        string msg = "";
        string serverId = archive.ServerId;
        string playerId = archive.PlayerId;

        PmcData playerData = _cmdUtil.RecordCacheManager!.GetPmcDataByPlayerId(playerId);

        // 本次对局元数据
        string timeString = _cmdUtil.DateFormatterFull(archive.CreateTime);
        string mapName = serverId[..serverId.IndexOf('.')].ToLower();

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info0",
            timeString, serverId, playerData.Info?.Nickname, playerData.Info?.Level,
            playerData.Id);

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info1",
            _cmdUtil.LocalizationManager!.GetMapName(mapName), StringUtil.TimeString(archive.Results?.PlayTime ?? 0));

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info2",
            (int)archive.EquipmentValue, (int)archive.SecuredValue, (int)archive.PreRaidValue);

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info3",
            (int)archive.GrossProfit,
            (int)archive.CombatLosses,
            (int)(archive.GrossProfit - archive.CombatLosses));

        string result = _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknow");

        if (archive.Results?.Result != null)
        {
            ExitStatus nonNullResult = archive.Results.Result.Value;
            if (Constants.ResultNames.TryGetValue(nonNullResult, out string? resultName))
            {
                result = _cmdUtil.LocalizationManager.GetText(resultName, resultName);
            }
        }

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info4",
            result,
            _cmdUtil.LocalizationManager.GetExitName(mapName, archive.Results?.ExitName ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.nullExitPosition")),
            archive.EftStats?.SurvivorClass ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknow"));

        // Dictionary<MongoId, TemplateItem> itemTpls = databaseService.GetTables().Templates.Items;
        Dictionary<string, string>? local = _databaseService.GetTables().Locales.Global[_cmdUtil.LocalizationManager.CurrentLanguage].Value;
        if (local == null) return _cmdUtil.GetLocalText("RC MC.Chat.GID.error0");

        if (mode == "all")
        {
            if (archive is { ItemsTakeIn.Count: > 0 })
            {
                // "\n\n带入对局物品:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
                msg += _cmdUtil.GetLocalText("RC MC.Chat.GID.info0");
                foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
                {
                    msg += $"\n\n - {GetItemDetails(tpl, modify, local)}";
                }
            }

            if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
            {
                foreach ((MongoId tpl, double modify) in archive.ItemsTakeOut)
                {
                    msg += $"\n\n - {GetItemDetails(tpl, modify, local)}";
                }
            }

            return msg;
        }

        List<MongoId> add = [], remove = [], change = [];
        
        RaidUtil.UpdateItemsChanged(add, remove, change, archive.ItemsTakeIn, archive.ItemsTakeOut);

        // "\n\n物品变化:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
        msg += _cmdUtil.GetLocalText("RC MC.Chat.GID.info1");
        
        foreach (MongoId addTpl in add)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(addTpl, 0);
            if (modify > Constants.ArchiveCheckJudgeError)
                msg += $"\n + {GetItemDetails(addTpl, modify, local)}";
        }

        foreach (MongoId removeTpl in remove)
        {
            double modify = archive.ItemsTakeIn.GetValueOrDefault(removeTpl, 0);
            if (modify > Constants.ArchiveCheckJudgeError)
                msg += $"\n - {GetItemDetails(removeTpl, modify, local)}";
        }
        
        foreach (MongoId changeTpl in change)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(changeTpl, 0)
                - archive.ItemsTakeIn.GetValueOrDefault(changeTpl, 0);
            if (modify > Constants.ArchiveCheckJudgeError)
                msg += $"\n ~ {GetItemDetails(changeTpl, modify, local)}";
        }

        return msg;
    }
    
    private string GetItemDetails(MongoId tpl, double modify, Dictionary<string, string>? local = null)
    {
        double price = _itemHelper.GetItemPrice(tpl) ?? 0;
        string name = local?.GetValueOrDefault($"{tpl} ShortName", tpl) ?? tpl;
        string desc = local?.GetValueOrDefault($"{tpl} Description", tpl) ?? tpl;

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