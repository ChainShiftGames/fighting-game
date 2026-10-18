using FightingGame.VfxLab.Files;
using FightingGame.VfxLab.View;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>
/// One variant in the grid: its live view, name, round and pitch, and the dev's verdict on it (pick, stars, hide,
/// note), which goes straight into the feedback file. Clicking the view selects it for the rack.
/// </summary>
public partial class LabVariantCell : PanelContainer
{
    // Developer settings (presentation): the selected cell's border and the views' shape.
    private static readonly Color _selectedColor = new(0.72f, 0.48f, 1f);
    private static readonly Color _panelColor = new(0.09f, 0.085f, 0.11f);
    private const float ViewRatio = 16f / 9f;

    private readonly StyleBoxFlat _style = new() { BgColor = _panelColor, ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 8 };
    private readonly ColorRect _greyscale = new()
    {
        MouseFilter = MouseFilterEnum.Ignore,
        Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://VfxLab/View/lab_greyscale.gdshader") },
    };

    public LabVariantView View { get; } = new();

    public string VariantId { get; private set; } = string.Empty;

    /// <summary>Builds the cell; call once, before it joins the scene.</summary>
    public void Build(LabVariant variant, LabSession session, LabStagePicture? picture, LabCellActions actions, bool solo)
    {
        VariantId = variant.Id;
        AddThemeStyleboxOverride("panel", _style);
        View.Build(variant, session.Sheet.Ranges, picture);
        VariantFeedback feedback = session.Feedback.For(variant.Id);

        VBoxContainer column = new();
        HBoxContainer header = new();
        Label name = new() { Text = variant.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true, TooltipText = variant.Id };
        name.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(name);
        Label round = new()
        {
            Text = session.RoundNameOf(variant.Id),
            Modulate = new Color(1f, 1f, 1f, 0.5f),
            ClipText = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.6f,
            TooltipText = session.RoundNameOf(variant.Id),
        };
        round.AddThemeFontSizeOverride("font_size", 12);
        header.AddChild(round);
        column.AddChild(header);

        // The view's height follows its width; a container that keeps a ratio doesn't reserve the height in a column.
        MarginContainer layers = new() { MouseFilter = MouseFilterEnum.Stop, ClipContents = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        layers.Resized += () => layers.CustomMinimumSize = new Vector2(0f, Mathf.Floor(layers.Size.X / ViewRatio));
        layers.AddChild(View);
        layers.AddChild(_greyscale);
        layers.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
            {
                actions.Select(VariantId);
                if (click.DoubleClick)
                {
                    actions.Solo(VariantId);
                }
            }
        };
        column.AddChild(layers);

        if (View.Missing.Count > 0)
        {
            column.AddChild(new Label { Text = "Missing shader: " + string.Join("; ", View.Missing), Modulate = new Color(1f, 0.45f, 0.45f), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        column.AddChild(new Label { Text = variant.Pitch, AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1f, 1f, 1f, 0.7f), CustomMinimumSize = new Vector2(200f, 0f) });
        column.AddChild(Verdict(feedback, actions, solo));

        LineEdit note = new() { Text = feedback.Note, PlaceholderText = "Note for Claude…" };
        note.TextChanged += text =>
        {
            feedback.Note = text;
            actions.FeedbackChanged();
        };
        column.AddChild(note);
        AddChild(column);
    }

    public void SetSelected(bool selected)
    {
        int width = selected ? 2 : 0;
        _style.BorderColor = _selectedColor;
        _style.SetBorderWidthAll(width);
    }

    public void SetGreyscale(bool on) => _greyscale.Visible = on;

    private HFlowContainer Verdict(VariantFeedback feedback, LabCellActions actions, bool solo)
    {
        HFlowContainer row = new();
        Button pick = new() { Text = "★ Pick", ToggleMode = true, ButtonPressed = feedback.Pick, TooltipText = "One you want to keep going with" };
        pick.Toggled += on =>
        {
            feedback.Pick = on;
            actions.FeedbackChanged();
        };
        OptionButton rating = new() { TooltipText = "How much you like it" };
        rating.AddItem("No stars");
        for (int stars = 1; stars <= 5; stars++)
        {
            rating.AddItem(new string('★', stars));
        }

        rating.Selected = feedback.Rating;
        rating.ItemSelected += index =>
        {
            feedback.Rating = (int)index;
            actions.FeedbackChanged();
        };
        Button hide = new() { Text = "Reject", ToggleMode = true, ButtonPressed = feedback.Hidden, TooltipText = "Hide it from the grid (Show rejected brings it back)" };
        hide.Toggled += on =>
        {
            feedback.Hidden = on;
            actions.FeedbackChanged();
        };
        Button fork = new() { Text = "Fork", TooltipText = "Save it with your changes as a new variant" };
        fork.Pressed += () => actions.Fork(VariantId);
        Button alone = new() { Text = solo ? "Back to grid" : "Solo", TooltipText = "Show it alone, big (or double-click the view)" };
        alone.Pressed += () => actions.Solo(VariantId);
        row.AddChild(pick);
        row.AddChild(rating);
        row.AddChild(hide);
        row.AddChild(fork);
        row.AddChild(alone);
        return row;
    }
}
