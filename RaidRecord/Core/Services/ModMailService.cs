using RaidRecord.Core.ChatBot;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.Services;

/// <summary>
/// 模组使用的邮件服务
/// </summary>
[Injectable]
public class ModMailService(
    I18N i18N,
    ModConfig modConfig,
    ItemHelper itemHelper,
    ConfigServer configServer,
    ProfileHelper profileHelper,
    DataGetterService dataGetter,
    PaymentService paymentService,
    MailSendService mailSendService,
    RaidRecordManagerChat modChatBot,
    EventOutputHolder eventOutputHolder
): IDialogueChatBot, IOnLoad
{
    public UserDialogInfo GetChatBot()
    {
        return dataGetter.GetChatBotInfo();
    }

    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        return modChatBot.HandleMessage(this, sessionId, request);
    }

    public Task OnLoad()
    {
        UserDialogInfo chatbot = GetChatBot();
        var coreConfig = configServer.GetConfig<CoreConfig>();
        coreConfig.Features.ChatbotFeatures.Ids[chatbot.Info!.Nickname!] = chatbot.Id;
        coreConfig.Features.ChatbotFeatures.EnabledBots[chatbot.Id] = true;
        // logger.Info($"[RaidRecord] 已经成功注册ChatBot: {chatbot.Id}");
        modConfig.Info(i18N.GetText("MainMod-Info.成功注册ChatBot", new
        {
            ChatBotId = chatbot.Id
        }));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 将消息发给对应sessionId的客户端
    /// </summary>
    public void SendMessage(string sessionId, string msg)
    {
        var details = new SendMessageDetails
        {
            RecipientId = sessionId,
            MessageText = msg,
            Sender = MessageType.UserMessage,
            SenderDetails = GetChatBot()
        };
        mailSendService.SendMessageToPlayer(details);
    }

    /// <summary>
    /// 批量发送消息
    /// </summary>
    public async Task SendAllMessage(string sessionId, string message)
    {
        string[] messages = StringUtil.SplitStringByNewlines(message);
        switch (messages.Length)
        {
            case 0:
                return;
            case 1:
                await Task.Delay(1000);
                SendMessage(sessionId, messages[0]);
                return;
        }

        await Task.Delay(750);

        // 同时有多条消息被启用时, 用来唯一标记
        string messageTag = $"[{messages[0][new Range(0, Math.Min(16, messages[0].Length))]}...]";

        for (int i = 0; i < messages.Length; i++)
        {
            SendMessage(sessionId, messages[i] + $"\n{i + 1}/{messages.Length} tag: {messageTag}");
            if (i < messages.Length - 1)
            {
                await Task.Delay(1250);
            }
        }
    }

    /// <summary>
    /// 按照指定数额进行扣费
    /// </summary>
    /// <returns>如果未成功则返回警告列表</returns>
    public List<Warning>? Payment(MongoId sessionId, long amount, PmcData? pmcData = null)
    {
        ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);
        pmcData ??= profileHelper.GetPmcProfile(sessionId);

        if (pmcData == null)
        {
            return
            [
                new Warning
                {
                    ErrorMessage = $"未获取到Session {sessionId} 对应的玩家存档信息"
                }
            ];
        }

        try
        {
            paymentService.AddPaymentToOutput(
                pmcData,
                Money.ROUBLES,
                amount,
                sessionId,
                output);
        }
        catch (Exception e)
        {
            output.Warnings ??= [];
            output.Warnings.Add(new Warning
            {
                ErrorMessage = $"扣费时出现错误: {e.Message} {e.StackTrace}"
            });
        }

        return output.Warnings;
    }

    /// <summary>
    /// 给玩家发送钱, 且为FIR状态
    /// </summary>
    public List<Warning>? SendMoney(MongoId sessionId, string msg, double amount)
    {
        ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);

        try
        {
            List<Warning>? warnings = SendItemsToPlayer(
                sessionId,
                msg,
                itemHelper.SplitStackIntoSeparateItems(new Item
                {
                    Id = new MongoId(),
                    Template = Money.ROUBLES,
                    Upd = new Upd
                    {
                        StackObjectsCount = amount
                    }
                }).SelectMany(x => x).ToList(),
                isFiRItem: true);
            if (warnings != null)
            {
                foreach (Warning warning in warnings)
                {
                    output.Warnings ??= [];
                    output.Warnings.Add(warning);
                }
                return warnings;
            }
        }
        catch (Exception e)
        {
            output.Warnings ??= [];
            output.Warnings.Add(new Warning
            {
                ErrorMessage = $"扣费时出现错误: {e.Message} {e.StackTrace}"
            });
        }

        return output.Warnings;
    }

    /// <summary>
    /// 将物品以System账户发送给玩家
    /// </summary>
    /// <param name="sessionId">玩家sessionId</param>
    /// <param name="msg">提示信息</param>
    /// <param name="items">物品列表</param>
    /// <param name="isFiRItem">是否令所有物品为突袭中发现物品</param>
    /// <param name="maxStorageTimeSeconds">默认为2天</param>
    /// <returns>如果未成功则返回警告列表</returns>
    public List<Warning>? SendItemsToPlayer(
        MongoId sessionId,
        string msg,
        List<Item>? items,
        bool isFiRItem = true,
        long? maxStorageTimeSeconds = 172800L)
    {
        try
        {
            if (items?.Count <= 0)
            {
                return
                [
                    new Warning
                    {
                        ErrorMessage = "未指定物品"
                    }
                ];
            }
            if (isFiRItem)
            {
                foreach (Item item in items ?? [])
                {
                    item.Upd ??= new Upd();
                    item.Upd.SpawnedInSession = isFiRItem;
                }
            }
            mailSendService.SendSystemMessageToPlayer(
                sessionId,
                msg,
                items,
                maxStorageTimeSeconds);
            return null;
        }
        catch (Exception e)
        {
            return
            [
                new Warning
                {
                    ErrorMessage = $"发送物品时出现错误: {e.Message} {e.StackTrace}"
                }
            ];
        }
    }
}