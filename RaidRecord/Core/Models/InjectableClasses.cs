using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Systems;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Models;

internal record InjectableClasses
{
    public JsonUtil? JsonUtil { get; set; }
    public RecordCacheManager? RecordCacheManager { get; set; }
    public LocalizationManager? LocalizationManager { get; set; }
    public ProfileHelper? ProfileHelper { get; set; }
    public ItemHelper? ItemHelper { get; set; }
    public ModConfig? ModConfig { get; set; }

    public bool IsValid()
    {
        return JsonUtil != null
               && RecordCacheManager != null
               && LocalizationManager != null
               && ProfileHelper != null
               && ItemHelper != null
               && ModConfig != null;
    }
}