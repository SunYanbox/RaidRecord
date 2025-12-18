using Microsoft.Extensions.DependencyInjection;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Systems;

[Injectable]
public class CustomStaticRouter : StaticRouter
{
    // private static IContainer? _container;
    private static IServiceProvider? _serviceProvider;

    public CustomStaticRouter(
        // IContainer container,
        JsonUtil jsonUtil,
        IServiceProvider serviceProvider,
        ISptLogger<CustomStaticRouter> logger
        // LocalizationManager localManager,
        // RecordCacheManager recordCacheManager,
        // ProfileHelper profileHelper,
        // ModConfig modConfig,
        // ItemHelper itemHelper
        ) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _serviceProvider  = serviceProvider;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        
        // Console.WriteLine("************注册的路由已被获取************");
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
                    await Task.Run(() => HandleRaidStart(info, sessionId, output));
                    // return new ValueTask<string>(output);
                    return output;
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
                    // return new ValueTask<string>(output);
                    return output;
                }
            )
        ];
    }

    private static bool ParaNotNullJudge(InjectableClasses data)
    {
        // CustomStaticRouter.HandleRaidStart中以下参数是否为null: data.RecordCacheManager({0}), data.JsonUtil({1}), data.ProfileHelper({2})), data.ItemHelper({3})
        if (data.RecordCacheManager == null || data.LocalizationManager == null || data.ProfileHelper == null || data.ItemHelper == null ||
            data.ModConfig == null || data.JsonUtil == null)
        {
            var msg = data.LocalizationManager?.GetTextFormat(
                "raidrecord.CSR.Para.null",
                data.RecordCacheManager == null,
                data.JsonUtil == null,
                data.ProfileHelper == null,
                data.ItemHelper == null
            ) ?? string.Format(
                "data.LocalizationManager为null, data.RecordCacheManager({0}), data.JsonUtil({1}), data.ProfileHelper({2})), data.ItemHelper({3})",
                data.RecordCacheManager, data.JsonUtil, data.ProfileHelper, data.ItemHelper);

            if (data.ModConfig == null)
            {
                msg += "data.ModConfig为null";
            }
            else
            {
                data.ModConfig.Log("Error", msg);
            }
            
            Console.WriteLine($"[RaidRecord]<Para> {msg}");
            
            return true;
        }
        
        return false;
    }
    
    private static void HandleRaidStart(StartLocalRaidRequestData info, MongoId sessionId, string output)
    {
        var data = new InjectableClasses();
        try
        {
            if (_serviceProvider == null) throw new NullReferenceException("_serviceProvider");
            data.JsonUtil = _serviceProvider.GetService<JsonUtil>();
            data.RecordCacheManager = _serviceProvider.GetService<RecordCacheManager>();
            data.LocalizationManager = _serviceProvider.GetService<LocalizationManager>();
            data.ProfileHelper = _serviceProvider.GetService<ProfileHelper>();
            data.ItemHelper = _serviceProvider.GetService<ItemHelper>();
            data.ModConfig = _serviceProvider.GetService<ModConfig>();
            
            if (ParaNotNullJudge(data)) throw new Exception(data.LocalizationManager?.GetTextFormat("raidrecord.CSR.Para.error0") ?? "data.LocalizationManager为空, 其他属性也可能为空");
            var response = data.JsonUtil.Deserialize<GetBodyResponseData<StartLocalRaidResponseData>>(output);
            // Console.WriteLine("解析后的output_response: " + data.JsonUtil.Serialize(response));
            // Console.WriteLine("解析后的output.Data: " + data.JsonUtil.Serialize(response.Data));
            var pmcData = data.ProfileHelper.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRS.error0", sessionId));
            var notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRS.error1", sessionId));
            var playerId = notSurePlayerId.Value;

            if (response?.Data?.ServerId?.Contains("Savage") ?? false) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRS.error2"));
            
            var logger = _serviceProvider.GetService<ISptLogger<CustomStaticRouter>>();
            if (logger == null)
            {
                Console.WriteLine("\n_serviceProvider没获取到日志记录器");
                return;
            }
            if (data == null)
            {
                logger.Error("data is null");
                logger.Error($"data type: {data?.GetType()}");
                return;
            }
            if (data.RecordCacheManager == null)
            {
                logger.Error("RecordCacheManager is null");
                logger.Error($"data type: {data.GetType()}");
                logger.Error($"data properties: {string.Join(", ", data.GetType().GetProperties().Select(p => p.Name))}");
                return;
            }

            if (string.IsNullOrEmpty(playerId))
            {
                logger.Error("playerId is null or empty");
                return;
            }
            
            data.RecordCacheManager.ZipAll(data.ItemHelper, playerId);
            var recordWrapper = data.RecordCacheManager.CreateRecord(playerId);
            
            Console.WriteLine($"DEBUG CustomStaticRouter.HandleRaidStart > 获取的记录recordWrapper是否为空: {recordWrapper == null}" +
                              $"\njson解析的对象response是否为空: {response == null}" +
                              $"\nresponse.Data是否为空: {response?.Data == null}");
            
            recordWrapper.Info.HandleRaidStart(response.Data, sessionId, data.ItemHelper, data.ProfileHelper);
            // recordWrapper.Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, data.ItemHelper);
            data.RecordCacheManager.SaveRecord(playerId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[RaidRecord] {data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRS.error3", e.Message, e.StackTrace)}");
            data.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidStart");
        }
    }
    
    private static void HandleRaidEnd(EndLocalRaidRequestData info, MongoId sessionId, string? output)
    {
        var data = new InjectableClasses();
        try
        {
            if (_serviceProvider == null) throw new NullReferenceException("_serviceProvider");
            data.JsonUtil = _serviceProvider.GetService<JsonUtil>();
            data.RecordCacheManager = _serviceProvider.GetService<RecordCacheManager>();
            data.LocalizationManager = _serviceProvider.GetService<LocalizationManager>();
            data.ProfileHelper = _serviceProvider.GetService<ProfileHelper>();
            data.ItemHelper = _serviceProvider.GetService<ItemHelper>();
            data.ModConfig = _serviceProvider.GetService<ModConfig>();
            if (ParaNotNullJudge(data)) throw new Exception(data.LocalizationManager?.GetTextFormat("raidrecord.CSR.Para.error0") ?? "data.LocalizationManager为空, 其他属性也可能为空");
            // Console.WriteLine($"<HandleRaidStart>\nurl: {url};\n info: {info};\n sessionId: {sessionId};\n output: {output};");
            // Console.WriteLine(data.JsonUtil.Serialize(info));
            var pmcData = data.ProfileHelper.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRE.error0"));
            var notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRE.error1"));
            var playerId = notSurePlayerId.Value;
            
            
            if (info?.ServerId?.Contains("Savage") ?? false) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRE.error2"));
            
            var records = data.RecordCacheManager.GetRecord(playerId);

            if (records.Count == 0 || !records[^1].IsInfo) throw new Exception(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRE.error3"));

            // if (info == null) throw new Exception("\nTag info is null!!!\n");
            // Console.WriteLine($"\n\ninfo直接print: {info} \n\ninfo序列化: {data.JsonUtil.Serialize(info)}");
            records[^1].Info.HandleRaidEnd(info, sessionId, data.ItemHelper, data.ProfileHelper);
            // records[^1].Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, data.ItemHelper);
            records[^1].Zip(data.ItemHelper);
            
            data.RecordCacheManager.ZipAll(data.ItemHelper, playerId);
            data.RecordCacheManager.SaveRecord(playerId);
        }
        catch (Exception e)
        {
            Console.WriteLine(data.LocalizationManager.GetTextFormat("raidrecord.CSR.HRE.error4", e.Message, e.StackTrace));
            data.ModConfig?.LogError(e, "CustomStaticRouter.HandleRaidEnd");
        }
    }
    
}
