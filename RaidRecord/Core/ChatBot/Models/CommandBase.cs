namespace RaidRecord.Core.ChatBot.Models;

public abstract class CommandBase
{
    public string? Key { get; init; }
    public string? Desc { get; set; }
    public ParaInfo? ParaInfo { get; init; } = new();
    public Parametric? Paras { get; set; }

    public abstract string Execute(Parametric parametric);
}