using RaidRecord.Core.ChatBot;
using RaidRecord.Core.Locals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

// ReSharper disable UnusedType.Global

namespace RaidRecord;

[Injectable]
internal class RaidRecordMod(
    ISptLogger<RaidRecordMod> logger,
    ConfigServer configServer,
    LocalizationManager localManager,
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
        UserDialogInfo chatbot = raidRecordManagerChat.GetChatBot();
        var coreConfig = configServer.GetConfig<CoreConfig>();
        coreConfig.Features.ChatbotFeatures.Ids[chatbot.Info.Nickname] = chatbot.Id;
        coreConfig.Features.ChatbotFeatures.EnabledBots[chatbot.Id] = true;
        // logger.Info($"[RaidRecord] 已经成功注册ChatBot: {chatbot.Id}");
        logger.Info(localManager.GetText("MainMod-Info.成功注册ChatBot", new
        {
            ChatBotId = chatbot.Id
        }));
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