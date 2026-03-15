namespace WarhammerArmyMaker.Models;

public sealed class ArmyUnit
{
    public int Id { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Notes { get; set; } = string.Empty;
    public string StatsJson { get; set; } = "{}";
    public string CategoriesJson { get; set; } = "[]";
}
