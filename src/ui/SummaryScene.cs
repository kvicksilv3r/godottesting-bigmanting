using Godot;

namespace BigManTing;

public partial class SummaryScene : Control
{
    private Label _scoreHeader = null!;
    private VBoxContainer _collectedList = null!;
    private VBoxContainer _comboList = null!;
    private Control _newDiscoveriesSection = null!;
    private VBoxContainer _newDiscoveriesList = null!;
    private Control _newUnlocksSection = null!;
    private VBoxContainer _newUnlocksList = null!;
    private Button _playAgainButton = null!;
    private Button _codexButton = null!;

    public override void _Ready()
    {
        _scoreHeader = GetNode<Label>("%ScoreHeader");
        _collectedList = GetNode<VBoxContainer>("%CollectedList");
        _comboList = GetNode<VBoxContainer>("%ComboList");
        _newDiscoveriesSection = GetNode<Control>("%NewDiscoveriesSection");
        _newDiscoveriesList = GetNode<VBoxContainer>("%NewDiscoveriesList");
        _newUnlocksSection = GetNode<Control>("%NewUnlocksSection");
        _newUnlocksList = GetNode<VBoxContainer>("%NewUnlocksList");
        _playAgainButton = GetNode<Button>("%PlayAgainButton");
        _codexButton = GetNode<Button>("%CodexButton");

        _playAgainButton.Pressed += () => GetTree().ChangeSceneToFile("res://src/ui/SwipeTestScene.tscn");
        _codexButton.Pressed += () =>
        {
            GameState.CodexReturnScene = "res://src/ui/SummaryScene.tscn";
            GetTree().ChangeSceneToFile("res://src/ui/CodexScene.tscn");
        };

        PopulateUI();
    }

    private void PopulateUI()
    {
        var collection = GetNode<CollectionManager>("/root/CollectionManager");
        var gameState = GetNode<GameState>("/root/GameState");

        _scoreHeader.Text = $"Round complete — Score: {collection.Score}";

        foreach (var item in collection.CollectedItems)
            _collectedList.AddChild(MakeLabel(item.DisplayName));

        foreach (var combo in collection.TriggeredCombosThisRound)
            _comboList.AddChild(MakeLabel($"{combo.ComboName}  +{combo.BonusPoints}pts"));

        if (gameState.NewlyDiscoveredThisRound.Count > 0)
        {
            _newDiscoveriesSection.Visible = true;
            foreach (var combo in gameState.NewlyDiscoveredThisRound)
                _newDiscoveriesList.AddChild(MakeLabel(combo.ComboName));
        }
        else
        {
            _newDiscoveriesSection.Visible = false;
        }

        if (gameState.NewlyUnlockedThisRound.Count > 0)
        {
            _newUnlocksSection.Visible = true;
            foreach (var item in gameState.NewlyUnlockedThisRound)
                _newUnlocksList.AddChild(MakeLabel(item.DisplayName));
        }
        else
        {
            _newUnlocksSection.Visible = false;
        }
    }

    private static Label MakeLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        return label;
    }
}
