using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Models.Services;
using RaidRecord.Core.Services;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SuntionCore.Services.I18NUtil;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class ListCmd: CommandBase
{
    private readonly I18NMgr _i18NMgr;
    private readonly CmdUtil _cmdUtil;
    private readonly DataGetterService _dataGetter;
    private I18N I18N => _i18NMgr.I18N!;

    public ListCmd(CmdUtil cmdUtil, I18NMgr i18NMgr, DataGetterService dataGetter)
    {
        _cmdUtil = cmdUtil;
        _i18NMgr = i18NMgr;
        Key = "list";
        Desc = "z2serverMessage.Cmd-List.Desc".Translate(I18N);
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("limit", "int", "z3translations.Cmd-参数化简述.limit".Translate(I18N))
            .AddParam("page", "int", "z3translations.Cmd-参数化简述.page".Translate(I18N))
            .SetOptional(["limit", "page"])
            .Build();
        _dataGetter = dataGetter;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        int numberLimit = _cmdUtil.GetParameter(parametric.Paras, "Limit", 10);
        int page = _cmdUtil.GetParameter(parametric.Paras, "Page", -1);

        ArchivePageableResult results = _dataGetter.GetArchivesPageable(
            parametric.SessionId,
            page,
            numberLimit);

        int countAfterCheck = results.Archives.Count;

        string msg = "z2serverMessage.Cmd-List.历史战绩.统计表头".Translate(I18N, new
        {
            ResultCount = countAfterCheck,
            TotalCount = countAfterCheck + results.JumpData,
            PageCurr = results.Page,
            PageTotal = results.PageMax
        });
        // string msg = $"历史战绩(共{countAfterCheck}/{totalCount}条, 第{page}页/共{(int)Math.Ceiling((double)totalCount / numberLimit)}页):\n";

        int jump = results.JumpData;

        // 字段宽度数组（9列）
        int[] colWidths =
        [
            3, 7, 8, 10, 10, 10, 10, 6, 4, 4
        ];

        // 计算字符串宽度
        // 遍历所有数据行，更新每列最大宽度
        for (int k = 0; k < countAfterCheck; k++)
        {
            RaidArchive row = results.Archives[k].Archive;

            string result = "UnknownResult".Translate(I18N);
            RaidResultData? raidResultData = row.Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                result = $"z3translations.{raidResultData.Result.Value.ToString()}".Translate(I18N);
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand",
                    "z2serverMessage.Cmd-List.获取对局结果信息时出错".Translate(I18N));
            }

            string[] values =
            [
                k.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(row.ServerId),
                _cmdUtil.I18NMgr!.GetMapName(row.ServerId[..row.ServerId.IndexOf('.')].ToLower()),
                row.PreRaidValue.ToString(),
                row.EquipmentValue.ToString(),
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

        string header = "z2serverMessage.Cmd-List.历史战绩.表头".Translate(I18N).Replace("\n", "");
        string[] coreHeader = header.Split('|');

        int colCount = Math.Min(colWidths.Length, coreHeader.Length);

        if (colCount != colWidths.Length || colCount != coreHeader.Length)
        {
            _cmdUtil.ModConfig!.Warn(
                "z2serverMessage.Cmd-List.历史战绩.表头长度不一致".Translate(
                    I18N,
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

        // msg += " - serverId                 序号 地图 入场总价值 带出收益 战备 战损 游戏时间 本局击杀数 结果\n";

        // 显示文本
        for (int i = 0; i < countAfterCheck; i++)
        {
            RaidArchive archive = results.Archives[i].Archive;

            string result = "UnknownResult".Translate(I18N);
            RaidResultData? raidResultData = archive.Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                result = $"z3translations.{raidResultData.Result.Value.ToString()}".Translate(I18N);
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand",
                    "z2serverMessage.Cmd-List.获取对局结果信息时出错".Translate(I18N));
                // _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
            }

            string[] values =
            [
                results.Archives[i].Index.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(archive.ServerId),
                _cmdUtil.I18NMgr!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower()),
                archive.PreRaidValue.ToString(),
                archive.EquipmentValue.ToString(),
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

            msg += "z2serverMessage.Cmd-List.历史战绩.表行".Translate(I18N, new
            {
                Index = values[0],
                PlayerGroup = values[1],
                MapName = values[2],
                PreRaidValue = values[3],
                EquipmentValue = values[4],
                GrossProfit = values[5],
                CombatLosses = values[6],
                PlayTime = values[7],
                KillCount = values[8],
                Result = values[9]
            }).Replace("|", "");

            // msg += $" - {archive.ServerId} {indexLeft + i} "
            //        + $"{_cmdUtil.I18n!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower())} "
            //        + $"{archive.PreRaidValue} {archive.GrossProfit} {archive.CombatLosses} "
            //        + $"{StringUtil.TimeString(archive.Results?.PlayTime ?? 0)} {result}\n";
        }
        // if (jump > 0) msg += $"跳过{jump}条无效数据";
        if (jump > 0) msg += "z2serverMessage.Cmd-List.历史战绩.跳过无效数据".Translate(I18N, new { JumpCount = jump });
        return msg;
    }
}