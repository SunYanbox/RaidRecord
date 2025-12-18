namespace RaidRecord.Core.ChatBot.Commands;

/// <summary>
/// 命令参数 | 参与命令调用的参数
/// </summary>
public class Parametrics(string sessionId, RaidRecordManagerChat managerChat)
{
    public string SessionId { get; set; } = sessionId;
    public RaidRecordManagerChat? ManagerChat { get; set; } = managerChat;
    public Dictionary<string, string> Paras { get; set; } = new();

}