using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Godot.Collections;

namespace BigManTing;

public partial class GameState : Node
{
	[Signal] public delegate void ItemUnlockedEventHandler(ItemResource item);
	[Signal] public delegate void ComboDiscoveredEventHandler(ComboRule combo);

	[Export] public Array<string> StarterItemIds { get; set; } = [];

	public Array<ItemResource> AllItems { get; private set; } = [];
	public Array<ComboRule> AllCombos { get; private set; } = [];

	public HashSet<string> UnlockedItemIds { get; } = [];
	public System.Collections.Generic.Dictionary<string, DateTime> DiscoveredCombos { get; } = [];
	public List<ItemResource> NewlyUnlockedThisRound { get; } = [];
	public List<ComboRule> NewlyDiscoveredThisRound { get; } = [];
	public List<ComboRule> PossibleCombos { get; private set; } = [];
	public int TotalPoints { get; set; } = 0;
	public int PossibleCombosCountAtRoundStart { get; set; } = 0;
	public HashSet<string> HintedComboIds { get; } = [];
	public List<string> TrackedComboNames { get; } = [];
	public System.Collections.Generic.Dictionary<string, List<int>> RevealedRequirements { get; } = new();
	public const int HintCost = 100;
	public const int MaxTrackedCombos = 2;
	public const int RevealCostBase = 75;

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

	public void RecomputePossibleCombos()
	{
		var tagCounts = new System.Collections.Generic.Dictionary<string, int>();
		foreach (var item in AllItems)
		{
			if (!UnlockedItemIds.Contains(item.Id)) continue;
			foreach (var tag in item.Tags)
			{
				tagCounts.TryGetValue(tag, out var count);
				tagCounts[tag] = count + 1;
			}
		}

		PossibleCombos = [];
		foreach (var combo in AllCombos)
		{
			if (combo.Requirements.Count == 0) continue;
			var possible = true;
			foreach (var req in combo.Requirements)
			{
				tagCounts.TryGetValue(req.Tag, out var have);
				if (have < req.Count) { possible = false; break; }
			}
			if (possible) PossibleCombos.Add(combo);
		}
	}

	public bool IsComboDiscovered(string id) => DiscoveredCombos.ContainsKey(id);
	public bool IsComboHinted(string id) => HintedComboIds.Contains(id);

	public List<int> GetRevealedIndices(string comboName) =>
		RevealedRequirements.TryGetValue(comboName, out var indices) ? indices : new List<int>();

	public int GetRequirementRevealCost(string comboName) =>
		(GetRevealedIndices(comboName).Count + 1) * RevealCostBase;

	public bool TryRevealRequirement(ComboRule combo)
	{
		var revealed = GetRevealedIndices(combo.ComboName);

		// Always keep the last requirement hidden
		if (combo.Requirements.Count - revealed.Count <= 1) return false;

		var nextIndex = -1;
		for (var i = 0; i < combo.Requirements.Count; i++)
			if (!revealed.Contains(i)) { nextIndex = i; break; }

		if (nextIndex == -1) return false;

		var cost = GetRequirementRevealCost(combo.ComboName);
		if (TotalPoints < cost) return false;

		TotalPoints -= cost;
		if (!RevealedRequirements.ContainsKey(combo.ComboName))
			RevealedRequirements[combo.ComboName] = new List<int>();
		RevealedRequirements[combo.ComboName].Add(nextIndex);
		Save();
		return true;
	}
	public bool IsComboTracked(string name) => TrackedComboNames.Contains(name);

	public bool TryTrackCombo(string name)
	{
		if (TrackedComboNames.Contains(name) || TrackedComboNames.Count >= MaxTrackedCombos)
			return false;
		TrackedComboNames.Add(name);
		Save();
		return true;
	}

	public void UntrackCombo(string name)
	{
		TrackedComboNames.Remove(name);
		Save();
	}

	public bool TryRevealHint()
	{
		var candidates = new List<ComboRule>();
		foreach (var combo in PossibleCombos)
			if (!IsComboDiscovered(combo.ComboName) && !IsComboHinted(combo.ComboName))
				candidates.Add(combo);

		if (candidates.Count == 0 || TotalPoints < HintCost)
			return false;

		TotalPoints -= HintCost;
		var pick = candidates[(int)GD.RandRange(0, candidates.Count - 1)];
		HintedComboIds.Add(pick.ComboName);
		Save();
		return true;
	}

	public ItemResource? MarkComboDiscovered(ComboRule combo)
	{
		if (DiscoveredCombos.TryAdd(combo.ComboName, DateTime.UtcNow))
		{
			TrackedComboNames.Remove(combo.ComboName);
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
			RecomputePossibleCombos();
			EmitSignal(SignalName.ItemUnlocked, reward);
		}

		return reward;
	}

	public void ClearSave()
	{
		if (FileAccess.FileExists(SavePath))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));

		UnlockedItemIds.Clear();
		DiscoveredCombos.Clear();
		HintedComboIds.Clear();
		TrackedComboNames.Clear();
		RevealedRequirements.Clear();
		NewlyUnlockedThisRound.Clear();
		NewlyDiscoveredThisRound.Clear();
		TotalPoints = 0;

		foreach (var id in StarterItemIds)
			UnlockedItemIds.Add(id);

		RecomputePossibleCombos();
	}

	public void Save()
	{
		var discoveredWithTimestamps = new System.Collections.Generic.Dictionary<string, string>();
		foreach (var kv in DiscoveredCombos)
			discoveredWithTimestamps[kv.Key] = kv.Value.ToString("O");

		var data = new System.Collections.Generic.Dictionary<string, object>
		{
			["discovered_combos"] = discoveredWithTimestamps,
			["unlocked_items"] = new List<string>(UnlockedItemIds),
			["hinted_combos"] = new List<string>(HintedComboIds),
			["tracked_combos"] = new List<string>(TrackedComboNames),
			["revealed_requirements"] = RevealedRequirements,
			["total_points"] = TotalPoints
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

		if (data.TryGetValue("total_points", out var totalPoints))
			TotalPoints = totalPoints.GetInt32();

		if (data.TryGetValue("hinted_combos", out var hinted))
			foreach (var id in hinted.EnumerateArray())
				HintedComboIds.Add(id.GetString() ?? "");

		if (data.TryGetValue("tracked_combos", out var tracked))
			foreach (var id in tracked.EnumerateArray())
				TrackedComboNames.Add(id.GetString() ?? "");

		if (data.TryGetValue("revealed_requirements", out var revealedReqs) &&
			revealedReqs.ValueKind == JsonValueKind.Object)
			foreach (var prop in revealedReqs.EnumerateObject())
			{
				var indices = new List<int>();
				foreach (var idx in prop.Value.EnumerateArray())
					indices.Add(idx.GetInt32());
				RevealedRequirements[prop.Name] = indices;
			}

		if (data.TryGetValue("discovered_combos", out var combos))
		{
			if (combos.ValueKind == JsonValueKind.Object)
			{
				foreach (var prop in combos.EnumerateObject())
				{
					var ts = DateTime.TryParse(prop.Value.GetString(), out var parsed) ? parsed : DateTime.MaxValue;
					DiscoveredCombos.TryAdd(prop.Name, ts);
				}
			}
			else if (combos.ValueKind == JsonValueKind.Array)
			{
				// backward compat: old saves stored only an array of names
				foreach (var id in combos.EnumerateArray())
					DiscoveredCombos.TryAdd(id.GetString() ?? "", DateTime.MaxValue);
			}
		}

		RecomputePossibleCombos();
	}
}
