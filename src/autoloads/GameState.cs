using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace BigManTing;

public partial class GameState : Node
{
	[Signal] public delegate void ItemUnlockedEventHandler(ItemResource item);
	[Signal] public delegate void ComboDiscoveredEventHandler(ComboRule combo);

	[Export] public Array<string> StarterItemIds { get; set; } = [];

	public Array<ItemResource> AllItems { get; private set; } = [];
	public Array<ComboRule> AllCombos { get; private set; } = [];

	public HashSet<string> UnlockedItemIds { get; } = [];
	public HashSet<string> DiscoveredComboIds { get; } = [];
	public List<ItemResource> NewlyUnlockedThisRound { get; } = [];
	public List<ComboRule> NewlyDiscoveredThisRound { get; } = [];

	public static string CodexReturnScene { get; set; } = "res://src/ui/MainMenuScene.tscn";

	private const string SavePath = "user://save.json";

	public override void _Ready()
	{
		AllItems = LoadResourcesFromDir<ItemResource>("res://resources/items/");
		AllCombos = LoadResourcesFromDir<ComboRule>("res://resources/combos/");
		Load();
	}

	private static Array<T> LoadResourcesFromDir<[MustBeVariant] T>(string dirPath) where T : Resource
	{
		var result = new Array<T>();
		var dir = DirAccess.Open(dirPath);
		if (dir == null) return result;

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (fileName != "")
		{
			if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
			{
				var resource = GD.Load<T>($"{dirPath}{fileName}");
				if (resource != null)
					result.Add(resource);
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
		return result;
	}

	public List<ItemResource> GetUnlockedItems()
	{
		var result = new List<ItemResource>();
		foreach (var item in AllItems)
			if (UnlockedItemIds.Contains(item.Id))
				result.Add(item);
		return result;
	}

	public bool IsItemUnlocked(string id) => UnlockedItemIds.Contains(id);

	public bool IsComboDiscovered(string id) => DiscoveredComboIds.Contains(id);

	public ItemResource? MarkComboDiscovered(ComboRule combo)
	{
		if (DiscoveredComboIds.Add(combo.ComboName))
		{
			NewlyDiscoveredThisRound.Add(combo);
			EmitSignal(SignalName.ComboDiscovered, combo);
		}

		if (string.IsNullOrEmpty(combo.RewardItemId))
			return null;

		if (!UnlockedItemIds.Add(combo.RewardItemId))
			return null;

		ItemResource? reward = null;
		foreach (var item in AllItems)
		{
			if (item.Id == combo.RewardItemId)
			{
				reward = item;
				break;
			}
		}

		if (reward != null)
		{
			NewlyUnlockedThisRound.Add(reward);
			EmitSignal(SignalName.ItemUnlocked, reward);
		}

		return reward;
	}

	public void ClearSave()
	{
		if (FileAccess.FileExists(SavePath))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));

		UnlockedItemIds.Clear();
		DiscoveredComboIds.Clear();
		NewlyUnlockedThisRound.Clear();
		NewlyDiscoveredThisRound.Clear();

		foreach (var id in StarterItemIds)
			UnlockedItemIds.Add(id);
	}

	public void Save()
	{
		var data = new System.Collections.Generic.Dictionary<string, object>
		{
			["discovered_combos"] = new List<string>(DiscoveredComboIds),
			["unlocked_items"] = new List<string>(UnlockedItemIds)
		};

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		file.StoreString(JsonSerializer.Serialize(data));
	}

	public void Load()
	{
		if (!FileAccess.FileExists(SavePath))
		{
			foreach (var id in StarterItemIds)
				UnlockedItemIds.Add(id);
			return;
		}

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		var json = file.GetAsText();
		var data = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, JsonElement>>(json);
		if (data == null) return;

		if (data.TryGetValue("unlocked_items", out var items))
			foreach (var id in items.EnumerateArray())
				UnlockedItemIds.Add(id.GetString() ?? "");

		if (data.TryGetValue("discovered_combos", out var combos))
			foreach (var id in combos.EnumerateArray())
				DiscoveredComboIds.Add(id.GetString() ?? "");
	}
}
