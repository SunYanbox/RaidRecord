using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
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
        Desc = "使用序号或serverId获取指定对局记录(至少需要一个参数)详细物品信息, 使用方式: \n";
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", "对局索引")
            .AddParam("mode", "string", "支持\"change\"(默认)或\"all\", 大小写不敏感\nchange只显示新增, 丢弃或变化的的物品数据; all是类似旧版本显示带入和带出的模式")
            .SetOptional(["index", "mode"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = CmdUtil.GetParameter(parametric.Paras, "index", -1);
        string mode = CmdUtil.GetParameter<string>(parametric.Paras, "mode", "change");

        return GetItemsDetails(_cmdUtil.GetArchiveWithIndex(index, parametric.SessionId), mode);
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

        msg += string.Format("{0} 对局ID: {1} 玩家信息: {2}(Level={3}, id={4})",
            timeString, serverId, playerData.Info?.Nickname, playerData.Info?.Level,
            playerData.Id);

        msg += string.Format("\n地图: {0} 生存时间: {1}",
            _cmdUtil.LocalizationManager!.GetMapName(mapName), StringUtil.TimeString(archive.Results?.PlayTime ?? 0));

        msg += string.Format("\n入局战备: {0}rub, 安全箱物资价值: {1}rub, 总带入价值: {2}rub",
            (int)archive.EquipmentValue, (int)archive.SecuredValue, (int)archive.PreRaidValue);

        msg += string.Format("\n带出价值: {0}rub, 战损{1}rub, 净利润{2}rub",
            (int)archive.GrossProfit,
            (int)archive.CombatLosses,
            (int)(archive.GrossProfit - archive.CombatLosses));

        string result = "未知";

        if (archive.Results?.Result != null)
        {
            result = _cmdUtil.LocalizationManager.GetText(archive.Results.Result.Value.ToString());
        }

        msg += string.Format("\n对局结果: {0} 撤离点: {1} 游戏风格: {2}",
            result,
            _cmdUtil.LocalizationManager.GetExitName(mapName, archive.Results?.ExitName ?? string.Format("RC MC.Chat.GAD.nullExitPosition")),
            archive.EftStats?.SurvivorClass ?? "未知");

        // Dictionary<MongoId, TemplateItem> itemTpls = databaseService.GetTables().Templates.Items;
        Dictionary<string, string>? local = _databaseService.GetTables().Locales.Global[_cmdUtil.LocalizationManager.CurrentLanguage].Value;
        if (local == null) return "无法显示属性, 这是由于SPT的本地化数据库加载失败";

        if (mode == "all")
        {
            if (archive is { ItemsTakeIn.Count: > 0 })
            {
                // "\n\n带入对局物品:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述"
                msg += "\n\n- - - - - - - - - - - - 带入对局物品- - - - - - - - - - - - \n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述";
                foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
                {
                    msg += $"\n\n - {GetItemDetails(tpl, modify, local)}";
                }
            }

            if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
            {
                msg += "\n- - - - - - - - - - - - 带出对局物品 - - - - - - - - - - - - ";
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
        msg += "\n\n物品变化:\n   物品名称  物品单价(rub) * 物品修正 = 物品总价值(rub)  物品描述";

        foreach (MongoId addTpl in add)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(addTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
                msg += $"\n + {GetItemDetails(addTpl, modify, local)}";
        }

        foreach (MongoId removeTpl in remove)
        {
            double modify = archive.ItemsTakeIn.GetValueOrDefault(removeTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
                msg += $"\n - {GetItemDetails(removeTpl, modify, local)}";
        }

        foreach (MongoId changeTpl in change)
        {
            double modify = archive.ItemsTakeOut.GetValueOrDefault(changeTpl, 0)
                            - archive.ItemsTakeIn.GetValueOrDefault(changeTpl, 0);
            if (Math.Abs(modify) > Constants.ArchiveCheckJudgeError)
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