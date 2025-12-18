using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using Path = System.IO.Path;

namespace RaidRecord.Models;

// 主本地化数据类
public class LocalizationData
{
    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
}

// 第一时间初始化本地数据
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public class LocalizationManager(
    ISptLogger<LocalizationManager> logger, 
    ModHelper modHelper, 
    ModConfig  modConfig,
    DatabaseServer databaseServer): IOnLoad
{
    public readonly Dictionary<string, Dictionary<string, string>> ExitNames = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, LocalizationData> _allLocalizations = new Dictionary<string, LocalizationData>();
    private string _currentLanguage = "ch"; // 默认语言

    public Task OnLoad()
    {
        var localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        localsDir = Path.Combine(localsDir, "db\\locals");
        if (Directory.Exists(localsDir))
        {
            // logger.Info($"本地化数据库存在: {localsDir}");
            foreach (var file in Directory.GetFiles(localsDir))
            {
                // eg: ch.json
                
                var fileName = Path.GetFileNameWithoutExtension(file);
                // logger.Info($"尝试加载{localsDir}文件夹下的{fileName}, file: {file}");
                if (file.EndsWith(".json") && fileName.Length == 2)
                {
                    // logger.Info($"> {fileName}成功加载");
                    _allLocalizations[fileName] = modHelper.GetJsonDataFromFile<LocalizationData>(localsDir, $"{fileName}.json");
                }
            }

            
            
            logger.Info($"[RaidRecord] {GetText("68e5af1cbe25230394726423")
                .Replace("<AvailableLanguages>", string.Join(", ", AvailableLanguages))}");
        }
        else
        {
            logger.Error($"[RaidRecord] {GetText("68e5af1cbe25230394726424")
                .Replace("<localsDir>", localsDir)}");
        }

        CurrentLanguage = modConfig.Configs.Local;
        
        var tables = databaseServer.GetTables();
        InitLocalization(tables.Locations, tables.Locales);
        return Task.CompletedTask;
    }

    protected void InitLocalization(Locations locations, LocaleBase locales)
    {
        // 所有的地图名称
        string[] mapNames = [
            "woods", 
            "bigmap", 
            // "develop",
            "factory4day", // factory4_day
            "factory4night", // factory4_night
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
            "tarkovstreets",
            // "terminal",
            // "town"
        ];
        var localesMap = locales.Global[CurrentLanguage].Value;
        var errorMsg = "";
        foreach (var mapName in mapNames)
        {
            ExitNames.Add(mapName, new Dictionary<string, string>());
            
            if (!locations.GetDictionary().ContainsKey(mapName))
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
            var map = locations.GetDictionary()[mapName]; // Locations.GetMappedKey(mapName)
            foreach (var exit in map.AllExtracts)
            {
                // Console.WriteLine($"map: {mapName}\n exit: {exit}\n data: " + JsonSerializer.Serialize(ExitNames));
                if (exit.Name == null) continue;
                if (!localesMap.ContainsKey(exit.Name))
                {
                    errorMsg += GetText("68e5af1cbe25230394726425").Replace("<exit.Name>", exit.Name) + "\n";
                    continue;
                }

                if (ExitNames[mapName].ContainsKey(exit.Name))
                {
                    errorMsg += GetText("68e5af1cbe25230394726426").Replace("<exit.Name>", exit.Name).Replace("<mapName>", mapName) + "\n";
                    continue;
                }
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
        Console.WriteLine($"[RaidRecord] {GetText("68e5af1dbe25230394726427")}");
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
        _allLocalizations.TryGetValue(CurrentLanguage, out var localization);
        if (localization == null) { return fallback; }
        localization.Translations.TryGetValue(key, out var result);
        if (result == null)
        {
            localization.Translations.TryGetValue("en", out result);
        }
        return result ?? fallback;
    }

    public string GetExitName(string map, string key)
    {
        return ExitNames.TryGetValue(map.Replace("_", "").ToLower(), out var mapExits) ? (mapExits.TryGetValue(key, out var name) ? name : key) : key;
    }
    
    // 只读属性, 查看支持的语言
    public List<string> AvailableLanguages => _allLocalizations.Keys.ToList<string>();
}