#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Presentation.World.Sparks;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// How each hit's spark is drawn (developer settings, the art file's <c>sparks</c>): where it starts relative to the
/// contact point, its direction, burst or tear, and its size. The preview marks where the picked hit's spark starts,
/// and dragging the mark moves it; edits go into the art file, written on Save.
/// </summary>
[Tool]
public partial class SparkPanel : VBoxContainer
{
    private const int StyleFromHit = 0;
    private const int PercentPerScale = 100;

    // Developer settings (editor): how far the steppers reach.
    private const int MaxOffsetPixels = 400;
    private const int MaxScalePercent = 400;
    private const int MinScalePercent = 10;

    private readonly OptionButton _hit = new() { TooltipText = "Which of the move's hits to edit" };
    private readonly HBoxContainer _controls = new();
    private readonly NumberStepper _forward = new() { Minimum = -MaxOffsetPixels, Maximum = MaxOffsetPixels };
    private readonly NumberStepper _up = new() { Minimum = -MaxOffsetPixels, Maximum = MaxOffsetPixels };
    private readonly NumberStepper _angle = new() { Minimum = -180, Maximum = 180, Step = 5 };
    private readonly NumberStepper _scale = new() { Minimum = MinScalePercent, Maximum = MaxScalePercent, Step = 10 };
    private readonly CheckBox _setAngle = new()
    {
        Text = "Set direction",
        TooltipText = "Off: the spark points the way the hit launches, or forward",
    };

    private readonly OptionButton _style = new() { TooltipText = "From hit: light hits burst, heavy hits and specials tear" };
    private readonly Label _note = new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = MapTheme.MutedLabel,
    };

    private BoxCanvas? _canvas;
    private DrawerMove? _move;
    private List<HitSparkSettings> _sparks = [];

    public SparkPanel()
    {
        AddThemeConstantOverride("separation", 6);
        AddChild(SectionTitle.Of("Hit spark", "art file · the star on the preview"));
        AddChild(_hit);
        foreach (NumberStepper stepper in new[] { _forward, _up, _scale })
        {
            _controls.AddChild(stepper);
        }

        AddChild(_controls);
        var direction = new HBoxContainer();
        direction.AddThemeConstantOverride("separation", 8);
        _setAngle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _style.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        direction.AddChild(_setAngle);
        direction.AddChild(_angle);
        direction.AddChild(_style);
        AddChild(direction);
        AddChild(_note);
        foreach (string style in new[] { "From hit", "Burst", "Tear" })
        {
            _style.AddItem(style);
        }

        _hit.ItemSelected += _ => ShowHit();
        _forward.ValueChanged += value => Edit(Current with { OffsetX = value });
        _up.ValueChanged += value => Edit(Current with { OffsetY = value });
        _scale.ValueChanged += value => Edit(Current with { Scale = value / (float)PercentPerScale });
        _angle.ValueChanged += value => Edit(Current with { Angle = value });
        _setAngle.Toggled += on => Edit(Current with { Angle = on ? 0f : null });
        _style.ItemSelected += index => Edit(Current with
        {
            Style = index == StyleFromHit ? null : (HitSparkStyle)(index - 1),
        });
    }

    /// <summary>The spark settings changed in the art file (not yet saved).</summary>
    public event Action? Changed;

    /// <summary>The preview the marker is drawn on.</summary>
    public void UseCanvas(BoxCanvas canvas)
    {
        _canvas = canvas;
        canvas.SparkMoved += (forward, up) =>
        {
            if (_sparks.Count > 0)
            {
                Edit(Current with { OffsetX = forward, OffsetY = up });
            }
        };
    }

    public void ShowMove(DrawerMove move)
    {
        _move = move;
        int hits = (move.Root[MoveFields.Hits] as JsonArray)?.Count ?? 0;
        string? cantEdit = move.Art switch
        {
            null => "No art file: the character is drawn as a placeholder, so sparks use the defaults.",
            { Problem.Length: > 0 } art => $"The art file can't be read, so sparks can't be edited: {art.Problem}",
            _ when hits == 0 => "The move has no hits, so it makes no spark.",
            _ => null,
        };
        _sparks = cantEdit is null ? [.. move.Art!.Sparks(move.Id)] : [];
        while (_sparks.Count < hits)
        {
            _sparks.Add(new HitSparkSettings());
        }

        _hit.Clear();
        for (int i = 0; i < hits; i++)
        {
            _hit.AddItem(string.Create(CultureInfo.InvariantCulture, $"Hit {i + 1} of {hits}"));
        }

        _hit.Visible = cantEdit is null && hits > 1;
        _controls.Visible = cantEdit is null;
        _setAngle.GetParent<Control>().Visible = cantEdit is null;
        _note.Text = cantEdit ?? "Drag the star on the preview, or set the offsets: pixels from where the hit box meets the " +
            "opponent, forward along the attacker's facing.";
        ShowHit();
    }

    private int HitIndex => Math.Max(0, _hit.Selected);

    private HitSparkSettings Current => _sparks[HitIndex];

    private void ShowHit()
    {
        if (_sparks.Count == 0)
        {
            ShowMarker(null);
            return;
        }

        HitSparkSettings spark = Current;
        _forward.Show("Forward", "px", (int)MathF.Round(spark.OffsetX));
        _up.Show("Up", "px", (int)MathF.Round(spark.OffsetY));
        _scale.Show("Size", "%", (int)MathF.Round(spark.Scale * PercentPerScale));
        _setAngle.SetPressedNoSignal(spark.Angle is not null);
        _angle.Show("Direction", "degrees · 90 up", (int)MathF.Round(spark.Angle ?? 0f));
        _angle.Visible = spark.Angle is not null;
        _style.Select(spark.Style is HitSparkStyle style ? (int)style + 1 : StyleFromHit);
        ShowMarker(spark);
    }

    private void Edit(HitSparkSettings spark)
    {
        DrawerMove move = _move!;
        var sparks = new List<HitSparkSettings>(_sparks) { [HitIndex] = spark };
        string problem = move.Art!.SetSparks(move.Id, sparks);
        if (problem.Length > 0)
        {
            _note.Text = problem;
            return;
        }

        _sparks = sparks;
        ShowHit();
        Changed?.Invoke();
    }

    private void ShowMarker(HitSparkSettings? spark)
    {
        if (_canvas is not null)
        {
            _canvas.Spark = spark;
            _canvas.QueueRedraw();
        }
    }
}
#endif
