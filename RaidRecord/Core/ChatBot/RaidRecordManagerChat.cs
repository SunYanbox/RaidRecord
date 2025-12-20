using Microsoft.Extensions.DependencyInjection;
using RaidRecord.Core.ChatBot.Commands;
using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.ChatBot;

[Injectable(InjectionType.Singleton)]
public class RaidRecordManagerChat(
    MailSendService mailSendService,
    ISptLogger<RaidRecordManagerChat> logger,
    ModConfig modConfig,
    IServiceProvider serviceProvider,
    CmdUtil cmdUtil): IDialogueChatBot, IOnLoad
{
    public readonly Dictionary<string, CommandBase> Commands = new();

    public Task OnLoad()
    {
        InitCommands();
        return Task.CompletedTask;
    }

    public UserDialogInfo GetChatBot()
    {
        return new UserDialogInfo
        {
            Id = "68e2d45e17ea301214c2596d",
            Aid = 8100860,
            Info = new UserDialogDetails
            {
                Nickname = cmdUtil.GetLocalText("RC MC.ChatBot.NickName"),
                Side = "Usec",
                Level = 69,
                MemberCategory = MemberCategory.Sherpa,
                SelectedMemberCategory = MemberCategory.Sherpa
            }
        };
    }

    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        try
        {
            SendAllMessage(sessionId, HandleCommand(request.Text, sessionId)).Wait();
        }
        catch (Exception e)
        {
            // this.error(e.name);
            // this.error(e.message);
            // this.error(e.stack);
            logger.Error($"[RaidRecord]<Chat> {cmdUtil.GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message)}");
            modConfig.LogError(e, "RaidRecordManagerChat.HandleMessage", cmdUtil.GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message));
            SendMessage(sessionId, cmdUtil.GetLocalText("RC MC.Chat.HM.error1", request.Text, e.Message));
        }
        return ValueTask.FromResult(request.DialogId);
    }

    private void RegisterCommand<T>() where T : CommandBase
    {
        var command = serviceProvider.GetService<T>();
        if (command != null)
        {
            AddCmd(command);
        }
        else
        {
            modConfig.Warn($"{typeof(T).Name} 无法从IServiceProvider获取");
        }
    }

    // 注册命令
    protected void InitCommands()
    {
        try
        {
            RegisterCommand<HelpCmd>();
            RegisterCommand<ClsCmd>();
            RegisterCommand<InfoCmd>();
            RegisterCommand<ListCmd>();
            RegisterCommand<ItemsCmd>();

            logger.Info($"[RaidRecord] {cmdUtil.GetLocalText("RC MC.Chat.initCmd.info0", string.Join(", ", Commands.Keys.ToArray()))}");
        }
        catch (Exception e)
        {
            modConfig.Error("RaidRecordManagerChat.InitCommands中出现错误: ", e);
            throw;
        }
    }

    private void AddCmd(CommandBase commandBase)
    {
        DataUtil.UpdateCommandDesc(commandBase);
        if (commandBase.Key == null)
        {
            modConfig.Error($"[RaidRecord]<Chat> 添加命令 {commandBase.GetType().Name} 失败, 缺少Key属性");
            return;
        }
        Commands[commandBase.Key] = commandBase;
    }

    private string HandleCommand(string command, string sessionId)
    {
        string[] data = StringUtil.SplitCommand(command.ToLower());
        if (data.Length <= 0)
        {
            return cmdUtil.GetLocalText("RC MC.Chat.handleCmd.error0");
        }

        // logger.Info($"全部命令: {string.Join(", ", _commands.Keys.ToArray())}, 输入的指令: \"{command}\", 检测出的指令: {data[0]}");
        if (!Commands.ContainsKey(data[0]))
        {
            return cmdUtil.GetLocalText("RC MC.Chat.handleCmd.error1", data[0], string.Join(",", Commands.Keys.ToArray()));
        }
        CommandBase iCmd = Commands[data[0]];
        iCmd.Paras = new Parametric(sessionId, this);

        int index = 1;
        while (index >= 1 && index < data.Length)
        {
            if (!string.IsNullOrEmpty(data[index + 1]))
            {
                iCmd.Paras.Paras[data[index]] = data[index + 1];
                index += 1;
            }
            index += 1;
        }
        string result = iCmd.Execute(iCmd.Paras);
        // 垃圾回收 低效 未来再优化
        iCmd.Paras.ManagerChat = null;
        iCmd.Paras.Paras.Clear();
        iCmd.Paras = null;
        return result;
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
}