using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
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
// ReSharper disable UnusedType.Global

namespace RaidRecord.Core.Systems;

[Injectable]
public class CustomStaticRouter: StaticRouter
{
    private static RaidUtil? _raidUtil;
    private static IServiceProvider? _serviceProvider;
    private static InjectableClasses? _injectableClasses;

    public CustomStaticRouter(
        JsonUtil jsonUtil,
        RaidUtil raidUtil,
        IServiceProvider serviceProvider,
        InjectableClasses injectableClasses,
        ISptLogger<CustomStaticRouter> sptLogger
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

    private static async Task HandleRaidStart(StartLocalRaidRequestData _, MongoId sessionId, string output)
    {
        if (_serviceProvider == null) throw new NullReferenceException(nameof(_serviceProvider));
        try
        {
            var response = _injectableClasses!.JsonUtil!.Deserialize<GetBodyResponseData<StartLocalRaidResponseData>>(output);
            string serverId = response!.Data!.ServerId!;
            PmcData? pmcData = _injectableClasses.ProfileHelper!.GetPmcProfile(sessionId);
            if (pmcData == null) throw new NullReferenceException("pmcData");
            MongoId? notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new NullReferenceException("pmcData.Id");
            MongoId playerId = notSurePlayerId.Value;
            MongoId? account = _injectableClasses.RecordManager!.GetAccount(sessionId);
            ModConfig? logger = _injectableClasses.ModConfig;
            I18NMgr i18NMgr = _injectableClasses.I18NMgr!;

            List<string> errors = [];

            if (_injectableClasses.RecordManager == null)
            {
                errors.Add(i18NMgr.GetText(
                    "CustomStaticRouter-Error.RecordManager为null",
                    new
                    {
                        DataType = _injectableClasses.GetType(),
                        DataProperties = _injectableClasses.GetType().GetProperties().Select(p => p.Name).ToList()
                    }
                ));
                // errors.Add("RecordManager is null"
                //            + $"data type: {_injectableClasses.GetType()}"
                //            + $"data properties: {string.Join(", ", _injectableClasses.GetType().GetProperties().Select(p => p.Name))}");
            }
            if (account == null)
            {
                errors.Add(i18NMgr.GetText("CustomStaticRouter-Error.无法通过PlayerId获取玩家账户"));
            }
            if (logger == null)
            {
                Console.WriteLine("[RaidRecord] "
                                  + i18NMgr.GetText(
                                      "CustomStaticRouter-Error.InjectableClasses未正确注入ModConfig属性"));
                // Console.WriteLine("[RaidRecord] ModConfig未正确注入InjectableClasses");
                if (errors.Count > 0)
                {
                    Console.WriteLine("[RaidRecord]"
                                      + i18NMgr
                                          .GetText("CustomStaticRouter-Error.对局开始.其他错误",
                                              new { Errors = string.Join(", ", errors) }));
                    // Console.WriteLine($"[RaidRecord] 其他错误: {string.Join(", ", errors)}");
                }
                return;
            }
            if (errors.Count > 0)
            {
                logger.LogError(
                    new InvalidDataException(
                        $"{nameof(_injectableClasses.RecordManager)}" +
                        $"or {nameof(playerId)}"
                    ),
                    "CustomStaticRouter.HandleRaidStart",
                    string.Join(", ", errors));
                return;
            }

            // 归档已有玩家对局缓存
            await _injectableClasses.RecordManager!.ZipAccount(playerId);
            // 创建新缓存
            RaidDataWrapper? recordWrapper = await _injectableClasses.RecordManager.CreateRecord(playerId);

            logger.Debug($"DEBUG CustomStaticRouter.HandleRaidStart > 获取的记录recordWrapper是否为空: {recordWrapper == null!}" +
                         $"\njson解析的对象response是否为空: {response == null!}" +
                         $"\nresponse.Data是否为空: {response?.Data == null}");

            if (response?.Data == null)
            {
                logger.Error(
                    i18NMgr
                        .GetText("CustomStaticRouter-Error.对局开始.响应解析失败"));
                // logger.Error("response.Data为null, 无法正确解析回合开始的数据");
                return;
            }
            if (recordWrapper?.Info is not null)
                _raidUtil?.HandleRaidStart(recordWrapper.Info, serverId, sessionId);
            // recordWrapper.Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, data.ItemHelper);
            await _injectableClasses.RecordManager.SaveEFTRecord(account!.Value);
            logger.Info(i18NMgr
                .GetText("CustomStaticRouter-Info.对局开始.已记录",
                    new
                    {
                        ServerId = serverId,
                        SessionId = sessionId,
                        ItemsTakeInCount = recordWrapper?.Info?.ItemsTakeIn.Count
                    }));
            // logger.Info($"已记录对局开始: ServerId: {serverId}, SessionId: {sessionId}, 带入对局物品数量: {recordWrapper?.Info?.ItemsTakeIn.Count}");
        }
        catch (Exception e)
        {
            string msg = $"在HandleRaidStart函数出现错误: {e.Message}\nstack: {e.StackTrace}";
            Console.WriteLine($"[RaidRecord] Error in HandleRaidStart: {msg}");
            _injectableClasses?.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidStart", msg);
        }
    }

    private static async Task HandleRaidEnd(EndLocalRaidRequestData request, MongoId sessionId, string? _)
    {
        try
        {
            // Console.WriteLine($"<HandleRaidStart>\n url: {url};\n info: {info};\n sessionId: {sessionId};\n output: {output};");
            // Console.WriteLine(data.JsonUtil.Serialize(info));
            PmcData? pmcData = _injectableClasses!.ProfileHelper!.GetPmcProfile(sessionId);
            if (pmcData == null) throw new NullReferenceException("pmcData");
            MongoId? notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new NullReferenceException("pmcData.Id");
            MongoId playerId = notSurePlayerId.Value;

            JsonUtil? jsonUtil = _injectableClasses.JsonUtil;
            I18NMgr i18NMgr = _injectableClasses.I18NMgr!;
            MongoId? accountId = _injectableClasses.RecordManager!.GetAccount(playerId);

            if (accountId == null)
                throw new Exception(i18NMgr.GetText("RecordManager-Error.指定的玩家账号不存在", new { PlayerId = playerId }));

            EFTCombatRecord records = await _injectableClasses.RecordManager!.GetRecord(accountId.Value);

            if (records.Records.Count == 0 || records.InfoRecordCache == null)
                throw new Exception(i18NMgr.GetText("CustomStaticRouter-Error.对局结束.没有有效开局数据"));

            if (request == null) throw new Exception("\nHandleRaidEnd的参数info为空, 这可能是SPT更改了服务端传递的参数; 在没有其他服务端模组影响的条件下, 该报错理论上很难发生!!!\n");
            // Console.WriteLine($"\n\n info直接print: {info} \n\n info序列化: {data.JsonUtil.Serialize(info)}");

            if (records.InfoRecordCache.Info is not null)
                _raidUtil?.HandleRaidEnd(records.InfoRecordCache.Info, request, sessionId);

            int itemsTakeOutCount = records.InfoRecordCache.Info?.ItemsTakeOut.Count ?? 0;

            await _injectableClasses.RecordManager.ZipAccount(playerId);
            await _injectableClasses.RecordManager.SaveEFTRecord(accountId.Value);
            _injectableClasses.ModConfig?.Info(
                i18NMgr.GetText(
                    "CustomStaticRouter-Info.对局结束.已记录",
                    new
                    {
                        request.ServerId,
                        SessionId = jsonUtil?.Serialize(sessionId),
                        ResultsResult = request.Results!.Result,
                        ResultsExitName = request.Results.ExitName,
                        ResultsPlayTime = request.Results.PlayTime,
                        LocationTransit = jsonUtil?.Serialize(request.LocationTransit),
                        ItemsTakeOutCount = itemsTakeOutCount
                    }
                )
            );
            // _injectableClasses.ModConfig?.Info($"已记录对局结束: {request.ServerId}, "
            //                                    + $"Session: {jsonUtil?.Serialize(sessionId)}, "
            //                                    + $"Results: {{ Result: {request.Results!.Result}, ExitName: {request.Results.ExitName}, PlayTime: {request.Results.PlayTime} }}, " // EndRaidResult对象太大了
            //                                    + $"LocationTransit: {jsonUtil?.Serialize(request.LocationTransit)}, "
            //                                    + $"带出对局物品数量: {itemsTakeOutCount}");
        }
        catch (Exception e)
        {
            string msg = $"在HandleRaidEnd函数出现错误: {e.Message}\nstack: {e.StackTrace}";
            Console.WriteLine($"[RaidRecord] Error in HandleRaidEnd: {msg}");
            _injectableClasses?.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidEnd", msg);
        }
    }
}

/*
 *   (RouteAction) new RouteAction<StartLocalRaidRequestData>("/client/match/local/start",
 *      (Func<string, StartLocalRaidRequestData, MongoId, string, ValueTask<string>>)
 *          (async (url, info, sessionID, output) => await matchCallbacks.StartLocalRaid(url, info, sessionID))),
 * ---
  (RouteAction) new RouteAction<EndLocalRaidRequestData>("/client/match/local/end",
        (Func<string, EndLocalRaidRequestData, MongoId, string, ValueTask<string>>)
            (async (url, info, sessionID, output) => await matchCallbacks.EndLocalRaid(url, info, sessionID)))
 */