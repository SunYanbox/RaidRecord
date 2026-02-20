using RaidRecord.Core.ChatBot.Commands;
using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.ChatBot;

[Injectable(InjectionType.Singleton)]
public class RaidRecordManagerChat(
    I18NMgr i18NMgr,
    ModConfig modConfig,
    IServiceProvider serviceProvider): IOnLoad
{
    public readonly Dictionary<string, CommandBase> Commands = new();
    private I18N I18N => i18NMgr.I18N!;

    public Task OnLoad()
    {
        InitCommands();
        return Task.CompletedTask;
    }

    public ValueTask<string> HandleMessage(ModMailService modMailService, MongoId sessionId, SendMessageRequest request)
    {
        try
        {
            modMailService.SendAllMessage(sessionId, HandleCommand(request.Text, sessionId)).Wait();
        }
        catch (Exception e)
        {
            modConfig.Error(
                "Chatbot-Error.指令处理失败".Translate(
                    I18N,
                    new { SessionId = sessionId }
                ), e);
            // modConfig.Error($"用户{sessionId}输入的指令处理失败: ", e);
            modMailService.SendMessage(sessionId, "Chatbot-Mail.发送指令处理失败信息".Translate(
                I18N,
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
            modConfig.Warn("Chatbot-Warn.无法从DI解析命令实例".Translate(
                I18N,
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
            RegisterCommand<PriceCmd>();
            RegisterCommand<BuyCmd>();

            modConfig.Info("Chatbot-Info.命令初始化完毕".Translate(
                I18N,
                new { WhichCommandsRegister = string.Join(", ", Commands.Keys.ToArray()) }
            ));
            // logger.Info($"[RaidRecord] 对局战绩管理命令({string.Join(", ", Commands.Keys.ToArray())})已注册");
        }
        catch (Exception e)
        {
            modConfig.Error("Chatbot-Error.命令初始化失败".Translate(
                I18N,
                new { ErrorMessage = e.Message }
            ), e);
            throw;
        }
    }

    private void AddCmd(CommandBase commandBase)
    {
        DataUtil.UpdateCommandDesc(commandBase);
        if (commandBase.Key == null)
        {
            modConfig.Error("Chatbot-Error.添加命令失败.缺少键".Translate(
                I18N,
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
            return "Chatbot-Error.未输入任何命令".Translate(I18N);
        }

        // logger.Info($"全部命令: {string.Join(", ", _commands.Keys.ToArray())}, 输入的指令: \"{command}\", 检测出的指令: {data[0]}");
        if (!Commands.TryGetValue(data[0], out CommandBase? iCmd))
        {
            return "Chatbot-Error.未知的命令".Translate(
                I18N,
                new { Command = data[0], AvailableCommands = string.Join(",", Commands.Keys.ToArray()) }
            );
            // return $"未知的命令: {data[0]}, 可用的命令包括: {string.Join(",", Commands.Keys.ToArray())}";
        }

        iCmd.Paras = new Parametric(sessionId, command, this);

        int index = 1;
        while (index >= 1 && index < data.Length)
        {
            if (index + 1 >= data.Length) break;
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
            modConfig.Error("Chatbot-Error.命令执行失败".Translate(
                I18N,
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
}