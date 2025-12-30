using System.Reflection;
using RaidRecord.Core.Configs;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
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
public class I18N(
    ModHelper modHelper,
    ModConfig modConfig,
    ISptLogger<I18N> logger,
    DatabaseServer databaseServer): IOnLoad
{
    public readonly Dictionary<string, string> MapNames = new();
    public readonly Dictionary<string, Dictionary<string, string>> ExitNames = new();
    private readonly Dictionary<string, I18NData> _allTrans = new();
    private Dictionary<string, string>? _sptLocals;

    /// <summary> 获取当前语言对应的SPT数据库 </summary>
    public Dictionary<string, string>? SptLocals => _sptLocals ??= GetSptLocals();

    /// <summary> 当前语言 </summary>
    public string CurrLang
    {
        get => _currentLang;
        set
        {
            if (!_allTrans.ContainsKey(value)) return;
            if (_currentLang != value)
                _sptLocals = null;
            _currentLang = value;
        }
    }
    
    /// <summary> 当前语言 </summary>
    public I18NData CurrLangData => _allTrans[_currentLang];
    
    /// <summary> 当前WebUILocal <remarks>获取不到WebUI回退到中文</remarks>> </summary>
    public WebUILocal WebUILocal => CurrLangData.WebUI ??= new WebUILocal();
    
    /// <summary> 只读属性, 查看支持的语言 </summary>
    public List<string> AvailableLang => _allTrans.Keys.ToList();

    private string _currentLang = "ch"; // 默认语言

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
        string localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        localsDir = Constants.DBLocalsFolderPath(localsDir, modHelper);
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
                    _allTrans[fileName] = modHelper.GetJsonDataFromFile<I18NData>(localsDir, $"{fileName}.json");
                    _allTrans[fileName].WebUI ??= new WebUILocal();
                }
                catch (Exception e)
                {
                    modConfig.Error($"加载本地化数据库时出错: {fileName}", e);
                }
            }

            CurrLang = modConfig.Configs.Local;

            // 从这里加载完毕
            modConfig.Info(GetText("I18N-Info.已加载语言信息", new
            {
                AvailableLang = string.Join(", ", AvailableLang),
                CurrLang
            }));
            // modConfig.Info($"已加载语言: {string.Join(", ", AvailableLang)}");
        }
        else
        {
            // 没加载成功就中英分别输出一遍
            logger.Error($"[RaidRecord] 本地化数据库不存在: {localsDir}");
            logger.Error($"[RaidRecord] Localisation database does not exist: {localsDir}");
        }


        if (modConfig.Configs.AutoUnloadOtherLang)
        {
            // 卸载不用的语言内存
            foreach (string language in _allTrans.Keys.ToArray())
            {
                if (language != "ch" && language != CurrLang)
                {
                    _allTrans.Remove(language);
                }
            }
        }

        DatabaseTables tables = databaseServer.GetTables();
        InitI18N(tables.Locations, tables.Locales);
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
        if (!_allTrans.TryGetValue(CurrLang, out I18NData? locales))
        {
            return !_allTrans.TryGetValue("ch", out I18NData? defaults1)
                ? errorKey
                : // 区域未加载，返回键本身
                defaults1.AllTrans.GetValueOrDefault(key, errorKey); // 区域未加载，返回键本身
        }
        if (locales.AllTrans.TryGetValue(key, out string? value))
        {
            return value;
        }
        return !_allTrans.TryGetValue("ch", out I18NData? defaults2)
            ? errorKey
            : // 区域未加载，返回键本身
            defaults2.AllTrans.GetValueOrDefault(key, errorKey); // 区域未加载，返回键本身
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
    /// 基于物品TplID获取物品本地化名称
    /// </summary>
    /// <returns>本地化后的字符串</returns>
    public string GetItemName(MongoId temple)
    {
        return SptLocals?.GetValueOrDefault($"{temple} Name") ?? temple.ToString();
    }

    /// <summary>
    /// 获取小写去除_的地图名称, 作为字典的键
    /// </summary>
    private static string GetMapKey(string mapName)
    {
        return mapName.Replace("_", "").ToLower();
    }

    /// <summary>
    /// 获取Spt本地化字典
    /// </summary>
    public Dictionary<string, string>? GetSptLocals()
    {
        return databaseServer.GetTables().Locales.Global[CurrLang].Value;
    }

    protected void InitI18N(Locations locations, LocaleBase locales)
    {
        Dictionary<string, string>? localesMap = locales.Global[CurrLang].Value;
        string warnMsg = "";

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
                    warnMsg += GetText("I18N-Warn.撤离点名称不存在", new
                    {
                        ExitName = exit.Name
                    });
                    // warnMsg += $"警告: 撤离点{exit.Name}不存在于SPT本地化字典中" + "\n";
                    continue;
                }

                if (ExitNames[mapKey].ContainsKey(exit.Name))
                {
                    warnMsg += GetText("I18N-Warn.重复添加撤离点", new
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
        modConfig.Info(GetText("I18N-Info.撤离点数据加载完毕", new
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
        return _allTrans[CurrLang].ArmorZone.GetValueOrDefault(key, key);
    }

    /// <summary>
    /// 获取角色本地化名称
    /// </summary>
    public string GetRoleName(string key)
    {
        return _allTrans[CurrLang].RoleNames.GetValueOrDefault(key, key);
    }
}