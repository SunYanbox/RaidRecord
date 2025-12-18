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
        return $"{time / 3600}h {(time % 3600) / 60}min {time % 60}s";
    }
    
    /// <summary>
    /// 用所有空白字符（空格、制表符、换行符等）分隔命令字符串, 并过滤掉空字符串元素
    /// </summary>
    public static string[] SplitCommand(string cmd)
    {
        // 根据指定的分隔字符和选项将字符串拆分成子串。
        return cmd.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
    }
    
    /// <summary>
    /// 分隔要发送的字符串, 避免客户端无法完整显示
    /// </summary>
    public static string[] SplitStringByNewlines(string str)
    {
        // 如果字符串长度不超过限制，直接返回包含原字符串的数组
        if (str.Length <= Constants.SendLimit)
        {
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
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        int year = date.Year;
        int month = date.Month;
        int day = date.Day;
        string time = date.ToShortTimeString();
        
        return $"{year}年{month}月{day}日 {time}";
    }
}