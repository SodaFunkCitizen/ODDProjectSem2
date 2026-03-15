namespace WarhammerArmyMaker.Models;

public sealed class ImportedUnit
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string PrimaryCategory { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, string> Stats { get; set; } = new();
    public List<string> Abilities { get; set; } = new();
    public string RawProfilesJson { get; set; } = string.Empty;

    public string CategoriesDisplay => string.Join(", ", Categories);
    public string StatsDisplay => string.Join(" | ", Stats.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    public string AbilitiesDisplay => string.Join(Environment.NewLine + Environment.NewLine, Abilities);
}
