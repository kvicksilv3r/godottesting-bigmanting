using Godot;
using Godot.Collections;

namespace BigManTing;

public partial class SwipeTestScene : Control
{
	[Export] public Array<ItemResource> Items { get; set; } = [];
	[Export] public PackedScene? CardScene { get; set; }

	private CollectionManager? _collection;
	private Label _scoreLabel = null!;
	private Label _comboLabel = null!;
	private Label _statusLabel = null!;
	private Control _cardArea = null!;
	private int _currentIndex = 0;

	public override void _Ready()
	{
		GD.Print($"[SwipeTestScene] Ready — CardScene={CardScene}, Items={Items.Count}");
		_collection = GetNodeOrNull<CollectionManager>("/root/CollectionManager");
		_scoreLabel = GetNode<Label>("%ScoreLabel");
		_comboLabel = GetNode<Label>("%ComboLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_cardArea = GetNode<Control>("%CardArea");

		if (_collection != null)
		{
			_collection.ScoreChanged += score => _scoreLabel.Text = $"Score: {score}";
			_collection.ComboTriggered += OnComboTriggered;
		}

		ShowNextCard();
	}

	private void ShowNextCard()
	{
		if (CardScene == null || _currentIndex >= Items.Count)
		{
			_statusLabel.Text = CardScene == null
				? "Assign Items and CardScene in the inspector."
				: $"Done! Final score: {_collection?.Score ?? 0}";
			_statusLabel.Visible = true;
			return;
		}

		var card = CardScene.Instantiate<SwipeCard>();
		card.Size = card.CustomMinimumSize;
		card.Position = (_cardArea.Size - card.Size) / 2f;
		_cardArea.AddChild(card); // _Ready() runs here, capturing the correct position

		card.SetItem(Items[_currentIndex]);
		card.SwipedRight += OnSwipedRight;
		card.SwipedLeft += OnSwipedLeft;
		_currentIndex++;
	}

	private void OnSwipedRight(ItemResource item)
	{
		_collection?.Collect(item);
		ShowNextCard();
	}

	private void OnSwipedLeft(ItemResource item)
	{
		_collection?.Reject(item);
		ShowNextCard();
	}

	private void OnComboTriggered(ComboRule combo)
	{
		_comboLabel.Text = $"COMBO: {combo.ComboName}  +{combo.BonusPoints}pts!";
		GetTree().CreateTimer(2.5).Timeout += () => _comboLabel.Text = "";
	}
}
