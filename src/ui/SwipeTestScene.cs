using Godot;
using System.Collections.Generic;

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
	private Control _cardArea = null!;

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
		_cardArea = GetNode<Control>("%CardArea");

		_collection.ScoreChanged += OnScoreChanged;
		_collection.ComboTriggered += OnComboTriggered;
		_collection.RoundEnded += EndRound;
		_collection.ItemCollected += OnItemCollected;

		_collection.Reset();
		BuildDeck();
		ShowNextCard();
	}

	public override void _ExitTree()
	{
		_collection.ScoreChanged -= OnScoreChanged;
		_collection.ComboTriggered -= OnComboTriggered;
		_collection.RoundEnded -= EndRound;
		_collection.ItemCollected -= OnItemCollected;
	}

	private void OnScoreChanged(int score) => _scoreLabel.Text = $"Score: {score}";
	private void OnItemCollected(ItemResource _) => UpdateCounters();

	private void BuildDeck()
	{
		var pool = _gameState.GetUnlockedItems();
		Shuffle(pool);
		var count = System.Math.Min(20, pool.Count);
		for (int i = 0; i < count; i++)
			_deck.Enqueue(pool[i]);
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
	}

	private void EndRound()
	{
		_gameState.Save();
		GetTree().ChangeSceneToFile("res://src/ui/SummaryScene.tscn");
	}
}
