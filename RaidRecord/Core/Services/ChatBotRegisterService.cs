using RaidRecord.Core.Locals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SuntionCore.Services.I18NUtil;
using SuntionCore.Services.LogUtils;

namespace RaidRecord.Core.Services;

/// <summary>
/// 注册聊天机器人
/// </summary>
[Injectable]
public class ChatBotRegisterService(
    I18NMgr i18NMgr,
    ConfigServer configServer,
    DataGetterService dataGetter
): IOnLoad
{
    public Task OnLoad()
    {
        UserDialogInfo chatbot = dataGetter.GetChatBotInfo();
        var coreConfig = configServer.GetConfig<CoreConfig>();
        coreConfig.Features.ChatbotFeatures.Ids[chatbot.Info!.Nickname!] = chatbot.Id;
        coreConfig.Features.ChatbotFeatures.EnabledBots[chatbot.Id] = true;
        // logger.Info($"[RaidRecord] 已经成功注册ChatBot: {chatbot.Id}");
        ModLogger.GetOrCreateLogger("RaidRecord").Info("z2serverMessage.MainMod-Info.成功注册ChatBot".Translate(i18NMgr.I18N!, new
        {
            ChatBotId = chatbot.Id
        }));
        return Task.CompletedTask;
    }
}