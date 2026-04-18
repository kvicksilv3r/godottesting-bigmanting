using Godot;

namespace BigManTing;

public partial class CodexScene : Control
{
    private VBoxContainer _comboList = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        _comboList = GetNode<VBoxContainer>("%ComboList");
        _backButton = GetNode<Button>("%BackButton");

        _backButton.Pressed += () => GetTree().ChangeSceneToFile(GameState.CodexReturnScene);

        PopulateCodex();
    }

    private void PopulateCodex()
    {
        var gameState = GetNode<GameState>("/root/GameState");

        foreach (var combo in gameState.AllCombos)
        {
            var entry = new VBoxContainer();

            if (gameState.IsComboDiscovered(combo.ComboName))
            {
                entry.AddChild(MakeLabel($"{combo.ComboName}  +{combo.BonusPoints}pts", 18, Colors.White));
                entry.AddChild(MakeLabel(combo.Description, 14, Colors.White));
                var reqs = string.Join(", ", System.Linq.Enumerable.Select(combo.Requirements,
                    r => r.Count > 1 ? $"{r.Tag} ×{r.Count}" : r.Tag));
                entry.AddChild(MakeLabel($"Needs: {reqs}", 13, new Color(0.7f, 0.7f, 0.7f)));
            }
            else
            {
                entry.AddChild(MakeLabel("???", 18, new Color(0.4f, 0.4f, 0.4f)));
            }

            _comboList.AddChild(entry);
            _comboList.AddChild(new HSeparator());
        }
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
