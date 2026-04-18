using Godot;
using Godot.Collections;
using System.Linq;

namespace BigManTing;

public partial class CollectionManager : Node
{
	[Signal] public delegate void ItemCollectedEventHandler(ItemResource item);
	[Signal] public delegate void ItemRejectedEventHandler(ItemResource item);
	[Signal] public delegate void ComboTriggeredEventHandler(ComboRule combo);
	[Signal] public delegate void ScoreChangedEventHandler(int newScore);
	[Signal] public delegate void RoundEndedEventHandler();

	[Export] public int MaxCollectPerRound { get; set; } = 5;

	public Array<ComboRule> ComboRules { get; private set; } = [];

	public override void _Ready()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		ComboRules = gameState.AllCombos;
	}

	public Array<ItemResource> CollectedItems { get; } = [];
	public System.Collections.Generic.List<ComboRule> TriggeredCombosThisRound { get; } = [];
	public int Score { get; private set; } = 0;

	private readonly System.Collections.Generic.HashSet<string> _triggeredCombos = [];

	public void Collect(ItemResource item)
	{
		CollectedItems.Add(item);
		Score += item.BaseValue;
		EmitSignal(SignalName.ItemCollected, item);
		EmitSignal(SignalName.ScoreChanged, Score);
		CheckCombos();
		if (CollectedItems.Count >= MaxCollectPerRound)
			EmitSignal(SignalName.RoundEnded);
	}

	public void Reject(ItemResource item)
	{
		EmitSignal(SignalName.ItemRejected, item);
	}

	public void Reset()
	{
		CollectedItems.Clear();
		TriggeredCombosThisRound.Clear();
		_triggeredCombos.Clear();
		Score = 0;
		EmitSignal(SignalName.ScoreChanged, Score);
		var gameState = GetNode<GameState>("/root/GameState");
		gameState.NewlyUnlockedThisRound.Clear();
		gameState.NewlyDiscoveredThisRound.Clear();
	}

	private void CheckCombos()
	{
		var tagCounts = new System.Collections.Generic.Dictionary<string, int>();
		foreach (var item in CollectedItems)
			foreach (var tag in item.Tags)
			{
				tagCounts.TryGetValue(tag, out int current);
				tagCounts[tag] = current + 1;
			}

		foreach (var combo in ComboRules)
		{
			if (_triggeredCombos.Contains(combo.ComboName))
				continue;
			if (combo.Requirements.Count == 0)
				continue;

			bool satisfied = true;
			foreach (var req in combo.Requirements)
			{
				tagCounts.TryGetValue(req.Tag, out int have);
				if (have < req.Count)
				{
					satisfied = false;
					break;
				}
			}

			if (satisfied)
			{
				_triggeredCombos.Add(combo.ComboName);
				TriggeredCombosThisRound.Add(combo);
				Score += combo.BonusPoints;
				var gameState = GetNode<GameState>("/root/GameState");
				gameState.MarkComboDiscovered(combo);
				EmitSignal(SignalName.ComboTriggered, combo);
				EmitSignal(SignalName.ScoreChanged, Score);
			}
		}
	}
}
