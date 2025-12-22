using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
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
    private readonly I18N _i18N;

    public ClsCmd(CmdUtil cmdUtil, DialogueHelper dialogueHelper, I18N i18N)
    {
        _cmdUtil = cmdUtil;
        _dialogueHelper = dialogueHelper;
        Key = "cls";
        Desc = i18N.GetText("Cmd-Cls.Desc");
        _i18N = i18N;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        UserDialogInfo managerProfile = _cmdUtil.GetChatBot();

        Dictionary<MongoId, Dialogue> dialogs = _dialogueHelper.GetDialogsForProfile(parametric.SessionId);
        Dialogue dialog = dialogs[managerProfile.Id];
        // if (dialog.Messages == null) return "找不到你的聊天记录";
        if (dialog.Messages == null) return _i18N.GetText("Cmd-Cls.找不到聊天记录");
        int count = dialog.Messages.Count;
        dialog.Messages = [];
        // $"已清除{count}条聊天记录, 重启游戏客户端后生效"
        // return $"已清除{count}条聊天记录, 重启游戏客户端后生效";
        return _i18N.GetText("Cmd-Cls.已清除聊天记录", new { Count = count });
    }
}