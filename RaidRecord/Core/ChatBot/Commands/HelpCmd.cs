using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class HelpCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly string _displayMessage;

    public HelpCmd(CmdUtil cmdUtil, LocalizationManager local)
    {
        _cmdUtil = cmdUtil;
        Key = "help";
        Desc = local.GetText("Cmd-Help.Desc");
        _displayMessage = local.GetText("Cmd-Help.显示文本");
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        // "帮助信息(参数需要按键值对写, 例如\"list index 1\"; 中括号表示可选参数; 指令与参数不区分大小写):"
        string msg = _displayMessage;
        if (parametric.ManagerChat == null)
        {
            _cmdUtil.ModConfig!.LogError(
                new NullReferenceException(nameof(parametric.ManagerChat)),
                "RaidRecordManagerChat.GetHelpCommand",
                msg);
            return msg;
        }
        foreach (CommandBase cmd in parametric.ManagerChat.Commands.Values)
        {
            msg += $"\n - {cmd.Key}: {cmd.Desc}\n";
        }
        return msg;
    }
}