using Godot;

namespace BigManTing;

[GlobalClass]
public partial class TagRequirement : Resource
{
    [Export] public string Tag { get; set; } = "";
    [Export] public int Count { get; set; } = 1;
}
