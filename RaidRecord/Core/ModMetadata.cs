using SPTarkov.Server.Core.Models.Spt.Mod;

namespace RaidRecord.Core;

public record ModMetadata: AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.raidrecord";
    public override string Name { get; init; } = "RaidRecord";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0.6.4");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://forge.sp-tarkov.com/mod/2341/raidrecord";
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "CC-BY-SA";
}