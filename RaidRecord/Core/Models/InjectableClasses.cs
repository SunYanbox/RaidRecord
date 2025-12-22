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
    RecordManager recordCacheManager,
    I18N i18N,
    ProfileHelper profileHelper,
    ItemHelper itemHelper,
    ModConfig modConfig,
    TimeUtil timeUtil
)
{
    public JsonUtil? JsonUtil { get; set; } = jsonUtil;
    public RecordManager? RecordManager { get; set; } = recordCacheManager;
    public I18N? I18N { get; set; } = i18N;
    public ProfileHelper? ProfileHelper { get; set; } = profileHelper;
    public ItemHelper? ItemHelper { get; set; } = itemHelper;
    public ModConfig? ModConfig { get; set; } = modConfig;
    public TimeUtil? TimeUtil { get; set; } = timeUtil;

    public bool IsValid()
    {
        return JsonUtil != null
               && RecordManager != null
               && I18N != null
               && ProfileHelper != null
               && ItemHelper != null
               && ModConfig != null
               && TimeUtil != null;
    }
}