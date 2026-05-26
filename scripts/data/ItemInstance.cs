using System.Collections.Generic;

namespace GodotGame;

public sealed class ItemInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Durability { get; set; } = 100;
    public string Quality { get; set; } = "standard";
    public string BoundUnitInstanceId { get; set; } = string.Empty;
    public Dictionary<string, float> Modifiers { get; } = new();
    public List<string> Flags { get; } = new();
}
