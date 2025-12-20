using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Json;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class InfoCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly DatabaseService _databaseService;

    public InfoCmd(CmdUtil cmdUtil, DatabaseService databaseService)
    {
        _cmdUtil = cmdUtil;
        _databaseService = databaseService;
        Key = "info";
        Desc = cmdUtil.GetLocalText("Command.Info.Desc");
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

        if (!string.IsNullOrEmpty(serverId))
        {
            List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
            RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
            if (record != null)
            {
                return GetArchiveDetails(record);
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
            return GetArchiveDetails(records[index]);
        }
        List<RaidArchive> records2 = _cmdUtil.GetArchivesBySession(parametric.SessionId);
        // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
        return _cmdUtil.GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
    }

    private string GetArchiveDetails(RaidArchive archive)
    {
        string msg = "";
        string serverId = archive.ServerId;
        string playerId = archive.PlayerId;

        PmcData playerData = _cmdUtil.RecordCacheManager!.GetPmcDataByPlayerId(playerId);
        // 本次对局元数据
        string timeString = _cmdUtil.DateFormatterFull(archive.CreateTime);
        string mapName = serverId[..serverId.IndexOf('.')].ToLower();

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info0",
            timeString, serverId, playerData.Info?.Nickname,
            playerData.Info?.Level, playerData.Id);

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info1",
            _cmdUtil.LocalizationManager!.GetMapName(mapName), StringUtil.TimeString(archive.Results?.PlayTime ?? 0));

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info2",
            (int)archive.EquipmentValue, (int)archive.SecuredValue, (int)archive.PreRaidValue);

        msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info3",
            (int)archive.GrossProfit,
            (int)archive.CombatLosses,
            (int)(archive.GrossProfit - archive.CombatLosses));

        string result = _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknowResult");

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

        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];
        LazyLoad<Dictionary<string, string>> localTemps = _databaseService.GetTables().Locales.Global[_cmdUtil.LocalizationManager.CurrentLanguage];
        Dictionary<string, string>? locals = localTemps.Value;

        if (victims.Count > 0)
        {
            msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.killed");
            foreach (Victim victim in victims)
            {
                string weapon;
                if (locals != null && victim.Weapon != null)
                {
                    weapon = locals.TryGetValue(victim.Weapon, out string? value1)
                        ? value1
                        : victim.Weapon ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }
                else
                {
                    weapon = victim.Weapon ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }


                msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info5",
                    victim.Time,
                    weapon,
                    _cmdUtil.LocalizationManager.GetArmorZoneName(victim.BodyPart ?? ""),
                    (int)(victim.Distance ?? 0),
                    victim.Name,
                    victim.Level,
                    victim.Side,
                    _cmdUtil.LocalizationManager.GetRoleName(victim.Role ?? ""));
                // Constants.RoleNames.TryGetValue(victim.Role,  out var value3) ? value3 : victim.Role);
            }
        }

        if (archive.Results?.Result != ExitStatus.KILLED) return msg;
        {
            Aggressor? aggressor = archive.EftStats?.Aggressor;
            if (aggressor != null)
            {
                string weapon;
                if (locals != null && aggressor.WeaponName != null)
                {
                    weapon = locals.TryGetValue(aggressor.WeaponName, out string? value1)
                        ? value1
                        : aggressor.WeaponName ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }
                else
                {
                    weapon = aggressor.WeaponName ?? _cmdUtil.GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }

                msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.info6",
                    aggressor.Name,
                    aggressor.Side,
                    weapon,
                    // Constants.RoleNames.TryGetValue(aggressor.Role,  out var value3) ? value3 : aggressor.Role);
                    _cmdUtil.LocalizationManager.GetRoleName(aggressor.Role ?? ""));
            }
            else
            {
                msg += _cmdUtil.GetLocalText("RC MC.Chat.GAD.killedLoadError");
            }
        }

        return msg;
    }
}