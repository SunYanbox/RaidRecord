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
        Desc = "获取自身所有符合条件的对局历史记录, 使用方式: \n";
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("limit", "int", "每一页历史记录数量限制")
            .AddParam("page", "int", "要查看的页码")
            .SetOptional(["limit", "page"])
            .Build();
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        List<RaidArchive> records = _cmdUtil.GetArchivesBySession(parametric.SessionId);
        int numberLimit = CmdUtil.GetParameter(parametric.Paras, "Limit", 10);
        int page = CmdUtil.GetParameter(parametric.Paras, "Page", 1);
        numberLimit = Math.Min(20, Math.Max(1, numberLimit));
        page = Math.Max(1, page);

        int totalCount = records.Count;
        int indexLeft = Math.Max(numberLimit * (page - 1), 0);
        int indexRight = Math.Min(numberLimit * page, totalCount);
        if (totalCount <= 0) return "您没有任何历史战绩, 请至少对局一次后再来查询吧";
        List<RaidArchive> results = [];
        for (int i = indexLeft; i < indexRight; i++)
        {
            results.Add(records[i]);
        }
        if (results.Count <= 0) return $"未查询到您第{indexLeft + 1}到{indexRight}条历史战绩";

        string msg = $"历史战绩(共{results.Count}/{totalCount}条, 第{page}页/共{(int)Math.Ceiling((double)totalCount / numberLimit)}页):\n";
        msg += " - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 结果\n";

        int jump = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (string.IsNullOrEmpty(results[i].ServerId))
            {
                jump++;
                continue;
            }

            string result = "未知结局";
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
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
            }

            msg += $" - {results[i].ServerId} {indexLeft + i} "
                   + $"{_cmdUtil.LocalizationManager!.GetMapName(results[i].ServerId[..results[i].ServerId.IndexOf('.')].ToLower())} "
                   + $"{results[i].PreRaidValue} {results[i].GrossProfit} {results[i].CombatLosses} "
                   + $"{StringUtil.TimeString(results[i].Results?.PlayTime ?? 0)} {result}\n";
        }
        if (jump > 0) msg += $"跳过{jump}条无效数据";
        return msg;
    }
}