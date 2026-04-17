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

    [Export] public Array<ComboRule> ComboRules { get; set; } = [];

    public Array<ItemResource> CollectedItems { get; } = [];
    public int Score { get; private set; } = 0;

    private readonly System.Collections.Generic.HashSet<string> _triggeredCombos = [];

    public void Collect(ItemResource item)
    {
        CollectedItems.Add(item);
        Score += item.BaseValue;
        EmitSignal(SignalName.ItemCollected, item);
        EmitSignal(SignalName.ScoreChanged, Score);
        CheckCombos();
    }

    public void Reject(ItemResource item)
    {
        EmitSignal(SignalName.ItemRejected, item);
    }

    public void Reset()
    {
        CollectedItems.Clear();
        _triggeredCombos.Clear();
        Score = 0;
        EmitSignal(SignalName.ScoreChanged, Score);
    }

    private void CheckCombos()
    {
        var collectedTags = CollectedItems
            .SelectMany(i => i.Tags)
            .ToHashSet();

        foreach (var combo in ComboRules)
        {
            if (_triggeredCombos.Contains(combo.ComboName))
                continue;

            if (combo.RequiredTags.All(tag => collectedTags.Contains(tag)))
            {
                _triggeredCombos.Add(combo.ComboName);
                Score += combo.BonusPoints;
                EmitSignal(SignalName.ComboTriggered, combo);
                EmitSignal(SignalName.ScoreChanged, Score);
            }
        }
    }
}
