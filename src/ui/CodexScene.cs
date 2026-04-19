using System;
using System.Collections.Generic;
using Godot;

namespace BigManTing;

public partial class CodexScene : Control
{
    private enum SortMode { Chronological, Alphabetical }

    private VBoxContainer _comboList = null!;
    private VBoxContainer _itemList = null!;
    private Button _hintButton = null!;
    private Label _pointsLabel = null!;
    private Label _trackedLabel = null!;
    private GameState _gameState = null!;
    private Dictionary<string, string> _tagToName = new();
    private Dictionary<string, string> _itemIdToName = new();
    private SortMode _sortMode = SortMode.Chronological;

    public override void _Ready()
    {
        _comboList = GetNode<VBoxContainer>("%ComboList");
        _itemList = GetNode<VBoxContainer>("%ItemList");
        _hintButton = GetNode<Button>("%HintButton");
        _pointsLabel = GetNode<Label>("%PointsLabel");
        _trackedLabel = GetNode<Label>("%TrackedLabel");
        _gameState = GetNode<GameState>("/root/GameState");

        foreach (var item in _gameState.AllItems)
        {
            foreach (var tag in item.Tags)
                _tagToName.TryAdd(tag, item.DisplayName);
            _itemIdToName.TryAdd(item.Id, item.DisplayName);
        }

        GetNode<Button>("%BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile(GameState.CodexReturnScene);

        GetNode<Button>("%RecentButton").Pressed += () => SetSort(SortMode.Chronological);
        GetNode<Button>("%AlphaButton").Pressed += () => SetSort(SortMode.Alphabetical);

        _hintButton.Pressed += () =>
        {
            _gameState.TryRevealHint();
            PopulateCodex();
            UpdateHintButton();
        };

        PopulateCodex();
        PopulateItems();
        UpdateHintButton();
    }

    private void SetSort(SortMode mode)
    {
        _sortMode = mode;
        PopulateCodex();
        UpdateHintButton();
    }

    private void UpdateHintButton()
    {
        _pointsLabel.Text = $"Your points: {_gameState.TotalPoints}";
        _trackedLabel.Text = $"Tracked combos: {_gameState.TrackedComboNames.Count}/{GameState.MaxTrackedCombos}";

        var hasCandidate = false;
        foreach (var combo in _gameState.PossibleCombos)
        {
            if (!_gameState.IsComboDiscovered(combo.ComboName) && !_gameState.IsComboHinted(combo.ComboName))
            {
                hasCandidate = true;
                break;
            }
        }

        _hintButton.Disabled = _gameState.TotalPoints < GameState.HintCost || !hasCandidate;
    }

    private void PopulateCodex()
    {
        foreach (Node child in _comboList.GetChildren())
            child.QueueFree();

        var combos = new List<ComboRule>(_gameState.AllCombos);

        if (_sortMode == SortMode.Chronological)
        {
            combos.Sort((a, b) =>
            {
                var aTier = GetTier(a);
                var bTier = GetTier(b);
                if (aTier != bTier) return aTier.CompareTo(bTier);
                if (aTier == 1) // discovered — sort newest first
                {
                    _gameState.DiscoveredCombos.TryGetValue(a.ComboName, out var aTs);
                    _gameState.DiscoveredCombos.TryGetValue(b.ComboName, out var bTs);
                    return bTs.CompareTo(aTs);
                }
                return string.Compare(a.ComboName, b.ComboName, StringComparison.OrdinalIgnoreCase);
            });
        }
        else
        {
            combos.Sort((a, b) =>
            {
                var aTier = GetTier(a);
                var bTier = GetTier(b);
                if (aTier != bTier) return aTier.CompareTo(bTier);
                if (aTier == 2) return 0;
                return string.Compare(a.ComboName, b.ComboName, StringComparison.OrdinalIgnoreCase);
            });
        }

        foreach (var combo in combos)
        {
            var entry = new VBoxContainer();

            if (_gameState.IsComboDiscovered(combo.ComboName))
            {
                var row = new HBoxContainer();

                var left = new VBoxContainer();
                left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                var reqs = string.Join(", ", System.Linq.Enumerable.Select(combo.Requirements,
                    r =>
                    {
                        var name = _tagToName.TryGetValue(r.Tag, out var n) ? n : r.Tag;
                        return r.Count > 1 ? $"{name} ×{r.Count}" : name;
                    }));
                left.AddChild(MakeLabel($"{combo.ComboName}  +{combo.BonusPoints}pts", 18, Colors.White));
                left.AddChild(MakeLabel(combo.Description, 14, Colors.White));
                left.AddChild(MakeLabel($"Needs: {reqs}", 13, new Color(0.7f, 0.7f, 0.7f)));
                row.AddChild(left);

                if (!string.IsNullOrEmpty(combo.RewardItemId) &&
                    _itemIdToName.TryGetValue(combo.RewardItemId, out var rewardName))
                {
                    var right = new VBoxContainer();
                    right.AddChild(MakeLabel("Unlocks", 12, new Color(0.6f, 0.6f, 0.6f)));
                    right.AddChild(MakeLabel(rewardName, 15, new Color(0.4f, 0.9f, 0.5f)));

                    var margin = new MarginContainer();
                    margin.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
                    margin.AddThemeConstantOverride("margin_right", 20);
                    margin.AddChild(right);
                    row.AddChild(margin);
                }

                entry.AddChild(row);
            }
            else if (_gameState.IsComboHinted(combo.ComboName))
            {
                var row = new HBoxContainer();

                var left = new VBoxContainer();
                left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                left.AddChild(MakeLabel(combo.ComboName, 18, new Color(0.9f, 0.75f, 0.2f)));

                var revealedIndices = _gameState.GetRevealedIndices(combo.ComboName);
                var reqParts = new List<string>();
                for (var i = 0; i < combo.Requirements.Count; i++)
                {
                    var req = combo.Requirements[i];
                    if (revealedIndices.Contains(i))
                    {
                        var name = _tagToName.TryGetValue(req.Tag, out var n) ? n : req.Tag;
                        reqParts.Add(req.Count > 1 ? $"{name} ×{req.Count}" : name);
                    }
                    else
                    {
                        reqParts.Add("???");
                    }
                }
                left.AddChild(MakeLabel(string.Join(", ", reqParts), 13, new Color(0.5f, 0.5f, 0.5f)));
                row.AddChild(left);

                var rightButtons = new VBoxContainer();

                var isTracked = _gameState.IsComboTracked(combo.ComboName);
                var trackBtn = new Button();
                trackBtn.Text = isTracked ? "Untrack" : "Track";
                trackBtn.Disabled = !isTracked && _gameState.TrackedComboNames.Count >= GameState.MaxTrackedCombos;
                var comboName = combo.ComboName;
                trackBtn.Pressed += () =>
                {
                    if (_gameState.IsComboTracked(comboName))
                        _gameState.UntrackCombo(comboName);
                    else
                        _gameState.TryTrackCombo(comboName);
                    PopulateCodex();
                    UpdateHintButton();
                };
                rightButtons.AddChild(trackBtn);

                if (combo.Requirements.Count - revealedIndices.Count > 1)
                {
                    var cost = _gameState.GetRequirementRevealCost(combo.ComboName);
                    var revealBtn = new Button();
                    revealBtn.Text = $"Reveal 1 ({cost}pts)";
                    revealBtn.Disabled = _gameState.TotalPoints < cost;
                    var comboRef = combo;
                    revealBtn.Pressed += () =>
                    {
                        _gameState.TryRevealRequirement(comboRef);
                        PopulateCodex();
                        UpdateHintButton();
                    };
                    rightButtons.AddChild(revealBtn);
                }

                row.AddChild(rightButtons);
                entry.AddChild(row);
            }
            else
            {
                entry.AddChild(MakeLabel("???", 18, new Color(0.4f, 0.4f, 0.4f)));
            }

            _comboList.AddChild(entry);
            _comboList.AddChild(new HSeparator());
        }
    }

    private void PopulateItems()
    {
        foreach (Node child in _itemList.GetChildren())
            child.QueueFree();

        foreach (var item in _gameState.AllItems)
        {
            var entry = new VBoxContainer();

            if (_gameState.IsItemUnlocked(item.Id))
            {
                entry.AddChild(MakeLabel($"{item.DisplayName}  +{item.BaseValue}pts", 18, Colors.White));
                if (!string.IsNullOrEmpty(item.Description))
                    entry.AddChild(MakeLabel(item.Description, 14, Colors.White));
                if (item.Tags.Length > 0)
                    entry.AddChild(MakeLabel(string.Join(", ", item.Tags), 13, new Color(0.7f, 0.7f, 0.7f)));
            }
            else
            {
                entry.AddChild(MakeLabel("???", 18, new Color(0.4f, 0.4f, 0.4f)));
            }

            _itemList.AddChild(entry);
            _itemList.AddChild(new HSeparator());
        }
    }

    // 0 = hinted (not yet discovered), 1 = discovered, 2 = unknown
    private int GetTier(ComboRule combo)
    {
        if (_gameState.IsComboDiscovered(combo.ComboName)) return 1;
        if (_gameState.IsComboHinted(combo.ComboName)) return 0;
        return 2;
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
