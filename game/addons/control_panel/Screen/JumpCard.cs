#if TOOLS
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using Godot;
using Sim.Math;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// A card of facts about a character's jumps, laid out like the Character tab's move facts: a soft label with a hint
/// under it, and the value in the mono font on the right.
/// </summary>
[Tool]
public partial class JumpCard : PanelContainer
{
    private const int Facts = 7;

    private readonly Label _note = new() { ThemeTypeVariation = MapTheme.MutedLabel, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Label[] _values = new Label[Facts];

    private static readonly (string Label, string Hint)[] _facts =
    [
        ("Jump air time", "frames from leaving the ground to landing"),
        ("Jump height", "highest point"),
        ("Jump distance", "forward jump, with the jump forward speed"),
        ("High jump air time", "28 / dash up"),
        ("High jump height", "highest point"),
        ("High jump distance", "forward high jump"),
        ("Prejump", "grounded frames before either leaves the ground"),
    ];

    public JumpCard()
    {
        ThemeTypeVariation = MapTheme.Card;
        TooltipText = "Worked out from the stats the way the game moves fighters, so it follows every edit at once.";
        var column = new VBoxContainer();
        column.AddChild(new Label { Text = "Jumps", ThemeTypeVariation = MapTheme.Title });
        _note.Text = "As the game flies them, before the match restarts.";
        column.AddChild(_note);
        column.AddChild(new HSeparator());
        var grid = new GridContainer { Columns = 2 };
        for (int i = 0; i < Facts; i++)
        {
            var name = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            name.AddThemeConstantOverride("separation", 0);
            name.AddChild(new Label { Text = _facts[i].Label, ThemeTypeVariation = MapTheme.FactLabel });
            name.AddChild(new Label { Text = _facts[i].Hint, ThemeTypeVariation = MapTheme.FactHint });
            _values[i] = new Label
            {
                ThemeTypeVariation = MapTheme.FactValue,
                HorizontalAlignment = HorizontalAlignment.Right,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            grid.AddChild(name);
            grid.AddChild(_values[i]);
        }

        column.AddChild(grid);
        AddChild(column);
    }

    public void Show(JumpArc jump, JumpArc high, int forwardSpeed, int prejumpFrames)
    {
        _note.Text = "As the game flies them, before the match restarts.";
        Fill(0, jump, forwardSpeed);
        Fill(3, high, forwardSpeed);
        _values[6].Text = $"{prejumpFrames} f";
    }

    /// <summary>Shows why there are no facts, e.g. a stat the file doesn't hold.</summary>
    public void ShowProblem(string problem)
    {
        _note.Text = problem;
        foreach (Label value in _values)
        {
            value.Text = "—";
        }
    }

    private void Fill(int first, JumpArc arc, int forwardSpeed)
    {
        if (arc.AirFrames == 0)
        {
            _values[first].Text = "never lands";
            _values[first + 1].Text = $"{Pixels(arc.ApexHeight)} px+";
            _values[first + 2].Text = "—";
            return;
        }

        _values[first].Text = $"{arc.AirFrames} f";
        _values[first + 1].Text = $"{Pixels(arc.ApexHeight)} px";
        _values[first + 2].Text = $"{Pixels((long)forwardSpeed * arc.AirFrames)} px";
    }

    private static string Pixels(long subpixels) =>
        (subpixels / (double)Subpixels.PerPixel).ToString("0", CultureInfo.InvariantCulture);
}
#endif
