using RaidRecord.Core.Configs;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Systems;

[Injectable]
public class CustomStaticRouter: StaticRouter
{
    // private static IContainer? _container;
    private static IServiceProvider? _serviceProvider;
    private static InjectableClasses? _injectableClasses;
    private static RaidUtil? _raidUtil;

    public CustomStaticRouter(
        ISptLogger<CustomStaticRouter> sptLogger,
        JsonUtil jsonUtil,
        InjectableClasses injectableClasses,
        IServiceProvider serviceProvider,
        RaidUtil raidUtil
    ): base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _raidUtil = raidUtil;
        _serviceProvider = serviceProvider;
        _injectableClasses = injectableClasses;
        if (!_injectableClasses.IsValid())
        {
            sptLogger.Error("[RaidRecord] CustomStaticRouter的参数InjectableClasses未正确注入");
        }
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<StartLocalRaidRequestData>(
                "/client/match/local/start",
                async (
                    _,
                    info,
                    sessionId,
                    output
                ) =>
                {
                    await Task.Run(() => HandleRaidStart(info, sessionId, output!));
                    return output!;
                }
            ),
            new RouteAction<EndLocalRaidRequestData>(
                "/client/match/local/end",
                async (
                    _,
                    info,
                    sessionId,
                    output
                ) =>
                {
                    await Task.Run(() => HandleRaidEnd(info, sessionId, output));
                    return output!;
                }
            )
        ];
    }

    private static void HandleRaidStart(StartLocalRaidRequestData request, MongoId sessionId, string output)
    {
        if (_serviceProvider == null) throw new NullReferenceException(nameof(_serviceProvider));
        try
        {
            var response = _injectableClasses!.JsonUtil!.Deserialize<GetBodyResponseData<StartLocalRaidResponseData>>(output);
            string serverId = response!.Data!.ServerId!;
            PmcData? pmcData = _injectableClasses.ProfileHelper!.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception($"获取不到来自session: {sessionId}的存档数据pmcData");
            MongoId? notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception($"获取不到来自session: {sessionId}的玩家ID数据pmcData.Id");
            MongoId playerId = notSurePlayerId.Value;
            MongoId? account = _injectableClasses.RecordCacheManager!.GetAccount(sessionId);
            ModConfig? logger = _injectableClasses.ModConfig;

            List<string> errors = [];

            if (_injectableClasses.RecordCacheManager == null)
            {
                errors.Add("RecordCacheManager is null"
                           + $"data type: {_injectableClasses.GetType()}"
                           + $"data properties: {string.Join(", ", _injectableClasses.GetType().GetProperties().Select(p => p.Name))}");
            }
            if (account == null)
            {
                errors.Add("无法通过playerId获取玩家账户ID");
            }
            if (logger == null)
            {
                Console.WriteLine("[RaidRecord] ModConfig未正确注入InjectableClasses");
                if (errors.Count > 0)
                {
                    Console.WriteLine($"[RaidRecord] 其他错误: {string.Join(", ", errors)}");
                }
                return;
            }
            if (errors.Count > 0)
            {
                logger.LogError(
                    new InvalidDataException(
                        $"{nameof(_injectableClasses.RecordCacheManager)}" +
                        $"or {nameof(playerId)}"
                    ),
                    "CustomStaticRouter.HandleRaidStart",
                    string.Join(", ", errors));
                return;
            }

            // 归档已有玩家对局缓存
            _injectableClasses.RecordCacheManager!.ZipAccount(playerId);
            // 创建新缓存
            RaidDataWrapper? recordWrapper = _injectableClasses.RecordCacheManager.CreateRecord(playerId);

            logger.Debug($"DEBUG CustomStaticRouter.HandleRaidStart > 获取的记录recordWrapper是否为空: {recordWrapper == null!}" +
                         $"\njson解析的对象response是否为空: {response == null!}" +
                         $"\nresponse.Data是否为空: {response?.Data == null}");

            if (response?.Data == null)
            {
                logger.Error("response.Data为null, 无法正确解析回合开始的数据");
                return;
            }
            if (recordWrapper?.Info is not null)
                _raidUtil?.HandleRaidStart(recordWrapper.Info, serverId, sessionId);
            // recordWrapper.Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, data.ItemHelper);
            _injectableClasses.RecordCacheManager.SaveEFTRecord(account!.Value);
            logger.Info($"已记录对局开始: ServerId: {serverId}, SessionId: {sessionId}, 带入对局物品数量: {recordWrapper?.Info?.ItemsTakeIn.Count}");
        }
        catch (Exception e)
        {
            string msg = $"在HandleRaidStart函数出现错误: {e.Message}\nstack: {e.StackTrace}";
            Console.WriteLine($"[RaidRecord] Error in HandleRaidStart: {msg}");
            _injectableClasses?.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidStart", msg);
        }
    }

    private static void HandleRaidEnd(EndLocalRaidRequestData request, MongoId sessionId, string? _)
    {
        try
        {
            // Console.WriteLine($"<HandleRaidStart>\n url: {url};\n info: {info};\n sessionId: {sessionId};\n output: {output};");
            // Console.WriteLine(data.JsonUtil.Serialize(info));
            PmcData? pmcData = _injectableClasses!.ProfileHelper!.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception("pmcData意外为空");
            MongoId? notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception("获取不到非空pmcData.Id");
            MongoId playerId = notSurePlayerId.Value;

            JsonUtil? jsonUtil = _injectableClasses.JsonUtil;

            MongoId? accountId = _injectableClasses.RecordCacheManager!.GetAccount(playerId);

            if (accountId == null)
                throw new Exception($"创建记录时未找到玩家{playerId}的账户Id, 请确保已存在过该玩家账户的记录");

            EFTCombatRecord records = _injectableClasses.RecordCacheManager!.GetRecord(accountId.Value);

            if (records.Records.Count == 0 || records.InfoRecordCache == null)
                throw new Exception("游戏结束时没有发现任何已经开始的对局数据");

            if (request == null) throw new Exception("\nHandleRaidEnd的参数info为空, 这可能是SPT更改了服务端传递的参数; 在没有其他服务端模组影响的条件下, 该报错理论上很难发生!!!\n");
            // Console.WriteLine($"\n\n info直接print: {info} \n\n info序列化: {data.JsonUtil.Serialize(info)}");

            if (records.InfoRecordCache.Info is not null)
                _raidUtil?.HandleRaidEnd(records.InfoRecordCache.Info, request, sessionId);

            int itemsTakeOutCount = records.InfoRecordCache.Info?.ItemsTakeOut.Count ?? 0;

            _injectableClasses.RecordCacheManager.ZipAccount(playerId);
            _injectableClasses.RecordCacheManager.SaveEFTRecord(accountId.Value);
            _injectableClasses.ModConfig?.Info($"已记录对局结束: {request.ServerId}, "
                                               + $"Session: {jsonUtil?.Serialize(sessionId)}, "
                                               + $"Results: {{ Result: {request.Results!.Result}, ExitName: {request.Results.ExitName}, PlayTime: {request.Results.PlayTime} }}, " // EndRaidResult对象太大了
                                               + $"LocationTransit: {jsonUtil?.Serialize(request.LocationTransit)}, "
                                               + $"带出对局物品数量: {itemsTakeOutCount}");
        }
        catch (Exception e)
        {
            string msg = $"在HandleRaidEnd函数出现错误: {e.Message}\nstack: {e.StackTrace}";
            Console.WriteLine($"[RaidRecord] Error in HandleRaidEnd: {msg}");
            _injectableClasses?.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidEnd", msg);
        }
    }

}