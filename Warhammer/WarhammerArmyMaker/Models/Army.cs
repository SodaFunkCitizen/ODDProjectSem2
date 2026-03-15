namespace WarhammerArmyMaker.Models;

public sealed class Army
{
    public int Id { get; set; }
    public string Name { get; set; } = "New Army";
    public string Faction { get; set; } = "Black Templars";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<ArmyUnit> Units { get; set; } = new();
}
