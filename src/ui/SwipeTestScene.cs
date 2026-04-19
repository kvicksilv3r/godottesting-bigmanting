using Godot;
using System.Collections.Generic;
using System.Linq;

namespace BigManTing;

public partial class SwipeTestScene : Control
{
	[Export] public PackedScene? CardScene { get; set; }

	private CollectionManager _collection = null!;
	private GameState _gameState = null!;
	private Label _scoreLabel = null!;
	private Label _comboLabel = null!;
	private Label _statusLabel = null!;
	private Label _takenLabel = null!;
	private Label _remainingLabel = null!;
	private Label _availableCombosLabel = null!;
	private Label _liveCombosLabel = null!;
	private VBoxContainer _trackedCombosPanel = null!;
	private VBoxContainer _collectedItemsPanel = null!;
	private Control _cardArea = null!;

	private Dictionary<string, string> _tagToName = new();

	private readonly Queue<ItemResource> _deck = new();

	public override void _Ready()
	{
		_collection = GetNode<CollectionManager>("/root/CollectionManager");
		_gameState = GetNode<GameState>("/root/GameState");
		_scoreLabel = GetNode<Label>("%ScoreLabel");
		_comboLabel = GetNode<Label>("%ComboLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_takenLabel = GetNode<Label>("%TakenLabel");
		_remainingLabel = GetNode<Label>("%RemainingLabel");
		_availableCombosLabel = GetNode<Label>("%AvailableCombosLabel");
		_liveCombosLabel = GetNode<Label>("%LiveCombosLabel");
		_trackedCombosPanel = GetNode<VBoxContainer>("%TrackedCombosPanel");
		_collectedItemsPanel = GetNode<VBoxContainer>("%CollectedItemsPanel");
		_cardArea = GetNode<Control>("%CardArea");

		foreach (var item in _gameState.AllItems)
			foreach (var tag in item.Tags)
				_tagToName.TryAdd(tag, item.DisplayName);

		_collection.ScoreChanged += OnScoreChanged;
		_collection.ComboTriggered += OnComboTriggered;
		_collection.RoundEnded += EndRound;
		_collection.ItemCollected += OnItemCollected;
		_collection.ItemRejected += OnItemRejected;
		_gameState.ItemUnlocked += OnItemUnlocked;
		_gameState.ComboDiscovered += OnComboDiscovered;

		_collection.Reset();
		BuildDeck();
		ShowNextCard();
		UpdateAvailableCombos();
		PopulateTrackedCombos();
		UpdateLiveCombos();
	}

	public override void _ExitTree()
	{
		_collection.ScoreChanged -= OnScoreChanged;
		_collection.ComboTriggered -= OnComboTriggered;
		_collection.RoundEnded -= EndRound;
		_collection.ItemCollected -= OnItemCollected;
		_collection.ItemRejected -= OnItemRejected;
		_gameState.ItemUnlocked -= OnItemUnlocked;
		_gameState.ComboDiscovered -= OnComboDiscovered;
	}

	private void OnScoreChanged(int score) => _scoreLabel.Text = $"Score: {score}";
	private void OnItemCollected(ItemResource _)
	{
		UpdateCounters();
		PopulateCollectedItems();
		UpdateLiveCombos();
	}
	private void OnItemRejected(ItemResource _) => UpdateLiveCombos();
	private void OnItemUnlocked(ItemResource _) => UpdateAvailableCombos();
	private void OnComboDiscovered(ComboRule _) => PopulateTrackedCombos();

	private void UpdateAvailableCombos()
	{
		var count = _gameState.PossibleCombos.Count;
		_availableCombosLabel.Text = $"{count} combos available";
	}

	private void UpdateLiveCombos()
	{
		var count = CountAchievableCombos();
		_liveCombosLabel.Text = $"{count} combos reachable";
	}

	private int CountAchievableCombos()
	{
		var collectedCounts = new Dictionary<string, int>();
		foreach (var item in _collection.CollectedItems)
			foreach (var tag in item.Tags)
			{
				collectedCounts.TryGetValue(tag, out var c);
				collectedCounts[tag] = c + 1;
			}

		var deckCounts = new Dictionary<string, int>();
		foreach (var item in _deck)
			foreach (var tag in item.Tags)
			{
				deckCounts.TryGetValue(tag, out var c);
				deckCounts[tag] = c + 1;
			}

		var remainingSlots = _collection.MaxCollectPerRound - _collection.CollectedItems.Count;

		var achievable = 0;
		foreach (var combo in _gameState.PossibleCombos)
		{
			if (_collection.TriggeredCombosThisRound.Any(c => c.ComboName == combo.ComboName)) continue;

			var possible = true;
			var stillNeeded = 0;
			foreach (var req in combo.Requirements)
			{
				collectedCounts.TryGetValue(req.Tag, out var have);
				var need = req.Count - have;
				if (need <= 0) continue;

				deckCounts.TryGetValue(req.Tag, out var inDeck);
				if (inDeck < need) { possible = false; break; }
				stillNeeded += need;
			}

			if (possible && stillNeeded <= remainingSlots)
				achievable++;
		}

		return achievable;
	}

	private void PopulateTrackedCombos()
	{
		foreach (Node child in _trackedCombosPanel.GetChildren())
			child.QueueFree();

		if (_gameState.TrackedComboNames.Count == 0)
			return;

		var header = new Label();
		header.Text = "Tracking";
		header.AddThemeFontSizeOverride("font_size", 13);
		header.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
		_trackedCombosPanel.AddChild(header);

		foreach (var name in _gameState.TrackedComboNames)
		{
			var combo = FindCombo(name);
			if (combo == null) continue;

			var nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.AddThemeFontSizeOverride("font_size", 14);
			nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.2f));
			_trackedCombosPanel.AddChild(nameLabel);

			var revealedIndices = _gameState.GetRevealedIndices(name);
			var reqParts = new List<string>();
			for (var i = 0; i < combo.Requirements.Count; i++)
			{
				var req = combo.Requirements[i];
				if (revealedIndices.Contains(i))
				{
					var itemName = _tagToName.TryGetValue(req.Tag, out var n) ? n : req.Tag;
					reqParts.Add(req.Count > 1 ? $"{itemName} ×{req.Count}" : itemName);
				}
				else
				{
					reqParts.Add("???");
				}
			}

			var reqLabel = new Label();
			reqLabel.Text = string.Join(", ", reqParts);
			reqLabel.AddThemeFontSizeOverride("font_size", 12);
			reqLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			reqLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			_trackedCombosPanel.AddChild(reqLabel);
		}
	}

	private void PopulateCollectedItems()
	{
		foreach (Node child in _collectedItemsPanel.GetChildren())
			child.QueueFree();

		var header = new Label();
		header.Text = "Collected";
		header.AddThemeFontSizeOverride("font_size", 13);
		header.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
		_collectedItemsPanel.AddChild(header);

		foreach (var item in _collection.CollectedItems)
		{
			var label = new Label();
			label.Text = item.DisplayName;
			label.AddThemeFontSizeOverride("font_size", 14);
			_collectedItemsPanel.AddChild(label);
		}
	}

	private ComboRule? FindCombo(string name)
	{
		foreach (var combo in _gameState.AllCombos)
			if (combo.ComboName == name) return combo;
		return null;
	}

	private void BuildDeck()
	{
		var pool = _gameState.GetUnlockedItems();
		Shuffle(pool);

		// Guarantee one item per requirement slot for each tracked combo
		var guaranteed = new List<ItemResource>();
		var guaranteedIds = new HashSet<string>();

		foreach (var comboName in _gameState.TrackedComboNames)
		{
			var combo = FindCombo(comboName);
			if (combo == null) continue;

			foreach (var req in combo.Requirements)
			{
				var needed = req.Count;
				foreach (var item in pool)
				{
					if (needed <= 0) break;
					if (guaranteedIds.Contains(item.Id)) continue;
					foreach (var tag in item.Tags)
					{
						if (tag != req.Tag) continue;
						guaranteed.Add(item);
						guaranteedIds.Add(item.Id);
						needed--;
						break;
					}
				}
			}
		}

		var deckSize = System.Math.Min(20, pool.Count);

		// Fill remaining slots with non-guaranteed items
		var deck = new List<ItemResource>(guaranteed);
		foreach (var item in pool)
		{
			if (deck.Count >= deckSize) break;
			if (!guaranteedIds.Contains(item.Id))
				deck.Add(item);
		}

		// Shuffle so guaranteed items aren't always in predictable positions
		Shuffle(deck);

		foreach (var item in deck)
			_deck.Enqueue(item);

		UpdateCounters();
	}

	private void UpdateCounters()
	{
		_takenLabel.Text = $"{_collection.CollectedItems.Count}/{_collection.MaxCollectPerRound} taken";
		_remainingLabel.Text = $"{_deck.Count} cards remaining";
	}

	private static void Shuffle(List<ItemResource> list)
	{
		var rng = new System.Random();
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

	private void ShowNextCard()
	{
		if (CardScene == null)
		{
			_statusLabel.Text = "Assign CardScene in the inspector.";
			_statusLabel.Visible = true;
			return;
		}

		if (_deck.Count == 0)
		{
			EndRound();
			return;
		}

		var item = _deck.Dequeue();
		var card = CardScene.Instantiate<SwipeCard>();
		card.Size = card.CustomMinimumSize;
		card.Position = (_cardArea.Size - card.Size) / 2f;
		_cardArea.AddChild(card);

		card.SetItem(item);
		card.SwipedRight += OnSwipedRight;
		card.SwipedLeft += OnSwipedLeft;
		UpdateCounters();
	}

	private void OnSwipedRight(ItemResource item)
	{
		_collection.Collect(item);
		if (_collection.CollectedItems.Count < _collection.MaxCollectPerRound)
			ShowNextCard();
	}

	private void OnSwipedLeft(ItemResource item)
	{
		_collection.Reject(item);
		ShowNextCard();
	}

	private void OnComboTriggered(ComboRule combo)
	{
		_comboLabel.Text = $"COMBO: {combo.ComboName}  +{combo.BonusPoints}pts!";
		GetTree().CreateTimer(2.5).Timeout += () => _comboLabel.Text = "";
		UpdateLiveCombos();
	}

	private void EndRound()
	{
		_gameState.Save();
		GetTree().ChangeSceneToFile("res://src/ui/SummaryScene.tscn");
	}
}
