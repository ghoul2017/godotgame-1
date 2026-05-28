using System.Collections.Generic;
using Godot;

namespace GodotGame;

public partial class UnitData : Resource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitRole { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int BaseDurability { get; set; }
    public int BaseEnergy { get; set; }
    public float MoveSpeed { get; set; } = 100f;
    public int InventoryCapacity { get; set; }
    public float CarryWeightLimit { get; set; }
    public List<string> EquipmentSlots { get; } = new();
    public List<string> AvailableCommands { get; } = new();
    public string DefaultBehaviorMode { get; set; } = "balanced";
    public bool IsAwakenedCapable { get; set; }
    public string IconPath { get; set; } = string.Empty;
    public string PortraitPath { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public float SelectionRadius { get; set; } = 18f;
    public List<string> Tags { get; } = new();
}
