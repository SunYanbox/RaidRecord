using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class InfoCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly I18NMgr _i18NMgr;
    private readonly DataGetterService _dataGetter;
    private readonly DataFormatService _dataFormatService;
    private I18N I18N => _i18NMgr.I18N!;

    public InfoCmd(CmdUtil cmdUtil, DataGetterService dataGetter, DataFormatService dataFormatService, I18NMgr i18NMgr)
    {
        _i18NMgr = i18NMgr;
        _cmdUtil = cmdUtil;
        Key = "info";
        Desc = "z2serverMessage.Cmd-Info.Desc".Translate(I18N);
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", "z3translations.Cmd-参数简述.index".Translate(I18N))
            .SetOptional(["index"])
            .Build();
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
            msg += "z2serverMessage.Cmd-Info.本局击杀标题".Translate(I18N);
            foreach (Victim victim in victims)
            {
                // "z2serverMessage.Cmd-Info.本局击杀信息": "\n {{VictimTime}} 使用{{WeaponName}}命中{{BodyPart}}淘汰距离{{VictimDistance}}m远的{{VictimName}}(等级:{{VictimLevel}} 阵营:{{VictimSide}} 角色:{{VictimRole}})",
                msg += "z2serverMessage.Cmd-Info.本局击杀信息".Translate(
                    I18N,
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
                // "z2serverMessage.Cmd-Info.本局击杀者": "\n击杀者: {{AggressorName}}(阵营: {{AggressorSide}})使用{{WeaponName}}命中{{ArmorZone}}淘汰了你",
                msg += "z2serverMessage.Cmd-Info.本局击杀者".Translate(
                    I18N,
                    new
                    {
                        AggressorName = aggressor.Name,
                        AggressorSide = aggressor.Side,
                        WeaponName = _dataFormatService.GetWeaponName(aggressor),
                        BodyPart = _cmdUtil.I18NMgr!.GetArmorZoneName(aggressor.BodyPart ?? "Unknown".Translate(I18N))
                    }
                );
            }
            else
            {
                msg += "z2serverMessage.Cmd-Info.本局被击杀者信息加载失败".Translate(I18N);
                // msg += "\n击杀者数据加载失败";
            }
        }

        return msg;
    }
}