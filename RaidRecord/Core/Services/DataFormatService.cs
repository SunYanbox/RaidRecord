using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.Services;

/// <summary>
/// 这个服务用于模组数据的格式化文本输出
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class DataFormatService(I18NMgr i18NMgr)
{
    public IReadOnlyDictionary<string, string>? SptLocals;
    public readonly string UnknowWeapon = "UnknownWeapon".Translate(i18NMgr.I18N!);

    #region Archive
    /// <summary> 从ServerId解析地图ID </summary>
    private string GetMapId(RaidArchive archive)
    {
        return archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower();
    }

    /// <summary> 获取Archive的创建时间格式化值 </summary>
    public string GetCreateTimeStr(RaidArchive archive)
    {
        return FromDateTimeSeconds(archive.CreateTime);
    }

    /// <summary> 获取Archive的地图名称格式化值 </summary>
    public string GetMapNameLocal(RaidArchive archive)
    {
        return i18NMgr.GetMapName(GetMapId(archive));
    }

    /// <summary> 获取Archive的击杀数 </summary>
    public int GetKillCount(RaidArchive archive)
    {
        return archive.EftStats?.Victims?.Count() ?? 0;
    }

    /// <summary> 获取Archive的爆头率 </summary>
    public string GetHeadshotRate(RaidArchive archive)
    {
        int killCount = GetKillCount(archive);
        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];
        int headshotKillCount = victims.Count(x => AlgorithmService.IsBodyPartHeadshotKill(x.BodyPart));
        return killCount == 0 ? "N/A" : $"{headshotKillCount / Math.Max((double)killCount, 1):P2}";
    }

    /// <summary> 获取对局生存风格 </summary>
    public string GetSurvivorClass(RaidArchive archive)
    {
        string survivorClass = archive.EftStats?.SurvivorClass ?? "Unknown";
        return i18NMgr.I18N?.SptLocals?.GetValueOrDefault(survivorClass) ?? "Unknown".Translate(i18NMgr.I18N!);
    }

    /// <summary> 获取Archive的撤离点名称 </summary>
    public string GetExitName(RaidArchive archive)
    {
        return i18NMgr.GetExitName(GetMapId(archive), archive.Results?.ExitName ?? "Unknown".Translate(i18NMgr.I18N!));
    }

    /// <summary> 获取Archive的结算结果 </summary>
    public string GetResultStr(RaidArchive archive)
    {
        return (archive.Results?.Result.ToString() ?? "UnknownResult").Translate(i18NMgr.I18N!);
    }

    /// <summary> 获取净利润 </summary>
    public long GetNetProfit(RaidArchive archive)
    {
        return archive.GrossProfit - archive.CombatLosses;
    }
    #endregion

    #region Victim
    /// <summary> 获取 Victim 击杀信息 的武器名称 </summary>
    public string GetWeaponName(Victim victim)
    {
        SptLocals ??= i18NMgr.I18N?.SptLocals;
        if (SptLocals == null || victim.Weapon == null) return UnknowWeapon;
        string weaponName = victim.Weapon.Replace("Short", "");
        return SptLocals.GetValueOrDefault(weaponName, weaponName);
    }

    /// <summary> 获取 Victim 击杀信息 的部位名称 </summary>
    public string GetBodyPartName(Victim victim)
    {
        return i18NMgr.GetArmorZoneName(victim.BodyPart ?? "");
    }

    /// <summary> 获取 Victim 击杀信息 的距离 </summary>
    public string GetDistance(Victim victim)
    {
        return $"{victim.Distance ?? 0:F2}m";
    }

    /// <summary> 获取 Victim 击杀信息 的身份 </summary>
    public string GetRoleName(Victim victim)
    {
        return i18NMgr.GetRoleName(victim.Role ?? "");
    }

    /// <summary> 获取 Aggressor 击杀信息 的身份 </summary>
    public string GetRoleName(Aggressor aggressor)
    {
        return i18NMgr.GetRoleName(aggressor.Role ?? "");
    }

    /// <summary> 获取 Aggressor 击杀玩家者的武器名称 </summary>
    public string GetWeaponName(Aggressor aggressor)
    {
        SptLocals ??= i18NMgr.I18N?.SptLocals;
        if (SptLocals == null || aggressor.WeaponName == null) return UnknowWeapon;
        string weaponName = aggressor.WeaponName.Replace("Short", "");
        return SptLocals.GetValueOrDefault(weaponName, weaponName);
    }

    /// <summary> 获取 Victim 击杀信息 的触发时间 </summary>
    public string GetVictimTime(Victim victim)
    {
        return victim.Time?[..13] ?? "N/A";
    }
    #endregion


    #region time
    /// <summary>
    /// 获取Unix时间戳的 日期-时间 格式化值
    /// </summary>
    /// <param name="seconds">时间戳</param>
    public string FromDateTimeSeconds(long seconds)
    {
        DateTime time = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
        return time.ToShortDateString() + " " + time.ToShortTimeString();
    }

    /// <summary> 获取Unix时间戳的 时间 格式化值 </summary>
    public string FromTimeSeconds(long seconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime.ToLongTimeString();
    }
    #endregion
}