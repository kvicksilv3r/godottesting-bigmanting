using Godot;

namespace BigManTing;

public partial class MainMenuScene : Control
{
	public override void _Ready()
	{
		GetNode<Button>("%PlayButton").Pressed += () =>
			GetTree().ChangeSceneToFile("res://src/ui/SwipeTestScene.tscn");

		GetNode<Button>("%CodexButton").Pressed += () =>
		{
			GameState.CodexReturnScene = "res://src/ui/MainMenuScene.tscn";
			GetTree().ChangeSceneToFile("res://src/ui/CodexScene.tscn");
		};

		var clearSaveDialog = GetNode<ConfirmationDialog>("%ClearSaveDialog");

		GetNode<Button>("%ClearSaveButton").Pressed += () =>
		{
			clearSaveDialog.PopupCentered();
		};

		clearSaveDialog.Confirmed += () =>
		{
			GetNode<GameState>("/root/GameState").ClearSave();
		};
	}
}
