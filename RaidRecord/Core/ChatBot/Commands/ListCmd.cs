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
        int countBeforeCheck = results.Count;
        if (countBeforeCheck <= 0)
        {
            return _local.GetText("Cmd-List.没有找到指定页的记录",
                new
                {
                    StartIndex = indexLeft + 1,
                    EndIndex = indexRight + 1,
                    IndexRange = $"[0, {totalCount})"
                });
        }
        // if (countBeforeCheck <= 0) return $"未查询到您第{indexLeft + 1}到{indexRight}条历史战绩";
        
        results.RemoveAll(x => string.IsNullOrEmpty(x.ServerId));
        int countAfterCheck = results.Count;

        string msg = _local.GetText("Cmd-List.历史战绩.统计表头", new
        {
            ResultCount = countAfterCheck,
            TotalCount = totalCount,
            PageCurrent = page,
            PageTotal = (int)Math.Ceiling((double)totalCount / numberLimit)
        });
        // string msg = $"历史战绩(共{countAfterCheck}/{totalCount}条, 第{page}页/共{(int)Math.Ceiling((double)totalCount / numberLimit)}页):\n";
        
        int jump = countBeforeCheck - countAfterCheck;

        // 字段宽度数组（9列）
        int[] colWidths =
        [
            3, 7, 10, 10, 10, 10, 16, 4, 4
        ];
        
        // 计算字符串宽度
        // 遍历所有数据行，更新每列最大宽度
        for (int k = 0; k < countAfterCheck; k++)
        {
            RaidArchive row = results[k];

            string result = _local.GetText("UnknownResult");
            RaidResultData? raidResultData = row.Results;
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
            }
            
            string[] values =
            [
                k.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(row.ServerId),
                _cmdUtil.LocalizationManager!.GetMapName(row.ServerId[..row.ServerId.IndexOf('.')].ToLower()),
                row.PreRaidValue.ToString(),
                row.GrossProfit.ToString(),
                row.CombatLosses.ToString(),
                row.Results?.PlayTime.ToString() ?? "",
                (row.EftStats?.Victims?.Count() ?? 0).ToString(),
                result
            ];

            for (int i = 0; i < values.Length; i++)
            {
                // 默认使用" | "分隔
                colWidths[i] = Math.Max(colWidths[i], values[i].Length + 3);
            }
        }
        
        string header = _local.GetText("Cmd-List.历史战绩.表头").Replace("\n", "");
        string[] coreHeader = header.Split('|');

        int colCount = Math.Min(colWidths.Length, coreHeader.Length);

        if (colCount != colWidths.Length || colCount != coreHeader.Length)
        {
            _cmdUtil.ModConfig!.Warn(
                    _local.GetText(
                            "Cmd-List.历史战绩.表头长度不一致",
                            new
                            {
                                // 理论列数
                                TheoreticalColCount = colWidths.Length,
                                // 实际列数
                                ActualColCount = coreHeader.Length
                            }
                        )
                );
        }
        
        for (int i = 0; i < colCount; i++)
        {
            msg += coreHeader[i].PadRight(colWidths[i]);
        }
        
        msg += "\n";
        
        // msg += " - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 本局击杀数 结果\n";

        // 显示文本
        for (int i = 0; i < countAfterCheck; i++)
        {
            RaidArchive archive = results[i];

            string result = _local.GetText("UnknownResult");
            RaidResultData? raidResultData = archive.Results;
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

            string[] values =
            [
                i.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(archive.ServerId),
                _cmdUtil.LocalizationManager!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower()),
                archive.PreRaidValue.ToString(),
                archive.GrossProfit.ToString(),
                archive.CombatLosses.ToString(),
                archive.Results?.PlayTime.ToString() ?? "",
                (archive.EftStats?.Victims?.Count() ?? 0).ToString(),
                result
            ];

            for (int j = 0; j < values.Length; j++)
            {
                if (values[j].Length < colWidths[j])
                {
                    values[j] = values[j].PadRight(colWidths[j]);
                }
            }

            msg += _local.GetText("Cmd-List.历史战绩.表行", new
            {
                Index = values[0],
                PlayerGroup = values[1],
                MapName = values[2],
                PreRaidValue = values[3],
                GrossProfit = values[4],
                CombatLosses = values[5],
                PlayTime = values[6],
                KillCount = values[7],
                Result = values[8]
            }).Replace("|", "");

            // msg += $" - {archive.ServerId} {indexLeft + i} "
            //        + $"{_cmdUtil.LocalizationManager!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower())} "
            //        + $"{archive.PreRaidValue} {archive.GrossProfit} {archive.CombatLosses} "
            //        + $"{StringUtil.TimeString(archive.Results?.PlayTime ?? 0)} {result}\n";
        }
        // if (jump > 0) msg += $"跳过{jump}条无效数据";
        if (jump > 0) msg += _local.GetText("Cmd-List.历史战绩.跳过无效数据", new { JumpCount = jump });
        return msg;
    }
}