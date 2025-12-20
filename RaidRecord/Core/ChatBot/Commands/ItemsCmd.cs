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
            .SetOptional(["serverId", "index"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        string serverId;
        int index;
        try
        {
            serverId = parametric.Paras.GetValueOrDefault("serverid", "");
            index = int.TryParse(parametric.Paras.GetValueOrDefault("index", "-1"), out int indexTemp) ? indexTemp : -1;
        }
        catch (Exception e)
        {
            // return $"参数解析时出现错误: {e.Message}";
            _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", _cmdUtil.GetLocalText("Command.Para.Parse.error0", e.Message));
            return _cmdUtil.GetLocalText("Command.Para.Parse.error0", e.Message);
        }

        // TODO: 显示新获得/遗失/更改的物品

        if (!string.IsNullOrEmpty(serverId))
        {
            List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
            RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
            if (record != null)
            {
                return GetItemsDetails(record);
            }
            else
            {
                // return $"serverId为{serverId}的对局不存在, 请检查你的输入";
                return _cmdUtil.GetLocalText("Command.Para.ServerId.NotExist", serverId);
            }
        }
        if (index >= 0)
        {
            List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
            // if (index >= records.Count) return $"索引{index}超出范围: [0, {records.Count})";
            if (index >= records.Count) return _cmdUtil.GetLocalText("Command.Para.Index.OutOfRange", index, records.Count);
            return GetItemsDetails(records[index]);
        }

        List<RaidArchive> records2 = _cmdUtil.GetArchivesBySession(parametric.SessionId);
        // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
        return _cmdUtil.GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
    }

    protected string GetItemsDetails(RaidArchive archive)
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

        if (archive is { ItemsTakeIn.Count: > 0 })
        {
            // "\n\n带入对局物品:\n   物品名称  物品单价  物品修正  物品总价值"
            msg += _cmdUtil.GetLocalText("RC MC.Chat.GID.info0");
            foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
            {
                // TemplateItem item = itemTpls[tpl];
                double price = _itemHelper.GetItemPrice(tpl) ?? 0;
                msg += $"\n\n - {local[$"{tpl} ShortName"]}  {price}  {modify}  {price * modify} {local[$"{tpl} Description"]}";
            }
        }

        if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
        {
            // "\n\n带出对局物品:\n   物品名称  物品单价  物品修正  物品总价值  物品描述"
            msg += _cmdUtil.GetLocalText("RC MC.Chat.GID.info1");

            foreach ((MongoId tpl, double modify) in archive.ItemsTakeOut)
            {
                // TemplateItem item = itemTpls[tpl];
                double price = _itemHelper.GetItemPrice(tpl) ?? 0;
                msg += $"\n\n - {local[$"{tpl} ShortName"]}  {price}  {modify}  {price * modify}  {local[$"{tpl} Description"]}";
            }
        }

        return msg;
    }
}