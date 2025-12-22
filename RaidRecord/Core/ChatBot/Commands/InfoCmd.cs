using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
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
    private readonly LocalizationManager _local;
    private readonly string _unknowWeapon;

    public InfoCmd(CmdUtil cmdUtil, DatabaseService databaseService, LocalizationManager local)
    {
        _cmdUtil = cmdUtil;
        _databaseService = databaseService;
        Key = "info";
        Desc = local.GetText("Cmd-Info.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", local.GetText("Cmd-参数简述.index"))
            .SetOptional(["index"])
            .Build();
        _unknowWeapon = local.GetText("UnknownWeapon");
        _local = local;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = _cmdUtil.GetParameter(parametric.Paras, "index", -1);

        return GetArchiveDetails(_cmdUtil.GetArchiveWithIndex(index, parametric.SessionId));
    }

    private string GetArchiveDetails(RaidArchive archive)
    {
        string msg = "";

        msg += _cmdUtil.GetArchiveMetadata(archive);

        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];
        LazyLoad<Dictionary<string, string>> localTemps = _databaseService.GetTables().Locales.Global[_cmdUtil.LocalizationManager!.CurrentLanguage];
        Dictionary<string, string>? locals = localTemps.Value;

        if (victims.Count > 0)
        {
            msg += _local.GetText("Cmd-Info.本局击杀标题");
            foreach (Victim victim in victims)
            {
                string weapon;
                if (locals != null && victim.Weapon != null)
                {
                    weapon = locals.TryGetValue(victim.Weapon, out string? value1)
                        ? value1
                        : victim.Weapon ?? _unknowWeapon;
                }
                else
                {
                    weapon = victim.Weapon ?? _unknowWeapon;
                }

                // "Cmd-Info.本局击杀信息": "\n {{VictimTime}} 使用{{WeaponName}}命中{{BodyPart}}淘汰距离{{VictimDistance}}m远的{{VictimName}}(等级:{{VictimLevel}} 阵营:{{VictimSide}} 角色:{{VictimRole}})",
                msg += _local.GetText(
                    "Cmd-Info.本局击杀信息",
                    new
                    {
                        VictimTime = victim.Time?[..13],
                        WeaponName = weapon,
                        BodyPart = _cmdUtil.LocalizationManager.GetArmorZoneName(victim.BodyPart ?? ""),
                        VictimDistance = (int)(victim.Distance ?? 0),
                        VictimName = victim.Name,
                        VictimLevel = victim.Level,
                        VictimSide = victim.Side,
                        VictimRole = _cmdUtil.LocalizationManager.GetRoleName(victim.Role ?? "")
                    }
                );
                // msg +=
                // $"\n {victim.Time} 使用{weapon}命中{_cmdUtil.LocalizationManager.GetArmorZoneName(victim.BodyPart ?? "")}淘汰距离{(int)(victim.Distance ?? 0)}m远的{victim.Name}(等级:{victim.Level} 阵营:{victim.Side} 角色:{_cmdUtil.LocalizationManager.GetRoleName(victim.Role ?? "")})";
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
                        : aggressor.WeaponName ?? _unknowWeapon;
                }
                else
                {
                    weapon = aggressor.WeaponName ?? _unknowWeapon;
                }

                // "Cmd-Info.本局击杀者": "\n击杀者: {{AggressorName}}(阵营: {{AggressorSide}})使用{{WeaponName}}命中{{ArmorZone}}淘汰了你",
                msg += _local.GetText(
                    "Cmd-Info.本局击杀者",
                    new
                    {
                        AggressorName = aggressor.Name,
                        AggressorSide = aggressor.Side,
                        WeaponName = weapon,
                        BodyPart = _cmdUtil.LocalizationManager.GetArmorZoneName(aggressor.BodyPart ?? _local.GetText("Unknown"))
                    }
                );
            }
            else
            {
                msg += _local.GetText("Cmd-Info.本局被击杀者信息加载失败");
                // msg += "\n击杀者数据加载失败";
            }
        }

        return msg;
    }
}