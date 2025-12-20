using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ListCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;

    public ListCmd(CmdUtil cmdUtil)
    {
        _cmdUtil = cmdUtil;
        Key = "list";
        Desc = cmdUtil.GetLocalText("Command.List.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("limit", "int", cmdUtil.GetLocalText("Command.Para.Limit.Desc"))
            .AddParam("page", "int", cmdUtil.GetLocalText("Command.Para.Page.Desc"))
            .SetOptional(["limit", "page"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
        int numberLimit, page;
        try
        {
            numberLimit = int.TryParse(parametric.Paras.GetValueOrDefault("limit", "10"), out int limitTemp) ? limitTemp : 10;
            page = int.TryParse(parametric.Paras.GetValueOrDefault("page", "1"), out int pageTemp) ? pageTemp : 1;
        }
        catch (Exception e)
        {
            // return $"参数解析时出现错误: {e.Message}";
            _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", _cmdUtil.GetLocalText("Command.Para.Parse.error0", e.Message));
            return _cmdUtil.GetLocalText("Command.Para.Parse.error0", e.Message);
        }
        numberLimit = Math.Min(20, Math.Max(1, numberLimit));
        page = Math.Max(1, page);

        int indexLeft = Math.Max(numberLimit * (page - 1), 0);
        int indexRight = Math.Min(numberLimit * page, records.Count);
        // if (records.Count <= 0) return "您没有任何历史战绩, 请至少对局一次后再来查询吧";
        if (records.Count <= 0) return _cmdUtil.GetLocalText("Command.List.error0");
        List<RaidArchive> results = [];
        for (int i = indexLeft; i < indexRight; i++)
        {
            results.Add(records[i]);
        }
        // if (results.Count <= 0) return $"未查询到您第{indexLeft+1}到{indexRight}条历史战绩";
        if (results.Count <= 0) return _cmdUtil.GetLocalText("Command.List.error1", indexLeft + 1, indexRight);

        // string msg = $"历史战绩(共{results.Count}/{records.Count}条):\n - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 结果\n";
        string msg = _cmdUtil.GetLocalText("Command.List.info0", results.Count, records.Count);

        int jump = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (string.IsNullOrEmpty(results[i].ServerId))
            {
                jump++;
                continue;
            }

            string result = _cmdUtil.GetLocalText("Command.List.unknownEnding");
            RaidResultData? raidResultData = results[i].Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                string resultName = Constants.ResultNames[raidResultData.Result.Value];
                result = _cmdUtil.LocalizationManager!.GetText(resultName, resultName);
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
            }

            msg += $" - {results[i].ServerId} {indexLeft + i} "
                   + $"{_cmdUtil.LocalizationManager!.GetMapName(results[i].ServerId[..results[i].ServerId.IndexOf('.')].ToLower())} "
                   + $"{results[i].PreRaidValue} {results[i].GrossProfit} {results[i].CombatLosses} "
                   + $"{StringUtil.TimeString(results[i].Results?.PlayTime ?? 0)} {result}\n";
        }
        // if (jump > 0) msg += $"跳过{jump}条无效数据";
        if (jump > 0) msg += _cmdUtil.GetLocalText("Command.List.info1", jump);
        return msg;
    }
}