using System.Reflection;
using RaidRecord.Core.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using Path = System.IO.Path;
// ReSharper disable ClassNeverInstantiated.Global

namespace RaidRecord.Core.Locals;

/// <summary>
/// 第一时间初始化本地数据
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public class LocalizationManager(
    ISptLogger<LocalizationManager> logger,
    ModHelper modHelper,
    ModConfig modConfig,
    DatabaseServer databaseServer): IOnLoad
{
    public readonly Dictionary<string, string> MapNames = new();
    public readonly Dictionary<string, Dictionary<string, string>> ExitNames = new();
    private readonly Dictionary<string, LocalizationData> _allLocalizations = new();
    private string _currentLanguage = "ch"; // 默认语言

    public Task OnLoad()
    {
        string localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        localsDir = Path.Combine(localsDir, "db\\locals");
        if (Directory.Exists(localsDir))
        {
            // logger.Info($"本地化数据库存在: {localsDir}");
            foreach (string file in Directory.GetFiles(localsDir))
            {
                // eg: ch.json

                string fileName = Path.GetFileNameWithoutExtension(file);
                // logger.Info($"尝试加载{localsDir}文件夹下的{fileName}, file: {file}");
                if (file.EndsWith(".json") && fileName.Length == 2)
                {
                    // logger.Info($"> {fileName}成功加载");
                    _allLocalizations[fileName] = modHelper.GetJsonDataFromFile<LocalizationData>(localsDir, $"{fileName}.json");
                }
            }

            logger.Info(
                $"[RaidRecord] {GetTextFormat("models.local.LM.onload.info0", string.Join(", ", AvailableLanguages))}");
        }
        else
        {
            logger.Error($"[RaidRecord] 本地化数据库不存在: {localsDir}");
            logger.Error($"[RaidRecord] Localisation database does not exist: {localsDir}");
        }

        CurrentLanguage = modConfig.Configs.Local;

        DatabaseTables tables = databaseServer.GetTables();
        InitLocalization(tables.Locations, tables.Locales);
        return Task.CompletedTask;
    }

    protected void InitLocalization(Locations locations, LocaleBase locales)
    {
        // 所有的地图名称
        string[] mapNames =
        [
            "woods",
            "bigmap",
            // "develop",
            "factory4_day", // factory4_day
            "factory4_night", // factory4_night
            // "hideout",
            "interchange",
            "laboratory",
            "labyrinth",
            "lighthouse",
            // "privatearea",
            "rezervbase",
            "sandbox",
            "sandboxhigh",
            "shoreline",
            // "suburbs",
            "tarkovstreets"
            // "terminal",
            // "town"
        ];
        Dictionary<string, string>? localesMap = locales.Global[CurrentLanguage].Value;
        string errorMsg = "";

        // 获取地图的本地化表示
        foreach (string mapName in mapNames)
        {
            MapNames[mapName.Replace("_", "")] = localesMap?.TryGetValue(mapName, out string? name) ?? false ? name : mapName;
        }

        foreach (string mapName in mapNames)
        {
            ExitNames.Add(mapName, new Dictionary<string, string>());

            if (!locations.GetDictionary().ContainsKey(mapName.Replace("_", "")))
            {
                // Console.WriteLine($"警告: 没有键为{mapName}的数据, ");
                // var options1 = new JsonSerializerOptions
                // {
                //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //     WriteIndented = true
                // };
                // Console.WriteLine("当前有的键: " + JsonSerializer.Serialize(Locations.GetDictionary().Keys.ToArray(), options1));
                continue;
            }
            Location map = locations.GetDictionary()[mapName.Replace("_", "")]; // Locations.GetMappedKey(mapName)
            foreach (AllExtractsExit exit in map.AllExtracts)
            {
                // Console.WriteLine($"map: {mapName}\n exit: {exit}\n data: " + JsonSerializer.Serialize(ExitNames));
                if (exit.Name == null) continue;
                if (localesMap != null && !localesMap.ContainsKey(exit.Name))
                {
                    errorMsg += GetTextFormat("models.local.LM.init.warn0", exit.Name) + "\n";
                    continue;
                }

                if (ExitNames[mapName].ContainsKey(exit.Name))
                {
                    errorMsg += GetTextFormat("models.local.LM.init.warn1", exit.Name, mapName) + "\n";
                    continue;
                }
                if (localesMap != null)
                    ExitNames[mapName].Add(exit.Name, localesMap[exit.Name]);
            }
        }

        // var options = new JsonSerializerOptions
        // {
        //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //     WriteIndented = true
        // };
        // Console.WriteLine(JsonSerializer.Serialize(ExitNames, options));
        // Console.WriteLine("[RaidRecord] " + errorMsg);
        modConfig.Log("Info", errorMsg);
        Console.WriteLine($"[RaidRecord] {GetTextFormat("models.local.LM.init.info0")}");
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_allLocalizations.ContainsKey(value))
                _currentLanguage = value;
        }
    }

    // 优先key, 备用en(ch百分百提供), 最终默认值: 未定义
    public string GetText(string key, string fallback = "未定义")
    {
        _allLocalizations.TryGetValue(CurrentLanguage, out LocalizationData? localization);
        if (localization == null) { return fallback; }
        localization.Translations.TryGetValue(key, out string? result);
        if (result == null)
        {
            localization.Translations.TryGetValue("en", out result);
        }
        return result ?? fallback;
    }

    public string GetTextFormat(string msgId, params object?[] args)
    {
        return string.Format(GetText(msgId), args);
    }

    public string GetMapName(string map)
    {
        return MapNames.GetValueOrDefault(map.Replace("_", "").ToLower(), map);
    }

    public string GetExitName(string map, string key)
    {
        return ExitNames.TryGetValue(map.Replace("_", "").ToLower(), out Dictionary<string, string>? mapExits) ? mapExits.GetValueOrDefault(key, key) : key;
    }

    public string GetArmorZoneName(string key)
    {
        return _allLocalizations[CurrentLanguage].ArmorZone.GetValueOrDefault(key, key);
    }

    public string GetRoleName(string key)
    {
        return _allLocalizations[CurrentLanguage].RoleNames.GetValueOrDefault(key, key);
    }

    // 只读属性, 查看支持的语言
    public List<string> AvailableLanguages => _allLocalizations.Keys.ToList();
}