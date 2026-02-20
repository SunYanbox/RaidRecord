using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ClsCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly DataGetterService _dataGetter;
    private readonly I18NMgr _i18NMgr;
    private I18N I18N => _i18NMgr.I18N!;

    public ClsCmd(CmdUtil cmdUtil, I18NMgr i18NMgr,
        DataGetterService dataGetter)
    {
        _cmdUtil = cmdUtil;
        Key = "cls";
        _i18NMgr = i18NMgr;
        Desc = "serverMessage.Cmd-Cls.Desc".Translate(I18N);
        _dataGetter = dataGetter;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        UserDialogInfo managerProfile = _dataGetter.GetChatBotInfo();

        Dictionary<MongoId, Dialogue> dialogs = _dataGetter.GetDialogsForProfile(parametric.SessionId);
        Dialogue dialog = dialogs[managerProfile.Id];
        // if (dialog.Messages == null) return "找不到你的聊天记录";
        if (dialog.Messages == null) return "serverMessage.Cmd-Cls.找不到聊天记录".Translate(I18N);
        int count = dialog.Messages.Count;
        dialog.Messages = [];
        // $"已清除{count}条聊天记录, 重启游戏客户端后生效"
        // return $"已清除{count}条聊天记录, 重启游戏客户端后生效";
        return "serverMessage.Cmd-Cls.已清除聊天记录".Translate(I18N, new { Count = count });
    }
}