using System.Text.Json;
using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Models;
using SPTarkov.DI.Annotations;

namespace RaidRecord.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class DataUtil(ModConfig config)
{
    private readonly string[] _sizeUnits = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
    
    /// <summary>
    /// 将字节数格式化为可读的文件大小
    /// </summary>
    public string GetFileSize(string filePath, int decimalPlaces = 2)
    {
        string emptySize = $"0 {_sizeUnits[0]}";
        
        if (!File.Exists(filePath)) return emptySize;

        long bytes = 0;
        
        try
        {
            bytes = new FileInfo(filePath).Length;
        }
        catch (Exception e)
        {
            config.Error($"获取文件尺寸时出错: {e.Message}", e, false);
        }
        
        if (bytes == 0) return emptySize;
        
        var magnitude = (int)Math.Floor(Math.Log(bytes, 1024));
        magnitude = Math.Min(magnitude, _sizeUnits.Length - 1);
        
        var adjustedSize = bytes / Math.Pow(1024, magnitude);
        return $"{adjustedSize.ToString($"F{decimalPlaces}")} {_sizeUnits[magnitude]}";
    }
    
    // 根据 ParaInfo 属性更新一条命令使用指南, 追加到原有的 desc 后
    public static void UpdateCommandDesc(CommandBase command)
    {
        string desc = $"> {command.Key}";

        if (command.ParaInfo == null || command.ParaInfo.Paras.Count <= 0)
        {
            command.Desc += desc;
            return;
        }

        foreach (string para in command.ParaInfo.Paras)
        {
            bool isOptional = command.ParaInfo.Optional.Contains(para);
            string type = command.ParaInfo.Types.GetValueOrDefault(para, "undefined");
            string centerString = $"{para}: {type}";
            desc += " " + (isOptional ? $"[{centerString}]" : centerString);
        }

        if (command.ParaInfo.Descs.Count > 0)
        {
            foreach (string para in command.ParaInfo.Paras)
            {
                if (command.ParaInfo.Descs.TryGetValue(para, out string? infoDesc))
                {
                    desc += $"\n\t> {para}: {infoDesc}";
                }
            }
        }

        command.Desc += desc;
    }

    /**
     * 获取字典的子集
     * @param dict 原字典
     * @param keys 键的列表
     * @returns
     */
    public static Dictionary<TKey, TValue> GetSubDict<TKey, TValue>(
            Dictionary<TKey, TValue> dict,
            IEnumerable<TKey> keys)
        // 限制泛型类型参数 TKey 必须是非空类型
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (TKey key in keys)
        {
            if (dict.TryGetValue(key, out TValue? value))
            {
                result[key] = value;
            }
        }
        return result;
    }

    public static RaidInfo Copy(RaidInfo raidInfo)
    {
        RaidInfo copy = raidInfo with {};
        return copy;
    }



    public static T Copy<T>(T source)
    {
        string json = JsonSerializer.Serialize(source);
        var copy = JsonSerializer.Deserialize<T>(json);
        return copy ?? throw new Exception("复制数据出错");
    }




}