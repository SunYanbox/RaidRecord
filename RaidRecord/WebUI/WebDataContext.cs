using SPTarkov.DI.Annotations;

namespace RaidRecord.WebUI;

/// <summary>
/// 管理Web中所有数据的共享
/// </summary>
[Injectable(InjectionType.Singleton)]
public class WebDataContext
{
    public string CurrentAccount = string.Empty;
}