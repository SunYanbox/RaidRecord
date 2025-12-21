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
        Desc = "使用序号或serverId获取详细对局记录(至少需要一个参数), 使用方式: \n";
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", "对局索引")
            .SetOptional(["index"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = CmdUtil.GetParameter(parametric.Paras, "index", -1);

        return GetArchiveDetails(_cmdUtil.GetArchiveWithIndex(index, parametric.SessionId));
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

        msg += $"{timeString} 对局ID: {serverId} 玩家信息: {playerData.Info?.Nickname}(Level={playerData.Info?.Level}, id={playerData.Id})";

        msg += $"\n地图: {_cmdUtil.LocalizationManager!.GetMapName(mapName)} 生存时间: {StringUtil.TimeString(archive.Results?.PlayTime ?? 0)}";

        msg += $"\n入局战备: {(int)archive.EquipmentValue}rub, 安全箱物资价值: {(int)archive.SecuredValue}rub, 总带入价值: {(int)archive.PreRaidValue}rub";

        msg += $"\n带出价值: {(int)archive.GrossProfit}rub, 战损{(int)archive.CombatLosses}rub, 净利润{(int)(archive.GrossProfit - archive.CombatLosses)}rub";

        string result = "未知结果";

        if (archive.Results?.Result != null)
        {
            result = _cmdUtil.LocalizationManager.GetText(archive.Results.Result.Value.ToString());
        }

        msg += $"\n对局结果: {result} 撤离点: {_cmdUtil.LocalizationManager.GetExitName(mapName, archive.Results?.ExitName ?? string.Format("RC MC.Chat.GAD.nullExitPosition"))} 游戏风格: {archive.EftStats?.SurvivorClass ?? "未知风格"}";

        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];
        LazyLoad<Dictionary<string, string>> localTemps = _databaseService.GetTables().Locales.Global[_cmdUtil.LocalizationManager.CurrentLanguage];
        Dictionary<string, string>? locals = localTemps.Value;

        if (victims.Count > 0)
        {
            msg += "\n本局击杀:";
            foreach (Victim victim in victims)
            {
                string weapon;
                if (locals != null && victim.Weapon != null)
                {
                    weapon = locals.TryGetValue(victim.Weapon, out string? value1)
                        ? value1
                        : victim.Weapon ?? "未知武器";
                }
                else
                {
                    weapon = victim.Weapon ?? "未知武器";
                }


                msg +=
                    $"\n {victim.Time} 使用{weapon}命中{_cmdUtil.LocalizationManager.GetArmorZoneName(victim.BodyPart ?? "")}淘汰距离{(int)(victim.Distance ?? 0)}m远的{victim.Name}(等级:{victim.Level} 阵营:{victim.Side} 角色:{_cmdUtil.LocalizationManager.GetRoleName(victim.Role ?? "")})";
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
                        : aggressor.WeaponName ?? "未知武器";
                }
                else
                {
                    weapon = aggressor.WeaponName ?? "未知武器";
                }

                msg += $"\n击杀者: {aggressor.Name}(阵营: {aggressor.Side})使用{weapon}命中{_cmdUtil.LocalizationManager.GetRoleName(aggressor.Role ?? "")}淘汰了你";
            }
            else
            {
                msg += "\n击杀者数据加载失败";
            }
        }

        return msg;
    }
}