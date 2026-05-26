using System.Collections.Generic;

namespace GodotGame;

public sealed class RocketState
{
    public bool IsConstructed { get; set; }
    public bool IsReadyToReturn { get; set; }
    public List<ItemStack> CargoItems { get; } = new();
    public List<string> ReturningAwakenedUnitIds { get; } = new();
    public List<string> ReturningChipIds { get; } = new();
    public List<string> ReturningBlueprintIds { get; } = new();
}
