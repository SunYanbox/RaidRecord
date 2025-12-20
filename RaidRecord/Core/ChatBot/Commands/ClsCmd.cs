using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ClsCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly DialogueHelper _dialogueHelper;

    public ClsCmd(CmdUtil cmdUtil, DialogueHelper dialogueHelper)
    {
        _cmdUtil = cmdUtil;
        _dialogueHelper = dialogueHelper;
        Key = "cls";
        Desc = cmdUtil.GetLocalText("Command.Cls.Desc");
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        UserDialogInfo managerProfile = _cmdUtil.GetChatBot();

        Dictionary<MongoId, Dialogue> dialogs = _dialogueHelper.GetDialogsForProfile(parametric.SessionId);
        Dialogue dialog = dialogs[managerProfile.Id];
        if (dialog.Messages == null) return _cmdUtil.GetLocalText("Command.Cls.error0");
        int count = dialog.Messages.Count;
        dialog.Messages = [];
        // $"已清除{count}条聊天记录, 重启游戏客户端后生效"
        return _cmdUtil.GetLocalText("Command.Cls.info0", count);
        // "找不到你的聊天记录"
    }
}