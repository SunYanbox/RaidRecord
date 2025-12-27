using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
#pragma warning disable CS8764 // 返回类型的为 Null 性与重写成员不匹配(可能是由于为 Null 性特性)。

namespace RaidRecord.Core;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public record ModMetadata: AbstractModMetadata, IModWebMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.raidrecord";
    public override string Name { get; init; } = "RaidRecord";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0.6.7");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://forge.sp-tarkov.com/mod/2341/raidrecord";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "CC-BY-SA";
}