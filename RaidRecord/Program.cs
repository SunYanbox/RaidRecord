using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.raidrecord";
    public override string Name { get; init; } = "RaidRecord";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0.6.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "CC-BY-SA";
}

[Injectable]
public class CustomStaticRouter : StaticRouter
{
    private static JsonUtil? _jsonUtil;
    private static RecordCacheManager? _recordCacheManager;
    private static ModConfig? _modConfig;
    private static ProfileHelper? _profileHelper;
    private static ItemHelper? _itemHelper;

    public CustomStaticRouter(
        JsonUtil jsonUtil,
        ModConfig modConfig,
        RecordCacheManager recordCacheManager,
        ProfileHelper profileHelper,
        ItemHelper itemHelper) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _jsonUtil = jsonUtil;
        _modConfig  = modConfig;
        _recordCacheManager = recordCacheManager;
        _profileHelper = profileHelper;
        _itemHelper = itemHelper;
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
    
    private static void HandleRaidStart(StartLocalRaidRequestData info, MongoId sessionId, string output)
    {
        try
        {
            var response = _jsonUtil.Deserialize<GetBodyResponseData<StartLocalRaidResponseData>>(output);
            // Console.WriteLine("解析后的output_response: " + _jsonUtil.Serialize(response));
            // Console.WriteLine("解析后的output.Data: " + _jsonUtil.Serialize(response.Data));
            var pmcData = _profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception($"获取不到来自session: {sessionId}的存档数据pmcData");
            var notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception($"获取不到来自session: {sessionId}的玩家ID数据pmcData.Id");
            var playerId = notSurePlayerId.Value;

            if (response?.Data?.ServerId?.Contains("Savage") ?? false) throw new Exception($"暂时不支持Savage模式的战绩记录");
            
            _recordCacheManager.ZipAll(_itemHelper, playerId);
            var recordWrapper = _recordCacheManager.CreateRecord(playerId);
            
            recordWrapper.Info.HandleRaidStart(response.Data, sessionId, _itemHelper, _profileHelper);
            // recordWrapper.Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, _itemHelper);
            _recordCacheManager.SaveRecord(playerId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[RaidRecord] Exception at HandleRaidStart: {e.Message}");
        }
    }
    
    private static void HandleRaidEnd(EndLocalRaidRequestData info, MongoId sessionId, string? output)
    {
        try
        {
            // Console.WriteLine($"<HandleRaidStart>\nurl: {url};\n info: {info};\n sessionId: {sessionId};\n output: {output};");
            // Console.WriteLine(_jsonUtil.Serialize(info));
            var pmcData = _profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null) throw new Exception($"pmcData is null");
            var notSurePlayerId = pmcData.Id;
            if (notSurePlayerId == null) throw new Exception($"notSurePlayerId is null");
            var playerId = notSurePlayerId.Value;
            
            
            if (info?.ServerId?.Contains("Savage") ?? false) throw new Exception($"暂时不支持Savage模式的战绩记录");
            
            var records = _recordCacheManager.GetRecord(playerId);

            if (records.Count == 0 || !records[^1].IsInfo) throw new Exception($"游戏结束时没有发现任何已经开始的对局数据");

            if (info == null) throw new Exception("\nTag info is null!!!\n");
            // Console.WriteLine($"\n\ninfo直接print: {info} \n\ninfo序列化: {_jsonUtil.Serialize(info)}");
            records[^1].Info.HandleRaidEnd(info, sessionId, _itemHelper, _profileHelper);
            // records[^1].Info.ItemsTakeIn = Utils.GetInventoryInfo(pmcData, _itemHelper);
            records[^1].Zip(_itemHelper);
            
            _recordCacheManager.ZipAll(_itemHelper, playerId);
            _recordCacheManager.SaveRecord(playerId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[RaidRecord] Exception at HandleRaidEnd: {e.Message}");
        }
    }
    
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
class RaidRecordMod(
        ISptLogger<RaidRecordMod> logger,
        ConfigServer configServer,
        RaidRecordManagerChat raidRecordManagerChat
    ): IOnLoad
{
    public Task OnLoad()
    {

        RegisterChatBot();
        return Task.CompletedTask;
    }

    protected void RegisterChatBot()
    {
        var chatbot = raidRecordManagerChat.GetChatBot();
        var coreConfig = configServer.GetConfig<CoreConfig>();
        coreConfig.Features.ChatbotFeatures.Ids[chatbot.Info.Nickname] = chatbot.Id;
        coreConfig.Features.ChatbotFeatures.EnabledBots[chatbot.Id] = true;
        logger.Info($"[RaidRecord] 已经注册ChatBot: {chatbot.Id}");
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




