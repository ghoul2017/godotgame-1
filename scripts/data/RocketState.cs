using System.Collections.Generic;

namespace GodotGame;

public sealed class RocketState
{
    public bool IsConstructed { get; set; }
    public float ConstructionProgress { get; set; }
    public string CargoInventoryId { get; set; } = string.Empty;
    public bool IsReadyToReturn { get; set; }
    public bool LaunchConfirmed { get; set; }
    public bool IsOverloaded { get; set; }
    public float CargoWeightLimit { get; set; } = 140f;
    public List<ItemStack> CargoItems { get; } = new();
    public List<string> ReturningItemInstanceIds { get; } = new();
    public List<string> ReturningAwakenedUnitIds { get; } = new();
    public List<string> ReturningChipIds { get; } = new();
    public List<string> ReturningBlueprintIds { get; } = new();
}
