using RaidRecord.Core.ChatBot.Models;
using RaidRecord.Core.Locals;
using RaidRecord.Core.Models;
using RaidRecord.Core.Services;
using RaidRecord.Core.Systems;
using RaidRecord.Core.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;

namespace RaidRecord.Core.ChatBot.Commands;

[Injectable]
public class BuyCmd: CommandBase
{
    private readonly I18N _i18N;
    private readonly CmdUtil _cmdUtil;
    private readonly ItemUtil _itemUtil;
    private readonly ItemHelper _itemHelper;
    private readonly PriceSystem _priceSystem;
    private readonly ProfileHelper _profileHelper;
    private readonly DataGetterService _dataGetter;
    private readonly ModMailService _modMailService;

    public BuyCmd(CmdUtil cmdUtil,
        I18N i18N,
        ItemUtil itemUtil,
        ItemHelper itemHelper,
        PriceSystem priceSystem,
        ProfileHelper profileHelper,
        DataGetterService dataGetter,
        ModMailService modMailService)
    {
        _cmdUtil = cmdUtil;
        Key = "buy";
        Desc = i18N.GetText("Cmd-Buy.Desc");
        ParaInfo = cmdUtil.ParaInfoBuilder
            .AddParam("index", "int", i18N.GetText("Cmd-参数简述.index"))
            .AddParam("limit", "int", i18N.GetText("Cmd-参数化简述.limit"))
            .AddParam("page", "int", i18N.GetText("Cmd-参数化简述.page"))
            .AddParam("list", "int", i18N.GetText("Cmd-参数化简述.list"))
            .AddParam("preview", "int", i18N.GetText("Cmd-参数化简述.preview"))
            .SetOptional(["index", "limit", "page", "list", "preview"])
            .Build();
        _i18N = i18N;
        _dataGetter = dataGetter;
        _itemUtil = itemUtil;
        _profileHelper = profileHelper;
        _itemHelper = itemHelper;
        _priceSystem = priceSystem;
        _modMailService = modMailService;
    }

    public override string Execute(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        if (parametric.Command.Contains("ls")
            || parametric.Command.Contains("list"))
        {
            return ExecuteBuyList(parametric);
        }

        int index = _cmdUtil.GetParameter(parametric.Paras, "index", -1);

        List<RaidArchive> records = _dataGetter.GetArchivesBySession(parametric.SessionId);

        index = Math.Max(Math.Min(index < 0 ? records.Count + index : index, records.Count - 1), 0);

        RaidArchive archive = records[index];

        Item[] equipments = archive.EquipmentItems
            ?.Where(x => _itemHelper.IsValidItem(x.Template)).ToArray() ?? [];

        long totalPrice = _itemUtil.GetItemsValueAll(equipments);

        PmcData? pmcData = _profileHelper.GetPmcProfile(parametric.SessionId);

        if (equipments.Length == 0 || totalPrice <= 0)
            return _i18N.GetText("Cmd-Buy.Error.没有获取到已记录的有效装备数据");
        if (pmcData == null) return _i18N.GetText("Cmd-Buy.Error.无法获取到Pmc存档信息");

        if (parametric.Command.Contains("preview") || parametric.Command.Contains("pv"))
        {
            string msg = $"Index: {index}, ServerId: {archive.ServerId}\n";
            foreach (Item item in equipments)
            {
                msg += $"{item.Template} {_i18N.GetItemName(item.Template)} {_itemHelper.GetItemQualityModifier(item)}x{_priceSystem.GetItemValueWithCache(item.Template)} rub\n";
            }
            return msg;
        }

        List<Warning>? warnings = _modMailService.Payment(parametric.SessionId, totalPrice, pmcData);

        if (warnings?.Count > 0)
        {
            return string.Join("\n", warnings.Select(x => x.ErrorMessage));
        }

        string successMsg = _i18N.GetText("Cmd-Buy.Success.您已购买装备", new
        {
            Index = index,
            archive.ServerId,
            EquipmentCount = equipments.Length,
            TotalPrice = totalPrice
        });

        List<Warning>? warnings2 = _modMailService.SendItemsToPlayer(
            parametric.SessionId,
            successMsg,
            equipments.ToList());

        return warnings2?.Count > 0 ? string.Join("\n", warnings2.Select(x => x.ErrorMessage)) : successMsg;

    }

