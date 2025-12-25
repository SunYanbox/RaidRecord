using System.Text.RegularExpressions;

namespace RaidRecord.Core.Utils;

/// <summary>
/// 字符串处理与格式化
/// </summary>
public static class StringUtil
{
    /// <summary>
    /// 精确到h-min-s的格式化时间
    /// </summary>
    public static string TimeString(long time)
    {
        return $"{time / 3600}h {time % 3600 / 60}min {time % 60}s";
    }

    /// <summary>
    /// 用所有空白字符（空格、制表符、换行符等）分隔命令字符串, 并过滤掉空字符串元素
    /// </summary>
    public static string[] SplitCommand(string cmd)
    {
        if (cmd.Length > 0 && cmd.Count(c => c == '"') % 2 == 1)
        {
            cmd += '"';
        }

        // 提取所有引号内的内容并用标记替换
        var markers = new Dictionary<string, string>();
        int markerIndex = 0;

        // 正则匹配成对的引号内容
        const string pattern = "\"([^\"]*)\"";
        string processedCmd = Regex.Replace(cmd, pattern, match =>
        {
            string quotedContent = match.Groups[1].Value;
            string marker = $"__MARKER_{markerIndex++}__";
            markers[marker] = quotedContent;
            return marker;
        });

        // 按空白字符分割
        string[] parts = processedCmd.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        // 恢复标记为原始内容
        for (int i = 0; i < parts.Length; i++)
        {
            if (markers.TryGetValue(parts[i], out string? originalContent))
            {
                parts[i] = originalContent;
            }
        }

        return parts;
    }

    /// <summary>
    /// 分隔要发送的字符串, 避免客户端无法完整显示
    /// </summary>
    public static string[] SplitStringByNewlines(string str)
    {
        switch (str.Length)
        {
            case 0:
                return [];
            // 如果字符串长度不超过限制，直接返回包含原字符串的数组
            case <= Constants.SendLimit:
                return [str];
        }

        // 用换行符分割
        string[] segments = str.Split('\n');
        List<string> result = [];
        string currentSegment = "";

        foreach (string segment in segments)
        {
            string potentialSegment = currentSegment != "" ? currentSegment + "\n" + segment : segment;

            if (potentialSegment.Length < Constants.SendLimit)
            {
                currentSegment = potentialSegment;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSegment))
                {
                    result.Add(currentSegment);
                }
                currentSegment = segment;
            }
        }

        // 添加最后一个分段
        if (!string.IsNullOrEmpty(currentSegment))
        {
            result.Add(currentSegment);
        }

        return result.ToArray();
    }

    public static string DateFormatterFull(long timestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        int year = date.Year;
        int month = date.Month;
        int day = date.Day;
        string time = date.ToShortTimeString();

        return $"{year}年{month}月{day}日 {time}";
    }
}