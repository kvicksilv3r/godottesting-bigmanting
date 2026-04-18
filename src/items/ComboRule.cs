using Godot;
using Godot.Collections;

namespace BigManTing;

/// Defines a bonus awarded when the player's collection satisfies all tag requirements.
[GlobalClass]
public partial class ComboRule : Resource
{
	[Export] public string ComboName { get; set; } = "";
	[Export] public string Description { get; set; } = "";
	[Export] public Array<TagRequirement> Requirements { get; set; } = [];
	[Export] public int BonusPoints { get; set; } = 0;
	[Export] public string RewardItemId { get; set; } = "";
}
