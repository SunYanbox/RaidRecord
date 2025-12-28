using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;

namespace RaidRecord.WebUI;

/// <summary>
/// 这个服务用于模组的Web端的格式化文本输出
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
public class WebFormatService(I18N i18N)
{
    /// <summary>
    /// 获取Archive中可空类型的格式化值
    /// </summary>
    public (string createTimeStr, string mapName, int killCount, string resultStr) GetArchiveInfo(RaidArchive archive)
    {
        return (
            createTimeStr: FromUnixTimestampSeconds(archive.CreateTime),
            mapName: i18N.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower()),
            killCount: archive.EftStats?.Victims?.Count() ?? 0,
            resultStr: i18N.GetText(archive.Results?.Result.ToString() ?? "UnknownResult")
        );
    }

    /// <summary>
    /// 获取Unix时间戳的格式化值
    /// </summary>
    /// <param name="seconds">时间戳</param>
    public string FromUnixTimestampSeconds(long seconds)
    {
        DateTime time = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
        return time.ToShortDateString() + " " + time.ToShortTimeString();
    }
}