using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Systems;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord.Core.Models;

[Injectable]
public class InjectableClasses(
    JsonUtil jsonUtil,
    RecordCacheManager recordCacheManager,
    LocalizationManager localizationManager,
    ProfileHelper profileHelper,
    ItemHelper itemHelper,
    ModConfig modConfig,
    TimeUtil timeUtil
)
{
    public JsonUtil? JsonUtil { get; set; } = jsonUtil;
    public RecordCacheManager? RecordCacheManager { get; set; } = recordCacheManager;
    public LocalizationManager? LocalizationManager { get; set; } = localizationManager;
    public ProfileHelper? ProfileHelper { get; set; } = profileHelper;
    public ItemHelper? ItemHelper { get; set; } = itemHelper;
    public ModConfig? ModConfig { get; set; } = modConfig;
    public TimeUtil? TimeUtil { get; set; } = timeUtil;

    public bool IsValid()
    {
        return JsonUtil != null
               && RecordCacheManager != null
               && LocalizationManager != null
               && ProfileHelper != null
               && ItemHelper != null
               && ModConfig != null
               && TimeUtil != null;
    }
}