    public string ExecuteBuyList(Parametric parametric)
    {
        string? verify = _cmdUtil.VerifyIParametric(parametric);
        if (verify != null) return verify;

        List<RaidArchive> records = _dataGetter.GetArchivesBySession(parametric.SessionId);
        int numberLimit = _cmdUtil.GetParameter(parametric.Paras, "Limit", 10);
        int page = _cmdUtil.GetParameter(parametric.Paras, "Page", -1);
        numberLimit = Math.Min(50, Math.Max(1, numberLimit));

        List<(int index, RaidArchive archive, long price)> recordsCanQuickBuy
            = records
                .Select((archive, index) => (index, archive,
                    archive.EquipmentItems is not null
                        ? _itemUtil.GetItemsValueAll(
                            archive.EquipmentItems.Where(x => _itemHelper.IsValidItem(x.Template)).ToArray())
                        : 0))
                .Where(x => x.archive.EquipmentItems is { Length: > 0 })
                .Cast<(int index, RaidArchive archive, long price)>().ToList();

        int totalCount = recordsCanQuickBuy.Count;
        int pageTotal = (int)Math.Ceiling((double)totalCount / numberLimit);

        page = Math.Min(Math.Max(1, page > 0 ? page : pageTotal + page + 1), pageTotal);

        int indexLeft = Math.Max(numberLimit * (page - 1), 0);
        int indexRight = Math.Min(numberLimit * page, totalCount);
        if (totalCount <= 0) return _i18N.GetText("Cmd-Buy.BuyList.没有任何快购列表");
        // if (totalCount <= 0) return "您没有任何快购列表, 请至少对局一次后再来查询吧";
        List<(int index, RaidArchive archive, long price)> results = [];
        for (int i = indexLeft; i < indexRight; i++)
        {
            results.Add(recordsCanQuickBuy[i]);
        }
        int countBeforeCheck = results.Count;
        if (countBeforeCheck <= 0)
        {
            return _i18N.GetText("Cmd-Buy.BuyList.没有找到指定页的记录",
                new
                {
                    StartIndex = indexLeft + 1,
                    EndIndex = indexRight + 1,
                    IndexRange = $"[0, {totalCount})"
                });
        }

        results.RemoveAll(x => string.IsNullOrEmpty(x.archive.ServerId));
        int countAfterCheck = results.Count;

        string msg = _i18N.GetText("Cmd-Buy.BuyList.快购列表.统计表头", new
        {
            ResultCount = countAfterCheck,
            TotalCount = totalCount,
            PageCurr = page,
            PageTotal = pageTotal
        });
        int jump = countBeforeCheck - countAfterCheck;

        // 字段宽度数组（9列）
        int[] colWidths =
        [
            3, 7, 8, 10, 10, 10, 10, 6, 4, 4, 8
        ];

        // 计算字符串宽度
        // 遍历所有数据行，更新每列最大宽度
        for (int k = 0; k < countAfterCheck; k++)
        {
            RaidArchive row = results[k].archive;

            string result = _i18N.GetText("UnknownResult");
            RaidResultData? raidResultData = row.Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                result = _cmdUtil.I18N!.GetText(raidResultData.Result.Value.ToString());
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.BuyListCommand",
                    _i18N.GetText("Cmd-Buy.BuyList.获取对局结果信息时出错"));
            }

