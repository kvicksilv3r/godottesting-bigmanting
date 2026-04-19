using Godot;

namespace BigManTing;

public partial class SwipeCard : Control
{
	[Signal] public delegate void SwipedRightEventHandler(ItemResource item);
	[Signal] public delegate void SwipedLeftEventHandler(ItemResource item);

	[Export] public float SwipeThreshold { get; set; } = 150f;
	[Export] public float MaxRotationDeg { get; set; } = 15f;

	private ItemResource _item;
	private Vector2 _homePosition;
	private Vector2 _dragStart;
	private bool _dragging = false;
	private float _dragX = 0f;

	private Label _nameLabel = null!;
	private Label _descLabel = null!;
	private TextureRect _iconRect = null!;
	private ColorRect _collectOverlay = null!;
	private ColorRect _rejectOverlay = null!;

	public override void _Ready()
	{
		_homePosition = Position;
		PivotOffset = Size / 2f;

		_nameLabel = GetNode<Label>("%ItemName");
		_descLabel = GetNode<Label>("%ItemDescription");
		_iconRect = GetNode<TextureRect>("%ItemIcon");
		_collectOverlay = GetNode<ColorRect>("%CollectOverlay");
		_rejectOverlay = GetNode<ColorRect>("%RejectOverlay");
	}

	public void SetItem(ItemResource item)
	{
		_item = item;
		_nameLabel.Text = item.DisplayName;
		_descLabel.Text = item.Description;
		_iconRect.Texture = item.Icon;
		_iconRect.Visible = item.Icon != null;
	}

	// Initial press must land on the card
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
		{
			_dragging = true;
			_dragStart = GetGlobalMousePosition();
		}
	}

	// Track drag and release globally so fast mouse movement stays captured
	public override void _Input(InputEvent @event)
	{
		if (!_dragging) return;

		if (@event is InputEventMouseMotion mm)
		{
			UpdateDrag(mm.GlobalPosition);
		}
		else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
		{
			_dragging = false;
			ReleaseDrag();
		}
	}

	private void UpdateDrag(Vector2 mousePos)
	{
		Vector2 delta = mousePos - _dragStart;
		_dragX = delta.X;
		Position = _homePosition + delta;
		Rotation = Mathf.DegToRad(_dragX / SwipeThreshold * MaxRotationDeg);

		float t = Mathf.Clamp(Mathf.Abs(_dragX) / SwipeThreshold, 0f, 1f);
		_collectOverlay.Modulate = new Color(1, 1, 1, _dragX > 0 ? t : 0f);
		_rejectOverlay.Modulate = new Color(1, 1, 1, _dragX < 0 ? t : 0f);
	}

	private void ReleaseDrag()
	{
		if (_dragX > SwipeThreshold)
		{
			AnimateOut(1);
		}
		else if (_dragX < -SwipeThreshold)
		{
			AnimateOut(-1);
		}
		else
			SnapBack();
	}

	private void SnapBack()
	{
		var tween = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Spring);
		tween.TweenProperty(this, "position", _homePosition, 0.4f);
		tween.Parallel().TweenProperty(this, "rotation", 0f, 0.4f);
		_collectOverlay.Modulate = new Color(1, 1, 1, 0f);
		_rejectOverlay.Modulate = new Color(1, 1, 1, 0f);
	}

	private void AnimateOut(int direction)
	{
		_dragging = false;
		Vector2 target = _homePosition + new Vector2(direction * 900f, -50f);
		var tween = CreateTween()
			.SetEase(Tween.EaseType.In)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "position", target, 0.3f);
		tween.TweenCallback(Callable.From(() =>
		{
			if (direction > 0)
				EmitSignal(SignalName.SwipedRight, _item);
			else
				EmitSignal(SignalName.SwipedLeft, _item);
		}));
	}
}
