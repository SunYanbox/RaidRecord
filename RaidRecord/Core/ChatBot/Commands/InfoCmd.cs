using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class InfoCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly I18N _i18N;
    private readonly DataGetterService _dataGetter;
    private readonly DataFormatService _dataFormatService;

    public InfoCmd(CmdUtil cmdUtil, DataGetterService dataGetter, DataFormatService dataFormatService, I18N i18N)
    {
        _cmdUtil = cmdUtil;
        Key = "info";
        Desc = i18N.GetText("Cmd-Info.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", i18N.GetText("Cmd-参数简述.index"))
            .SetOptional(["index"])
            .Build();
        _i18N = i18N;
        _dataGetter = dataGetter;
        _dataFormatService = dataFormatService;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int index = _cmdUtil.GetParameter(parametric.Paras, "index", -1);

        return GetArchiveDetails(_dataGetter.GetArchiveWithIndex(index, parametric.SessionId));
    }

    private string GetArchiveDetails(RaidArchive archive)
    {
        string msg = "";

        msg += _cmdUtil.GetArchiveMetadata(archive);

        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];

        if (victims.Count > 0)
        {
            msg += _i18N.GetText("Cmd-Info.本局击杀标题");
            foreach (Victim victim in victims)
            {
                // "Cmd-Info.本局击杀信息": "\n {{VictimTime}} 使用{{WeaponName}}命中{{BodyPart}}淘汰距离{{VictimDistance}}m远的{{VictimName}}(等级:{{VictimLevel}} 阵营:{{VictimSide}} 角色:{{VictimRole}})",
                msg += _i18N.GetText(
                    "Cmd-Info.本局击杀信息",
                    new
                    {
                        VictimTime = _dataFormatService.GetVictimTime(victim),
                        WeaponName = _dataFormatService.GetWeaponName(victim),
                        BodyPart = _dataFormatService.GetBodyPartName(victim),
                        VictimDistance = _dataFormatService.GetDistance(victim),
                        VictimName = victim.Name,
                        VictimLevel = victim.Level,
                        VictimSide = victim.Side,
                        VictimRole = _dataFormatService.GetRoleName(victim)
                    }
                );
                // msg +=
                // $"\n {victim.Time} 使用{weapon}命中{_cmdUtil.I18n.GetArmorZoneName(victim.BodyPart ?? "")}淘汰距离{(int)(victim.Distance ?? 0)}m远的{victim.Name}(等级:{victim.Level} 阵营:{victim.Side} 角色:{_cmdUtil.I18n.GetRoleName(victim.Role ?? "")})";
                // Constants.RoleNames.TryGetValue(victim.Role,  out var value3) ? value3 : victim.Role);
            }
        }

        if (archive.Results?.Result != ExitStatus.KILLED) return msg;
        {
            Aggressor? aggressor = archive.EftStats?.Aggressor;
            if (aggressor != null)
            {
                // "Cmd-Info.本局击杀者": "\n击杀者: {{AggressorName}}(阵营: {{AggressorSide}})使用{{WeaponName}}命中{{ArmorZone}}淘汰了你",
                msg += _i18N.GetText(
                    "Cmd-Info.本局击杀者",
                    new
                    {
                        AggressorName = aggressor.Name,
                        AggressorSide = aggressor.Side,
                        WeaponName = _dataFormatService.GetWeaponName(aggressor),
                        BodyPart = _cmdUtil.I18N!.GetArmorZoneName(aggressor.BodyPart ?? _i18N.GetText("Unknown"))
                    }
                );
            }
            else
            {
                msg += _i18N.GetText("Cmd-Info.本局被击杀者信息加载失败");
                // msg += "\n击杀者数据加载失败";
            }
        }

        return msg;
    }
}