            string[] values =
            [
                results[k].index.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(row.ServerId),
                _cmdUtil.I18N!.GetMapName(row.ServerId[..row.ServerId.IndexOf('.')].ToLower()),
                row.GrossProfit.ToString(),
                row.CombatLosses.ToString(),
                row.EquipmentValue.ToString(),
                row.EquipmentItems?.Length.ToString() ?? "",
                row.Results?.PlayTime.ToString() ?? "",
                (row.EftStats?.Victims?.Count() ?? 0).ToString(),
                result,
                results[k].price.ToString()
            ];

            for (int i = 0; i < values.Length; i++)
            {
                // 默认使用" | "分隔
                colWidths[i] = Math.Max(colWidths[i], values[i].Length + 3);
            }
        }

        string header = _i18N.GetText("Cmd-Buy.BuyList.快购列表.表头").Replace("\n", "");
        string[] coreHeader = header.Split('|');

        int colCount = Math.Min(colWidths.Length, coreHeader.Length);

        if (colCount != colWidths.Length || colCount != coreHeader.Length)
        {
            _cmdUtil.ModConfig!.Warn(
                _i18N.GetText(
                    "Cmd-Buy.BuyList.快购列表.表头长度不一致",
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

        // 显示文本
        for (int i = 0; i < countAfterCheck; i++)
        {
            RaidArchive archive = results[i].archive;

            string result = _i18N.GetText("UnknownResult");
            RaidResultData? raidResultData = archive.Results;
            try
            {
                if (raidResultData?.Result == null)
                {
                    throw new NullReferenceException(nameof(raidResultData.Result));
                }
                result = _cmdUtil.I18N!.GetText(raidResultData.Result.Value.ToString());
            }
            catch (Exception e)
            {
                _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.BuyListCommand",
                    _i18N.GetText("Cmd-Buy.BuyList.获取对局结果信息时出错"));
                // _cmdUtil.ModConfig!.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
            }

            string[] values =
            [
                results[i].index.ToString(),
                CmdUtil.GetPlayerGroupOfServerId(archive.ServerId),
                _cmdUtil.I18N!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower()),
                archive.GrossProfit.ToString(),
                archive.CombatLosses.ToString(),
                archive.EquipmentValue.ToString(),
                archive.EquipmentItems?.Length.ToString() ?? "",
                archive.Results?.PlayTime.ToString() ?? "",
                (archive.EftStats?.Victims?.Count() ?? 0).ToString(),
                result,
                results[i].price.ToString()
            ];

            for (int j = 0; j < values.Length; j++)
            {
                if (values[j].Length < colWidths[j])
                {
                    values[j] = values[j].PadRight(colWidths[j]);
                }
            }

            // "Cmd-Buy.BuyList.快购列表.表行": " - {{Index}} | {{PlayerGroup}} | {{MapName}} | {{GrossProfit}} | {{CombatLosses}} | {{EquipmentValue}} | {{EquipmentCount}} | {{PlayTime}} | {{KillCount}} | {{Result}} | {{QuickBuyPrice}}\n",
            msg += _i18N.GetText("Cmd-Buy.BuyList.快购列表.表行", new
            {
                Index = values[0],
                PlayerGroup = values[1],
                MapName = values[2],
                GrossProfit = values[3],
                CombatLosses = values[4],
                EquipmentValue = values[5],
                EquipmentCount = values[6],
                PlayTime = values[7],
                KillCount = values[8],
                Result = values[9],
                QuickBuyPrice = values[10]
            }).Replace("|", "");

            // msg += $" - {archive.ServerId} {indexLeft + i} "
            //        + $"{_cmdUtil.I18n!.GetMapName(archive.ServerId[..archive.ServerId.IndexOf('.')].ToLower())} "
            //        + $"{archive.PreRaidValue} {archive.GrossProfit} {archive.CombatLosses} "
            //        + $"{StringUtil.TimeString(archive.Results?.PlayTime ?? 0)} {result}\n";
        }
        // if (jump > 0) msg += $"跳过{jump}条无效数据";
        if (jump > 0) msg += _i18N.GetText("Cmd-Buy.BuyList.快购列表.跳过无效数据", new { JumpCount = jump });
        return msg;
    }
}