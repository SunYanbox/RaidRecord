using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

// ReSharper disable UnusedType.Global

namespace RaidRecord;

[Injectable]
internal class RaidRecordMod(ModConfig modConfig): IOnLoad
{
    public Task OnLoad()
    {
        modConfig.Info($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
        return Task.CompletedTask;
    }
}