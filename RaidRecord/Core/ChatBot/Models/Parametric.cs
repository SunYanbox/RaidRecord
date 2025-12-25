namespace RaidRecord.Core.ChatBot.Models;

/// <summary>
/// 命令参数 | 参与命令调用的参数
/// </summary>
public class Parametric(string sessionId, string cmd, RaidRecordManagerChat managerChat)
{
    public string SessionId { get; set; } = sessionId;
    public RaidRecordManagerChat? ManagerChat { get; set; } = managerChat;
    public Dictionary<string, string> Paras { get; set; } = new();
    public string Command = cmd;
}