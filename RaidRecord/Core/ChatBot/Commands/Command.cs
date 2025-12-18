namespace RaidRecord.Core.ChatBot.Commands;

public class Command
{
    public string? Key { get; set; }
    public string? Desc { get; set; }
    public ParaInfo? ParaInfo { get; set; }
    public Parametric? Paras { get; set; }
    public CommandCallback? Callback { get; set; }
}