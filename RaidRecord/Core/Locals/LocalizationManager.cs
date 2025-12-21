using System.Reflection;
using RaidRecord.Core.Configs;
using SPTarkov.Common.Extensions;
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
/// 初始化本地数据
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton)]
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
                if (!file.EndsWith(".json") || fileName.Length != 2) continue;
                // logger.Info($"> {fileName}成功加载");
                try
                {
                    _allLocalizations[fileName] = modHelper.GetJsonDataFromFile<LocalizationData>(localsDir, $"{fileName}.json");
                }
                catch (Exception e)
                {
                    modConfig.Error($"加载本地化数据库时出错: {fileName}", e);
                }
            }

            modConfig.Info($"已加载语言: {string.Join(", ", AvailableLanguages)}");
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

    /// <summary>
    /// 获取本地化值的核心方法
    /// 实现多级回退：当前语言 -> 回退到中文 -> 报错加key的值
    /// </summary>
    public string GetLocalisedValue(string key)
    {
        string errorKey = $"[Error {key}]";
        // 获取当前区域的本地化字典
        if (!_allLocalizations.TryGetValue(CurrentLanguage, out LocalizationData? locales))
        {
            return !_allLocalizations.TryGetValue("ch", out LocalizationData? defaults1)
                ? errorKey
                : // 区域未加载，返回键本身
                defaults1.Translations.GetValueOrDefault(key, errorKey); // 区域未加载，返回键本身
        }
        if (locales.Translations.TryGetValue(key, out string? value))
        {
            return value;
        }
        return !_allLocalizations.TryGetValue("ch", out LocalizationData? defaults2)
            ? errorKey
            : // 区域未加载，返回键本身
            defaults2.Translations.GetValueOrDefault(key, errorKey); // 区域未加载，返回键本身
    }

    /// <summary>
    /// 处理带参数的本地化字符串
    /// 将{{属性名}}替换为参数对象的属性值
    /// </summary>
    protected string GetLocalised(string key, object? args)
    {
        string rawLocalizedString = GetLocalisedValue(key);
        if (args == null) return rawLocalizedString;

        PropertyInfo[] typeProperties = args.GetType().GetProperties();

        foreach (PropertyInfo propertyInfo in typeProperties)
        {
            // 获取JSON属性名（支持[JsonProperty]特性）
            string localizedName = $"{{{{{propertyInfo.GetJsonName()}}}}}";
            if (rawLocalizedString.Contains(localizedName))
            {
                rawLocalizedString = rawLocalizedString.Replace(
                    localizedName,
                    propertyInfo.GetValue(args)?.ToString() ?? string.Empty
                );
            }
        }

        return rawLocalizedString;
    }

    /// <summary>
    /// 获取本地化文本（主要公共方法）
    /// </summary>
    /// <param name="key">本地化键</param>
    /// <param name="args">可选参数对象，属性将替换字符串中的{{属性名}}</param>
    /// <returns>本地化后的字符串</returns>
    public string GetText(string key, object? args = null)
    {
        return args is null ? GetLocalisedValue(key) : GetLocalised(key, args);
    }

    /// <summary>
    /// 获取小写去除_的地图名称, 作为字典的键
    /// </summary>
    private string GetMapKey(string mapName)
    {
        return mapName.Replace("_", "").ToLower();
    }

    protected void InitLocalization(Locations locations, LocaleBase locales)
    {
        // 所有的地图名称
        string[] mapNames =
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
        Dictionary<string, string>? localesMap = locales.Global[CurrentLanguage].Value;
        string warnMsg = "";

        // 获取地图的本地化表示
        foreach (string mapName in mapNames)
        {
            MapNames[GetMapKey(mapName)]
                = localesMap?.TryGetValue(mapName, out string? name) ?? false ? name : mapName;
        }

        modConfig.Debug($"已加载地图: {string.Join(", ", MapNames.Values.Select(x => x.ToString()))}");

        Dictionary<string, Location> sptLocations = locations.GetDictionary();

        foreach (string mapName in mapNames)
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
                    warnMsg += $"警告: {exit.Name}不存在于本地化字典中" + "\n";
                    continue;
                }

                if (ExitNames[mapKey].ContainsKey(exit.Name))
                {
                    warnMsg += $"警告: 尝试重复添加撤离点{exit.Name}到{mapName}的数据" + "\n";
                    continue;
                }
                if (localesMap != null)
                    ExitNames[mapKey].Add(exit.Name, localesMap[exit.Name]);
            }
        }

        if (!string.IsNullOrEmpty(warnMsg)) modConfig.Log("Warn", warnMsg);
        modConfig.Info("已成功加载各个地图撤离点数据");
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
        return _allLocalizations[CurrentLanguage].ArmorZone.GetValueOrDefault(key, key);
    }

    /// <summary>
    /// 获取角色本地化名称
    /// </summary>
    public string GetRoleName(string key)
    {
        return _allLocalizations[CurrentLanguage].RoleNames.GetValueOrDefault(key, key);
    }

    // 只读属性, 查看支持的语言
    public List<string> AvailableLanguages => _allLocalizations.Keys.ToList();
}