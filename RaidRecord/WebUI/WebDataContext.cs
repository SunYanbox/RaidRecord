using RaidRecord.Core.Models.Services;
using SPTarkov.DI.Annotations;

namespace RaidRecord.WebUI;

/// <summary>
/// 管理模组的Web中所有数据的共享
/// </summary>
[Injectable(InjectionType.Singleton)]
public class WebDataContext
{
    /// <summary> 当前管理的账号 </summary>
    public string CurrAccount = string.Empty;

    /// <summary> 正在查看的对局信息ID(这里不用索引是为了避免索引变更) </summary>
    public readonly HashSet<ArchiveIndexed> LookingServerIds = [];
}