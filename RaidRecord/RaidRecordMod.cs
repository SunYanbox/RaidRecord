using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;
using MudBlazor.Services;

// ReSharper disable UnusedType.Global

namespace RaidRecord;

[Injectable]
internal class RaidRecordMod(
    ModConfig modConfig,
    HttpServer httpServer,
    WebApplicationBuilder builder): IOnLoad
{
    public Task OnLoad()
    {

        builder.Services.AddMudServices();

        modConfig.Info($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
        modConfig.Info($"WeiUI run at {httpServer.ListeningUrl()}/RaidRecord");
        return Task.CompletedTask;
    }
}