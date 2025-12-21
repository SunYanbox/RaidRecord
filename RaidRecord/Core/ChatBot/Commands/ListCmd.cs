using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ListCmd: CommandBase
{
    private readonly CmdUtil _cmdUtil;
    private readonly LocalizationManager _local;

    public ListCmd(CmdUtil cmdUtil, LocalizationManager local)
    {
        _cmdUtil = cmdUtil;
        Key = "list";
        Desc = local.GetText("Cmd-List.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("limit", "int", local.GetText("Cmd-参数化简述.limit"))
            .AddParam("page", "int", local.GetText("Cmd-参数化简述.page"))
            .SetOptional(["limit", "page"])
            .Build();
        _local = local;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
        int numberLimit = _cmdUtil.GetParameter(parametric.Paras, "Limit", 10);
        int page = _cmdUtil.GetParameter(parametric.Paras, "Page", 1);
        numberLimit = Math.Min(20, Math.Max(1, numberLimit));
        page = Math.Max(1, page);

        int totalCount = records.Count;
        int indexLeft = Math.Max(numberLimit * (page - 1), 0);
        int indexRight = Math.Min(numberLimit * page, totalCount);
        if (totalCount <= 0) return _local.GetText("Cmd-List.没有任何历史战绩");
        // if (totalCount <= 0) return "您没有任何历史战绩, 请至少对局一次后再来查询吧";
        List<RaidArchive> results = [];
        for (int i = indexLeft; i < indexRight; i++)
        {
            results.Add(records[i]);
        }
        if (results.Count <= 0)
        {
            return _local.GetText("Cmd-List.没有找到指定页的记录",
                new
                {
                    StartIndex = indexLeft + 1,
                    EndIndex = indexRight + 1,
                    IndexRange = $"[0, {totalCount})"
                });
        }
        // if (results.Count <= 0) return $"未查询到您第{indexLeft + 1}到{indexRight}条历史战绩";

        string msg = _local.GetText("Cmd-List.历史战绩.统计表头", new
        {
            ResultCount = results.Count,
            TotalCount = totalCount,
            PageCurrent = page,
            PageTotal = (int)Math.Ceiling((double)totalCount / numberLimit)
        });
        // string msg = $"历史战绩(共{results.Count}/{totalCount}条, 第{page}页/共{(int)Math.Ceiling((double)totalCount / numberLimit)}页):\n";
        msg += _local.GetText("Cmd-List.历史战绩.表头");
        // msg += " - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 结果\n";

        int jump = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (string.IsNullOrEmpty(results[i].ServerId))
            {
                jump++;
                continue;
            }

            string result = _local.GetText("UnknownResult");
            RaidResultData? raidResultData = results[i].Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                result = _cmdUtil.LocalizationManager!.GetText(raidResultData.Result.Value.ToString());
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand",
                    _local.GetText("Cmd-List.获取对局结果信息时出错"));
                // _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
            }

            msg += _local.GetText("Cmd-List.历史战绩.表行", new
            {
                Index = indexLeft + i,
                PlayerGroup = _cmdUtil.GetPlayerGroupOfServerId(results[i].ServerId),
                results[i].ServerId,
                MapName = _cmdUtil.LocalizationManager!.GetMapName(results[i].ServerId[..results[i].ServerId.IndexOf('.')].ToLower()),
                results[i].PreRaidValue,
                results[i].GrossProfit,
                results[i].CombatLosses,
                results[i].Results?.PlayTime,
                Result = result
            });

            // msg += $" - {results[i].ServerId} {indexLeft + i} "
            //        + $"{_cmdUtil.LocalizationManager!.GetMapName(results[i].ServerId[..results[i].ServerId.IndexOf('.')].ToLower())} "
            //        + $"{results[i].PreRaidValue} {results[i].GrossProfit} {results[i].CombatLosses} "
            //        + $"{StringUtil.TimeString(results[i].Results?.PlayTime ?? 0)} {result}\n";
        }
        // if (jump > 0) msg += $"跳过{jump}条无效数据";
        if (jump > 0) msg += _local.GetText("Cmd-List.历史战绩.跳过无效数据", new { JumpCount = jump });
        return msg;
    }
}