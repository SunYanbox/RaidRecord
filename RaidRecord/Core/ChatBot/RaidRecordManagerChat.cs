using RaidRecord.Core.ChatBot.Commands;
using RaidRecord.Core.Configs;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Systems;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Json;

namespace RaidRecord.Core.ChatBot;

[Injectable(InjectionType.Singleton)]
public class RaidRecordManagerChat(
    MailSendService mailSendService,
    ISptLogger<RaidRecordManagerChat> logger,
    ProfileHelper profileHelper,
    DialogueHelper dialogueHelper,
    LocalizationManager localizationManager,
    DatabaseService databaseService,
    ItemHelper itemHelper,
    ModConfig modConfig,
    RecordCacheManager recordCacheManager): IDialogueChatBot, IOnLoad
{
    private readonly Dictionary<string, Command> _commands = new();
    protected readonly ParaInfoBuilder ParaInfoBuilder = new();

    private string GetLocalText(string msgId, params object?[] args)
    {
        return localizationManager.GetTextFormat(msgId, args);
    }

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
                Nickname = GetLocalText("RC MC.ChatBot.NickName"),
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
            logger.Error($"[RaidRecord]<Chat> {GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message)}");
            modConfig.LogError(e, "RaidRecordManagerChat.HandleMessage", GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message));
            SendMessage(sessionId, GetLocalText("RC MC.Chat.HM.error1", request.Text, e.Message));
        }
        return ValueTask.FromResult(request.DialogId);
    }


    // 注册命令

    protected void InitCommands()
    {
        Command[] commands =
        [
            new()
            {
                Key = "help",
                Desc = GetLocalText("Command.Help.Desc"),
                ParaInfo = new ParaInfo(),
                Paras = null,
                Callback = GetHelpCommand()
            },
            new()
            {
                Key = "list",
                Desc = GetLocalText("Command.List.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("limit", "int", GetLocalText("Command.Para.Limit.Desc"))
                    .AddParam("page", "int", GetLocalText("Command.Para.Page.Desc"))
                    .SetOptional(["limit", "page"])
                    .Build(),
                Paras = null,
                Callback = GetListCommand()
            },
            new()
            {
                Key = "info",
                Desc = GetLocalText("Command.Info.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("serverId", "string", GetLocalText("Command.Para.ServerId.Desc"))
                    .AddParam("index", "int", GetLocalText("Command.Para.Index.Desc"))
                    .SetOptional(["serverId", "index"])
                    .Build(),
                Paras = null,
                Callback = GetInfoCommand()
            },
            new()
            {
                Key = "items",
                Desc = GetLocalText("Command.Items.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("serverId", "string", GetLocalText("Command.Para.ServerId.Desc"))
                    .AddParam("index", "int", GetLocalText("Command.Para.Index.Desc"))
                    .SetOptional(["serverId", "index"])
                    .Build(),
                Paras = null,
                Callback = GetItemsCommand()
            },
            new()
            {
                Key = "cls",
                Desc = GetLocalText("Command.Cls.Desc"),
                ParaInfo = new ParaInfo(),
                Paras = null,
                Callback = GetClsCommand()
            }
        ];
        foreach (Command command in commands)
        {
            DataUtil.UpdateCommandDesc(command);
            if (command.Key == null) continue;
            _commands[command.Key] = command;
        }
        logger.Info($"[RaidRecord] {GetLocalText("RC MC.Chat.initCmd.info0", string.Join(", ", _commands.Keys.ToArray()))}");
    }

    private string HandleCommand(string command, string sessionId)
    {
        string[] data = StringUtil.SplitCommand(command.ToLower());
        if (data.Length <= 0)
        {
            return GetLocalText("RC MC.Chat.handleCmd.error0");
        }

        // logger.Info($"全部命令: {string.Join(", ", _commands.Keys.ToArray())}, 输入的指令: \"{command}\", 检测出的指令: {data[0]}");
        if (!_commands.ContainsKey(data[0]))
        {
            return GetLocalText("RC MC.Chat.handleCmd.error1", data[0], string.Join(",", _commands.Keys.ToArray()));
        }
        Command iCmd = _commands[data[0]];
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
        if (iCmd.Callback == null)
        {
            string error = $"Command \"{iCmd.Key}\"Callback为null; 这是服务端错误，请反馈给开发者";
            modConfig.Log("Error", error);
            return error;
        }
        string result = iCmd.Callback(iCmd.Paras);
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

    // Command 的工具

    protected string? VerifyIParametric(Parametric parametric)
    {
        if (string.IsNullOrEmpty(parametric.SessionId))
        {
            return GetLocalText("RC MC.Chat.verify.error0");
        }

        try
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(parametric.SessionId!);
            if (pmcData?.Id == null)
            {
                throw new NullReferenceException($"{nameof(pmcData)} or {nameof(pmcData.Id)}");
            }
            string playerId = pmcData.Id;
            if (string.IsNullOrEmpty(playerId)) throw new Exception(GetLocalText("RC MC.Chat.verify.error1"));
        }
        catch (Exception e)
        {
            modConfig.LogError(e, "RaidRecordManagerChat.VerifyIParametric", GetLocalText("RC MC.Chat.verify.error2", e.Message));
            return GetLocalText("RC MC.Chat.verify.error2", e.Message);
        }

        return parametric.ManagerChat == null ? GetLocalText("RC MC.Chat.verify.error3") : null;

    }

    protected MongoId? GetAccountBySession(string sessionId)
    {
        return recordCacheManager.GetAccount(profileHelper.GetPmcProfile(sessionId)?.Id ?? new MongoId());
    }

    protected List<RaidArchive> GetArchivesBySession(string sessionId)
    {
        List<RaidArchive> result = [];
        MongoId? account = GetAccountBySession(sessionId);
        if (account == null) return result;
        EFTCombatRecord records = recordCacheManager.GetRecord(account.Value);
        foreach (RaidDataWrapper record in records.Records)
        {
            if (record.IsArchive)
            {
                result.Add(record.Archive!);
            }
        }
        return result;
    }

    protected string DateFormatterFull(long timestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        int year = date.Year;
        int month = date.Month;
        int day = date.Day;
        string time = date.ToShortTimeString();
        return GetLocalText("RC MC.Chat.Format.Time", year, month, day, time);
    }

    protected string GetArchiveDetails(RaidArchive archive)
    {
        string msg = "";
        string serverId = archive.ServerId;
        string playerId = archive.PlayerId;

        PmcData playerData = recordCacheManager.GetPmcDataByPlayerId(playerId);
        // 本次对局元数据
        string timeString = DateFormatterFull(archive.CreateTime);
        string mapName = serverId[..serverId.IndexOf('.')].ToLower();

        msg += GetLocalText("RC MC.Chat.GAD.info0",
            timeString, serverId, playerData?.Info?.Nickname,
            playerData?.Info?.Level, playerData?.Id);

        msg += GetLocalText("RC MC.Chat.GAD.info1",
            localizationManager.GetMapName(mapName), StringUtil.TimeString(archive.Results?.PlayTime ?? 0));

        msg += GetLocalText("RC MC.Chat.GAD.info2",
            (int)archive.EquipmentValue, (int)archive.SecuredValue, (int)archive.PreRaidValue);

        msg += GetLocalText("RC MC.Chat.GAD.info3",
            (int)archive.GrossProfit,
            (int)archive.CombatLosses,
            (int)(archive.GrossProfit - archive.CombatLosses));

        string result = GetLocalText("RC MC.Chat.GAD.unknowResult");

        if (archive.Results?.Result != null)
        {
            ExitStatus nonNullResult = archive.Results.Result.Value;
            if (Constants.ResultNames.TryGetValue(nonNullResult, out string? resultName))
            {
                result = localizationManager.GetText(resultName, resultName);
            }
        }

        msg += GetLocalText("RC MC.Chat.GAD.info4",
            result,
            localizationManager.GetExitName(mapName, archive.Results?.ExitName ?? GetLocalText("RC MC.Chat.GAD.nullExitPosition")),
            archive.EftStats?.SurvivorClass ?? GetLocalText("RC MC.Chat.GAD.unknow"));

        List<Victim> victims = archive.EftStats?.Victims?.ToList() ?? [];
        LazyLoad<Dictionary<string, string>> localTemps = databaseService.GetTables().Locales.Global[localizationManager.CurrentLanguage];
        Dictionary<string, string>? locals = localTemps.Value;

        if (victims.Count > 0)
        {
            msg += GetLocalText("RC MC.Chat.GAD.killed");
            foreach (Victim victim in victims)
            {
                string weapon;
                if (locals != null && victim.Weapon != null)
                {
                    weapon = locals.TryGetValue(victim.Weapon, out string? value1)
                        ? value1
                        : victim.Weapon ?? GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }
                else
                {
                    weapon = victim.Weapon ?? GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }


                msg += GetLocalText("RC MC.Chat.GAD.info5",
                    victim.Time,
                    weapon,
                    localizationManager.GetArmorZoneName(victim.BodyPart ?? ""),
                    (int)(victim.Distance ?? 0),
                    victim.Name,
                    victim.Level,
                    victim.Side,
                    localizationManager.GetRoleName(victim.Role ?? ""));
                // Constants.RoleNames.TryGetValue(victim.Role,  out var value3) ? value3 : victim.Role);
            }
        }

        if (archive.Results?.Result != ExitStatus.KILLED) return msg;
        {
            Aggressor? aggressor = archive.EftStats?.Aggressor;
            if (aggressor != null)
            {
                string weapon;
                if (locals != null && aggressor.WeaponName != null)
                {
                    weapon = locals.TryGetValue(aggressor.WeaponName, out string? value1)
                        ? value1
                        : aggressor.WeaponName ?? GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }
                else
                {
                    weapon = aggressor.WeaponName ?? GetLocalText("RC MC.Chat.GAD.unknowWeapon");
                }

                msg += GetLocalText("RC MC.Chat.GAD.info6",
                    aggressor.Name,
                    aggressor.Side,
                    weapon,
                    // Constants.RoleNames.TryGetValue(aggressor.Role,  out var value3) ? value3 : aggressor.Role);
                    localizationManager.GetRoleName(aggressor.Role ?? ""));
            }
            else
            {
                msg += GetLocalText("RC MC.Chat.GAD.killedLoadError");
            }
        }

        return msg;
    }

    protected string GetItemsDetails(RaidArchive archive)
    {
        string msg = "";
        string serverId = archive.ServerId;
        string playerId = archive.PlayerId;

        PmcData playerData = recordCacheManager.GetPmcDataByPlayerId(playerId);

        // 本次对局元数据
        string timeString = DateFormatterFull(archive.CreateTime);
        string mapName = serverId[..serverId.IndexOf('.')].ToLower();

        msg += GetLocalText("RC MC.Chat.GAD.info0",
            timeString, serverId, playerData?.Info?.Nickname, playerData?.Info?.Level,
            playerData?.Id);

        msg += GetLocalText("RC MC.Chat.GAD.info1",
            localizationManager.GetMapName(mapName), StringUtil.TimeString(archive.Results?.PlayTime ?? 0));

        msg += GetLocalText("RC MC.Chat.GAD.info2",
            (int)archive.EquipmentValue, (int)archive.SecuredValue, (int)archive.PreRaidValue);

        msg += GetLocalText("RC MC.Chat.GAD.info3",
            (int)archive.GrossProfit,
            (int)archive.CombatLosses,
            (int)(archive.GrossProfit - archive.CombatLosses));

        string result = GetLocalText("RC MC.Chat.GAD.unknow");

        if (archive.Results?.Result != null)
        {
            ExitStatus nonNullResult = archive.Results.Result.Value;
            if (Constants.ResultNames.TryGetValue(nonNullResult, out string? resultName))
            {
                result = localizationManager.GetText(resultName, resultName);
            }
        }

        msg += GetLocalText("RC MC.Chat.GAD.info4",
            result,
            localizationManager.GetExitName(mapName, archive.Results?.ExitName ?? GetLocalText("RC MC.Chat.GAD.nullExitPosition")),
            archive.EftStats?.SurvivorClass ?? GetLocalText("RC MC.Chat.GAD.unknow"));

        // Dictionary<MongoId, TemplateItem> itemTpls = databaseService.GetTables().Templates.Items;
        Dictionary<string, string>? local = databaseService.GetTables().Locales.Global[localizationManager.CurrentLanguage].Value;
        if (local == null) return GetLocalText("RC MC.Chat.GID.error0");

        if (archive is { ItemsTakeIn.Count: > 0 })
        {
            // "\n\n带入对局物品:\n   物品名称  物品单价  物品修正  物品总价值"
            msg += GetLocalText("RC MC.Chat.GID.info0");
            foreach ((MongoId tpl, double modify) in archive.ItemsTakeIn)
            {
                // TemplateItem item = itemTpls[tpl];
                double price = itemHelper.GetItemPrice(tpl) ?? 0;
                msg += $"\n\n - {local[$"{tpl} ShortName"]}  {price}  {modify}  {price * modify} {local[$"{tpl} Description"]}";
            }
        }

        if (archive is { ItemsTakeOut.Count: <= 0 }) return msg;
        {
            // "\n\n带出对局物品:\n   物品名称  物品单价  物品修正  物品总价值  物品描述"
            msg += GetLocalText("RC MC.Chat.GID.info1");

            foreach ((MongoId tpl, double modify) in archive.ItemsTakeOut)
            {
                // TemplateItem item = itemTpls[tpl];
                double price = itemHelper.GetItemPrice(tpl) ?? 0;
                msg += $"\n\n - {local[$"{tpl} ShortName"]}  {price}  {modify}  {price * modify}  {local[$"{tpl} Description"]}";
            }
        }

        return msg;
    }

    #region CommandCallbacks
    public CommandCallback GetHelpCommand()
    {
        return parametric =>
        {
            string? verify = VerifyIParametric(parametric);
            if (verify != null) return verify;

            // "帮助信息(参数需要按键值对写, 例如\"list index 1\"; 中括号表示可选参数; 指令与参数不区分大小写):"
            string msg = GetLocalText("Command.Help.Head");
            if (parametric.ManagerChat == null)
            {
                modConfig.LogError(
                    new NullReferenceException(nameof(parametric.ManagerChat)),
                    "RaidRecordManagerChat.GetHelpCommand",
                    msg);
                return msg;
            }
            foreach (Command cmd in parametric.ManagerChat._commands.Values)
            {
                msg += $"\n - {cmd.Key}: {cmd.Desc}\n";
            }
            return msg;
        };
    }

    private CommandCallback GetClsCommand()
    {
        return parametric =>
        {
            string? verify = VerifyIParametric(parametric);
            if (verify != null) return verify;

            UserDialogInfo managerProfile = GetChatBot();

            Dictionary<MongoId, Dialogue> dialogs = dialogueHelper.GetDialogsForProfile(parametric.SessionId);
            Dialogue dialog = dialogs[managerProfile.Id];
            if (dialog.Messages == null) return GetLocalText("Command.Cls.error0");
            int count = dialog.Messages.Count;
            dialog.Messages = [];
            // $"已清除{count}条聊天记录, 重启游戏客户端后生效"
            return GetLocalText("Command.Cls.info0", count);
            // "找不到你的聊天记录"
        };
    }

    private CommandCallback GetListCommand()
    {
        return parametric =>
        {
            string? verify = VerifyIParametric(parametric);
            if (verify != null) return verify;

            List<RaidArchive> records = GetArchivesBySession(parametric.SessionId);
            int numberLimit, page;
            try
            {
                numberLimit = int.TryParse(parametric.Paras.GetValueOrDefault("limit", "10"), out int limitTemp) ? limitTemp : 10;
                page = int.TryParse(parametric.Paras.GetValueOrDefault("page", "1"), out int pageTemp) ? pageTemp : 1;
            }
            catch (Exception e)
            {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }
            numberLimit = Math.Min(20, Math.Max(1, numberLimit));
            page = Math.Max(1, page);

            int indexLeft = Math.Max(numberLimit * (page - 1), 0);
            int indexRight = Math.Min(numberLimit * page, records.Count);
            // if (records.Count <= 0) return "您没有任何历史战绩, 请至少对局一次后再来查询吧";
            if (records.Count <= 0) return GetLocalText("Command.List.error0");
            List<RaidArchive> results = [];
            for (int i = indexLeft; i < indexRight; i++)
            {
                results.Add(records[i]);
            }
            // if (results.Count <= 0) return $"未查询到您第{indexLeft+1}到{indexRight}条历史战绩";
            if (results.Count <= 0) return GetLocalText("Command.List.error1", indexLeft + 1, indexRight);

            // string msg = $"历史战绩(共{results.Count}/{records.Count}条):\n - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 结果\n";
            string msg = GetLocalText("Command.List.info0", results.Count, records.Count);

            int jump = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (string.IsNullOrEmpty(results[i].ServerId))
                {
                    jump++;
                    continue;
                }

                string result = GetLocalText("Command.List.unknownEnding");
                RaidResultData? raidResultData = results[i].Results;
                try
                {
                    if (raidResultData?.Result == null)
                    {
                        throw new NullReferenceException(nameof(raidResultData.Result));
                    }
                    string resultName = Constants.ResultNames[raidResultData.Result.Value];
                    result = localizationManager.GetText(resultName, resultName);
                }
                catch (Exception e)
                {
                    modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
                }

                msg += $" - {results[i].ServerId} {indexLeft + i} "
                       + $"{localizationManager.GetMapName(results[i].ServerId[..results[i].ServerId.IndexOf('.')].ToLower())} "
                       + $"{results[i].PreRaidValue} {results[i].GrossProfit} {results[i].CombatLosses} "
                       + $"{StringUtil.TimeString(results[i].Results?.PlayTime ?? 0)} {result}\n";
            }
            // if (jump > 0) msg += $"跳过{jump}条无效数据";
            if (jump > 0) msg += GetLocalText("Command.List.info1", jump);
            return msg;
        };
    }

    public CommandCallback GetInfoCommand()
    {
        return parametric =>
        {
            string? verify = VerifyIParametric(parametric);
            if (verify != null) return verify;

            string serverId;
            int index;
            try
            {
                serverId = parametric.Paras.GetValueOrDefault("serverid", "");
                index = int.TryParse(parametric.Paras.GetValueOrDefault("index", "-1"), out int indexTemp) ? indexTemp : -1;
            }
            catch (Exception e)
            {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }

            if (!string.IsNullOrEmpty(serverId))
            {
                List<RaidArchive> records = GetArchivesBySession(parametric.SessionId);
                RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
                if (record != null)
                {
                    return GetArchiveDetails(record);
                }
                else
                {
                    // return $"serverId为{serverId}的对局不存在, 请检查你的输入";
                    return GetLocalText("Command.Para.ServerId.NotExist", serverId);
                }
            }
            if (index >= 0)
            {
                List<RaidArchive> records = GetArchivesBySession(parametric.SessionId);
                // if (index >= records.Count) return $"索引{index}超出范围: [0, {records.Count})";
                if (index >= records.Count) return GetLocalText("Command.Para.Index.OutOfRange", index, records.Count);
                return GetArchiveDetails(records[index]);
            }
            List<RaidArchive> records2 = GetArchivesBySession(parametric.SessionId);
            // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
            return GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
        };
    }

    public CommandCallback GetItemsCommand()
    {
        return parametric =>
        {
            string? verify = VerifyIParametric(parametric);
            if (verify != null) return verify;

            string serverId;
            int index;
            try
            {
                serverId = parametric.Paras.GetValueOrDefault("serverid", "");
                index = int.TryParse(parametric.Paras.GetValueOrDefault("index", "-1"), out int indexTemp) ? indexTemp : -1;
            }
            catch (Exception e)
            {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }

            // TODO: 显示新获得/遗失/更改的物品

            if (!string.IsNullOrEmpty(serverId))
            {
                List<RaidArchive> records = GetArchivesBySession(parametric.SessionId);
                RaidArchive? record = records.Find(x => x.ServerId.ToString() == serverId);
                if (record != null)
                {
                    return GetItemsDetails(record);
                }
                else
                {
                    // return $"serverId为{serverId}的对局不存在, 请检查你的输入";
                    return GetLocalText("Command.Para.ServerId.NotExist", serverId);
                }
            }
            if (index >= 0)
            {
                List<RaidArchive> records = GetArchivesBySession(parametric.SessionId);
                // if (index >= records.Count) return $"索引{index}超出范围: [0, {records.Count})";
                if (index >= records.Count) return GetLocalText("Command.Para.Index.OutOfRange", index, records.Count);
                return GetItemsDetails(records[index]);
            }

            List<RaidArchive> records2 = GetArchivesBySession(parametric.SessionId);
            // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
            return GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
        };
    }
    #endregion
}