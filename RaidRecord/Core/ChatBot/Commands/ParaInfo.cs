namespace RaidRecord.Core.ChatBot.Commands;

/// <summary>
/// 参数信息 | 仅用于help获取命令信息
/// </summary>
public class ParaInfo
{
    public List<string> Paras { get; set; } = [];
    public Dictionary<string, string> Types { get; set; } = new();
    public Dictionary<string, string> Descs { get; set; } = new();
    public HashSet<string> Optional { get; set; } = [];

}