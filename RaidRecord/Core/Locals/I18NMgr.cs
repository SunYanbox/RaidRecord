using System.Reflection;
using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Servers;
using SuntionCore.Services.I18NUtil;

// ReSharper disable ClassNeverInstantiated.Global

namespace RaidRecord.Core.Locals;

/// <summary>
/// 初始化本地数据
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 10)]
public class I18NMgr(
    ModHelper modHelper,
    ModConfig modConfig,
    ModMetadata modMetadata,
    DatabaseServer databaseServer): IOnLoad
{
    public readonly Dictionary<string, string> MapNames = new();
    public readonly Dictionary<string, Dictionary<string, string>> ExitNames = new();
    public I18N? I18N { get; private set; }
    
    /// <summary> 重新初始化当前语言 </summary>
    public void ReInitLang()
    {
        DatabaseTables tables = databaseServer.GetTables();
        InitI18N(tables.Locations, tables.Locales);
    }

    // 所有的地图名称
    private readonly string[] _mapNames =
    [
        "Woods",
        "bigmap",
        // "develop",
        "factory4_day", // factory4_day
        "factory4_night", // factory4_night
        // "hideout",
        "Interchange",
        "Laboratory",
        "Labyrinth",
        "Lighthouse",
        // "privatearea",
        "RezervBase",
        "Sandbox",
        // "sandboxhigh",
        "Shoreline",
        // "suburbs",
        "TarkovStreets"
        // "terminal",
        // "town"
    ];

    public Task OnLoad()
    {
        I18N.Initialize(databaseServer);
        I18N = new I18N(modMetadata.ModGuid);
        string localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        localsDir = Constants.DBLocalsFolderPath(localsDir, modHelper);
        try
        {
            I18N.LoadFolders(localsDir);
            I18N.CurrentLang = modConfig.Configs.Local;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{modMetadata.Name}] {e.Message} {e.StackTrace}");
            throw;
        }

        if (modConfig.Configs.AutoUnloadOtherLang)
        {
            // 卸载不用的语言内存
            foreach (string language in I18N.AvailableLang)
            {
                if (language != "ch" && language != I18N.CurrentLang)
                {
                    I18N.Remove(language);
                }
            }
        }

        ReInitLang();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 基于物品TplID获取物品本地化名称
    /// </summary>
    /// <returns>本地化后的字符串</returns>
    public string GetItemName(MongoId temple)
    {
        return I18N!.SptLocals?.GetValueOrDefault($"{temple} Name") ?? temple.ToString();
    }

    /// <summary>
    /// 获取小写去除_的地图名称, 作为字典的键
    /// </summary>
    private static string GetMapKey(string mapName)
    {
        return mapName.Replace("_", "").ToLower();
    }

    protected void InitI18N(Locations locations, LocaleBase locales)
    {
        Dictionary<string, string>? localesMap = locales.Global[I18N!.CurrentLang].Value;
        string warnMsg = "";
        
        MapNames.Clear();
        ExitNames.Clear();

        // 获取地图的本地化表示
        foreach (string mapName in _mapNames)
        {
            MapNames[GetMapKey(mapName)]
                = localesMap?.TryGetValue(mapName, out string? name) ?? false ? name : mapName;
        }

        modConfig.Debug($"已加载地图: {string.Join(", ", MapNames.Values.Select(x => x.ToString()))}");

        Dictionary<string, Location> sptLocations = locations.GetDictionary();

        foreach (string mapName in _mapNames)
        {
            string mapKey = GetMapKey(mapName);

            ExitNames.Add(mapKey, new Dictionary<string, string>());

            if (!sptLocations.TryGetValue(mapName.Replace("_", ""), out Location? map))
            {
                continue;
            }
            foreach (AllExtractsExit exit in map.AllExtracts)
            {
                // Console.WriteLine($"map: {mapName}\n exit: {exit}\n data: " + JsonSerializer.Serialize(ExitNames));
                if (exit.Name == null) continue;
                if (localesMap != null && !localesMap.ContainsKey(exit.Name))
                {
                    warnMsg += "z2serverMessage.I18N-Warn.撤离点名称不存在".Translate(I18N, new
                    {
                        ExitName = exit.Name
                    });
                    // warnMsg += $"警告: 撤离点{exit.Name}不存在于SPT本地化字典中" + "\n";
                    continue;
                }

                if (ExitNames[mapKey].ContainsKey(exit.Name))
                {
                    warnMsg += "z2serverMessage.I18N-Warn.重复添加撤离点".Translate(I18N, new
                    {
                        ExitName = exit.Name,
                        MapName = mapName
                    });
                    // warnMsg += $"警告: 地图 {mapName} 的数据中已存在撤离点 {exit.Name}，添加失败" + "\n";
                    continue;
                }
                if (localesMap != null)
                    ExitNames[mapKey].Add(exit.Name, localesMap[exit.Name]);
            }
        }

        if (!string.IsNullOrEmpty(warnMsg)) modConfig.Log("Warn", warnMsg);
        // modConfig.Info("已成功加载各个地图撤离点数据");
        modConfig.Info("z2serverMessage.I18N-Info.撤离点数据加载完毕".Translate(I18N, new
        {
            MapCount = MapNames.Count,
            ExitCount = ExitNames.Sum(x => x.Value.Count)
        }));
    }

    /// <summary>
    /// 获取地图本地化名称
    /// </summary>
    public string GetMapName(string map)
    {
        return MapNames.GetValueOrDefault(GetMapKey(map), map);
    }

    /// <summary>
    /// 获取撤离点本地化名称
    /// </summary>
    public string GetExitName(string map, string key)
    {
        return ExitNames.TryGetValue(GetMapKey(map), out Dictionary<string, string>? mapExits) ? mapExits.GetValueOrDefault(key, key) : key;
    }

    /// <summary>
    /// 获取命中区域本地化名称
    /// </summary>
    public string GetArmorZoneName(string key)
    {
        return $"z0armorZone.{key}".Translate(I18N!);
    }

    /// <summary>
    /// 获取角色本地化名称
    /// </summary>
    public string GetRoleName(string key)
    {
        return $"z1roleNames.{key}".Translate(I18N!);
    }
}