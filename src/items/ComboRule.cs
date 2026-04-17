using Godot;

namespace BigManTing;

/// Defines a bonus awarded when the player's collection contains all required tags.
[GlobalClass]
public partial class ComboRule : Resource
{
    [Export] public string ComboName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    /// All tags that must appear in the collected set to trigger this combo.
    [Export] public string[] RequiredTags { get; set; } = [];
    [Export] public int BonusPoints { get; set; } = 0;
}
