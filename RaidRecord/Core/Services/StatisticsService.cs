using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SuntionCore.Services.LogUtils;

namespace RaidRecord.Core.Services;

/// <summary> 负责统计历史战绩 </summary>
[Injectable(InjectionType.Singleton)]
public sealed class StatisticsService(
    RecordManager recordManager,
    DataFormatService dataFormatService)
{
    public readonly ModLogger Logger = ModLogger.GetOrCreateLogger("RaidRecord");
    
    public async Task<Dictionary<string, List<RaidDataWrapper>>?> GroupByPlayerMap(MongoId account)
    {
        try
        {
            EFTCombatRecord combatRecord = await recordManager.GetRecord(account);
            HashSet<string> maps = combatRecord.Records
                .Where(x => x.Archive is not null)
                .Select(x => dataFormatService.GetMapId(x.Archive!)).ToHashSet();
            Dictionary<string, List<RaidDataWrapper>> result = maps.ToDictionary<string, string, List<RaidDataWrapper>>(
                map => map, _ => []);
            foreach (RaidDataWrapper wrapper in combatRecord.Records)
            {
                if (wrapper.Archive is null)
                {
                    Logger.Warn($"按地图分组统计数据时, {wrapper}未存档");
                    continue;
                }
                result[dataFormatService.GetMapId(wrapper.Archive)].Add(wrapper);
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.Error($"获取账号{account}的存档时出现错误", e);
        }
        return null;
    }
    
    public async Task<Dictionary<ExitStatus, List<RaidDataWrapper>>?> GroupByExitStatus(MongoId account)
    {
        try
        {
            EFTCombatRecord combatRecord = await recordManager.GetRecord(account);
            HashSet<ExitStatus> maps = combatRecord.Records
                .Where(x => x.Archive?.Results is { Result: not null })
                .Select(x => x.Archive!.Results!.Result!.Value).ToHashSet();
            Dictionary<ExitStatus, List<RaidDataWrapper>> result = maps.ToDictionary<ExitStatus, ExitStatus, List<RaidDataWrapper>>(
                map => map, _ => []);
            foreach (RaidDataWrapper wrapper in combatRecord.Records)
            {
                if (wrapper.Archive is null)
                {
                    Logger.Warn($"按撤离情况分组统计数据时, {wrapper}未存档");
                    continue;
                }

                if (wrapper.Archive?.Results?.Result is null)
                {
                    Logger.Warn($"按撤离情况分组统计数据时, {wrapper}的结果为空");
                    continue;
                }
                result[wrapper.Archive.Results.Result.Value].Add(wrapper);
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.Error($"获取账号{account}的存档时出现错误", e);
        }
        return null;
    }
}