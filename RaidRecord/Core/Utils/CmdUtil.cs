using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Systems;
using RaidRecord.WebUI;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace RaidRecord.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class CmdUtil(
    I18N i18N,
    ModConfig modConfig,
    ProfileHelper profileHelper,
    RecordManager recordCacheManager,
    DataFormatService dataFormatService
)
{
    #region 提供依赖给工具调用 | 只放大部分命令需要的依赖
    public readonly I18N? I18N = i18N;
    public readonly RecordManager? RecordManager = recordCacheManager;
    public readonly ModConfig? ModConfig = modConfig;
    public readonly ParaInfoBuilder ParaInfoBuilder = new();
    #endregion

    public static string GetPlayerGroupOfServerId(string serverId)
    {
        var group = PlayerGroup.Pmc; // 默认
        if (string.IsNullOrEmpty(serverId)) return group.ToString();
        string[] parts = serverId.Split('.');
        if (parts.Length <= 1) return group.ToString();
        string side = parts[1].ToLowerInvariant();
        group = side.Contains("pmc") ? PlayerGroup.Pmc : PlayerGroup.Scav;
        return group.ToString();
    }

    public string GetArchiveMetadata(RaidArchive archive)
    {
        string msg = "";
        string serverId = archive.ServerId;
        string playerId = archive.PlayerId;
        PmcData playerData = RecordManager!.GetPmcDataByPlayerId(playerId);

        // "Record-元数据.Id与玩家信息": "{{TimeFormat}} 对局ID: {{ServerId}} 玩家信息: {{Nickname}}(Level={{Level}}, id={{PlayerId}})"
        msg += I18N!.GetText(
            "Record-元数据.Id与玩家信息",
            new
            {
                TimeFormat = dataFormatService.GetCreateTimeStr(archive),
                ServerId = serverId,
                playerData.Info?.Nickname,
                playerData.Info?.Level,
                PlayerId = playerId
            }
        );
        //"Record-元数据.地图与存活时间": "\n地图: {{MapName}} 生存时间: {{PlayTime}} 击杀数量: {{KillCount}} 爆头击杀率: {{HeadshotKillRate}}",
        msg += I18N!.GetText(
            "Record-元数据.地图与存活时间",
            new
            {
                MapName = dataFormatService.GetMapNameLocal(archive),
                PlayTime = dataFormatService.FromTimeSeconds(archive.Results?.PlayTime ?? 0),
                KillCount = dataFormatService.GetKillCount(archive),
                HeadshotKillRate = dataFormatService.GetHeadshotRate(archive)
            }
        );
        // "Record-元数据.入场信息": "\n入局战备: {{EquipmentValue}}rub, 安全箱物资价值: {{SecuredValue}}rub, 总带入价值: {{PreRaidValue}}rub",
        msg += I18N!.GetText(
            "Record-元数据.入场信息",
            new
            {
                EquipmentValue = (int)archive.EquipmentValue,
                SecuredValue = (int)archive.SecuredValue,
                PreRaidValue = (int)archive.PreRaidValue
            }
        );

        // "Record-元数据.退出信息": "\n带出价值: {{GrossProfit}}rub, 战损{{CombatLosses}}rub, 净利润{{NetProfit}}rub",
        msg += I18N!.GetText(
            "Record-元数据.退出信息",
            new
            {
                GrossProfit = (int)archive.GrossProfit,
                CombatLosses = (int)archive.CombatLosses,
                NetProfit = dataFormatService.GetNetProfit(archive)
            }
        );

        // "Record-元数据.对局结果": "\n对局结果: {{Result}} 撤离点: {{ExitName}} 游戏风格: {{SurvivorClass}}",
        msg += I18N.GetText(
            "Record-元数据.对局结果",
            new
            {
                Result = dataFormatService.GetResultStr(archive),
                ExitName = dataFormatService.GetExitName(archive),
                SurvivorClass = dataFormatService.GetSurvivorClass(archive)
            }
        );
        return msg;
    }

    /// <summary>
    /// 从解析的参数字典中获取指定类型的参数的值
    /// </summary>
    /// <param name="parameters">参数字典</param>
    /// <param name="key">参数键</param>
    /// <param name="defaultValue">默认值</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>获取到的参数值或者默认值</returns>
    /// <exception cref="ArgumentException">传入的参数键为空</exception>
    public T GetParameter<T>(Dictionary<string, string> parameters,
        string key,
        T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException(I18N!.GetText("CmdUtil-Error.参数键为空", new { KeyName = nameof(key) }));
        // throw new ArgumentException("Para key cannot be null or empty", nameof(key));

        // 如果字典中不存在该键，返回默认值
        if (!parameters.TryGetValue(key.ToLower(), out string? stringValue))
            return defaultValue;

        // 如果值为空，返回默认值
        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        try
        {
            // 使用Convert.ChangeType进行转换
            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch (Exception ex) when (
            ex is InvalidCastException or FormatException or OverflowException)
        {
            // 转换失败时返回默认值
            return defaultValue;
        }
    }

    public string DateFormatterFull(long timestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        int year = date.Year;
        int month = date.Month;
        int day = date.Day;
        string time = date.ToShortTimeString();
        return I18N!.GetText("CmdUtil-日期时间格式化", new
        {
            Year = year,
            Month = month,
            Day = day,
            Time = time
        });
        // return $"{year}年{month}月{day}日 {time}";
    }

    /// <summary>  Command验证参数的工具 </summary>
    public string? VerifyIParametric(Parametric parametric)
    {
        if (string.IsNullOrEmpty(parametric.SessionId))
        {
            return I18N!.GetText("CmdUtil-Error.参数验证.Value为空",
                new { ValueName = nameof(parametric.SessionId) });
            // return "未输入session参数或session为空";
        }

        try
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(parametric.SessionId!);
            if (pmcData?.Id == null)
            {
                throw new NullReferenceException(I18N!.GetText(
                    "CmdUtil-Error.PmcData为空或PmcData.Id为空"
                ));
                // throw new NullReferenceException($"{nameof(pmcData)} or {nameof(pmcData.Id)}");
            }
            string playerId = pmcData.Id;
            // if (string.IsNullOrEmpty(playerId)) throw new Exception("playerId为null或为空");
            if (string.IsNullOrEmpty(playerId))
            {
                throw new Exception(I18N!.GetText("CmdUtil-Error.参数验证.Value为空",
                    new { ValueName = nameof(playerId) }));
            }
        }
        catch (Exception e)
        {
            string errorMsg = I18N!.GetText("CmdUtil-Error.用户未注册或者Session已失效",
                new { ErrorMessage = e.Message });
            ModConfig?.LogError(e, "RaidRecordManagerChat.VerifyIParametric", errorMsg);
            return errorMsg;
        }

        return parametric.ManagerChat == null
            ? I18N!.GetText("CmdUtil-Error.参数验证.属性ManagerChat为空")
            : null;
        // return parametric.ManagerChat == null ? "实例未正确初始化: 缺少managerChat属性" : null;
    }
}