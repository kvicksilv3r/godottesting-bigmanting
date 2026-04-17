using Godot;

namespace BigManTing;

[GlobalClass]
public partial class ItemResource : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public string Description { get; set; } = "";
	[Export] public Texture2D? Icon { get; set; }
	[Export] public string[] Tags { get; set; } = [];
	[Export] public int BaseValue { get; set; } = 1;
}
