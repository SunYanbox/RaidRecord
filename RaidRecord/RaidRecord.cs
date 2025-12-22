using RaidRecord.Core.ChatBot;
using RaidRecord.Core.Configs;
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
internal class RaidRecordMod(ModConfig modConfig): IOnLoad
{
    public Task OnLoad()
    {

        RegisterChatBot();
        return Task.CompletedTask;
    }

    protected void RegisterChatBot()
    {
        modConfig.Info($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
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