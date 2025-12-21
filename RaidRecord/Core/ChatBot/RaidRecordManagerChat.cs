using Microsoft.Extensions.DependencyInjection;
using RaidRecord.Core.ChatBot.Commands;
using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Services;

namespace RaidRecord.Core.ChatBot;

[Injectable(InjectionType.Singleton)]
public class RaidRecordManagerChat(
    MailSendService mailSendService,
    ModConfig modConfig,
    LocalizationManager localizationManager,
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
        return cmdUtil.GetChatBot();
    }

    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        try
        {
            SendAllMessage(sessionId, HandleCommand(request.Text, sessionId)).Wait();
        }
        catch (Exception e)
        {
            modConfig.Error(
                localizationManager.GetText(
                        "Chatbot-Error.指令处理失败",
                        new { SessionId = sessionId }
                    ), e);
            // modConfig.Error($"用户{sessionId}输入的指令处理失败: ", e);
            SendMessage(sessionId, localizationManager.GetText(
                "Chatbot-Mail.发送指令处理失败信息",
                new
                {
                    SendText = request.Text,
                    ErrorMessage = e.Message
                }
            ));
            // SendMessage(sessionId, $"指令处理失败: {request.Text}\n请检查你输入的指令: '{e.Message}'");
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
            modConfig.Warn(localizationManager.GetText(
                "Chatbot-Warn.无法从DI解析命令实例",
                new { CommandType = typeof(T).Name }
                ));
            // modConfig.Warn($"{typeof(T).Name} 无法从IServiceProvider获取");
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

            modConfig.Info(localizationManager.GetText(
                "Chatbot-Info.命令初始化完毕",
                new { WhichCommandsRegister = string.Join(", ", Commands.Keys.ToArray()) }
                ));
            // logger.Info($"[RaidRecord] 对局战绩管理命令({string.Join(", ", Commands.Keys.ToArray())})已注册");
        }
        catch (Exception e)
        {
            modConfig.Error(localizationManager.GetText(
                "Chatbot-Error.命令初始化失败",
                new { ErrorMessage = e.Message }
            ), e);
            // modConfig.Error("RaidRecordManagerChat.InitCommands中出现错误: ", e);
            throw;
        }
    }

    private void AddCmd(CommandBase commandBase)
    {
        DataUtil.UpdateCommandDesc(commandBase);
        if (commandBase.Key == null)
        {
            modConfig.Error(localizationManager.GetText(
                "Chatbot-Error.添加命令失败.缺少键",
                new { CommandType = commandBase.GetType().Name }
            ));
            // modConfig.Error($"[RaidRecord]<Chat> 添加命令 {commandBase.GetType().Name} 失败, 缺少Key属性");
            return;
        }
        Commands[commandBase.Key] = commandBase;
    }

    private string HandleCommand(string command, string sessionId)
    {
        string[] data = StringUtil.SplitCommand(command.ToLower());
        if (data.Length <= 0)
        {
            return localizationManager.GetText("Chatbot-Error.未输入任何命令");
        }

        // logger.Info($"全部命令: {string.Join(", ", _commands.Keys.ToArray())}, 输入的指令: \"{command}\", 检测出的指令: {data[0]}");
        if (!Commands.TryGetValue(data[0], out CommandBase? iCmd))
        {
            return localizationManager.GetText(
                "Chatbot-Error.未知的命令",
                new { Command = data[0], AvailableCommands = string.Join(",", Commands.Keys.ToArray()) }
            );
            // return $"未知的命令: {data[0]}, 可用的命令包括: {string.Join(",", Commands.Keys.ToArray())}";
        }

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
        string result = string.Empty;
        try
        {
            result = iCmd.Execute(iCmd.Paras);
        }
        catch (Exception e)
        {
            result += e.Message;
            modConfig.Error(localizationManager.GetText(
                "Chatbot-Error.命令执行失败",
                new { Command = iCmd.GetType().Name, ErrorMessage = e.Message }
                ), e);
            modConfig.Error($"RaidRecordManagerChat.HandleCommand中{iCmd.GetType().Name}执行时出现错误: ", e);
        }
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