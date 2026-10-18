using System;
using System.Collections.Generic;
using System.Linq;
using FightingGame.VfxLab.Files;
using FightingGame.VfxLab.View;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>
/// The tuning rack for the selected variant: a control for every uniform its shaders declare (by group), its parts'
/// clocks and cards, and the move's timing. Every change goes into the feedback file as the dev's value.
/// </summary>
public partial class LabRack : PanelContainer
{
    // Developer setting (presentation): the rack's width.
    private const float Width = 400f;

    private readonly VBoxContainer _rows = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private LabSession? _session;
    private LabVariant? _variant;
    private IReadOnlyList<LabEffectCard> _cards = [];
    private LabViewOptions _options = new();

    /// <summary>A variant's values changed (its id).</summary>
    public Action<string> ValuesChanged { get; set; } = _ => { };

    /// <summary>The move's timing changed; every view uses it.</summary>
    public Action TimingChanged { get; set; } = () => { };

    public Action<string> ForkAsked { get; set; } = _ => { };

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Width, 0f);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        MarginContainer margin = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddChild(_rows);
        scroll.AddChild(margin);
        AddChild(scroll);
    }

    /// <param name="cards">The selected variant's cards, whose shaders say which uniforms there are.</param>
    public void Show(LabSession session, LabVariant? variant, IReadOnlyList<LabEffectCard> cards, LabViewOptions options)
    {
        (_session, _variant, _cards, _options) = (session, variant, cards, options);
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in _rows.GetChildren())
        {
            child.QueueFree();
        }

        if (_session is null || _variant is null)
        {
            _rows.AddChild(new Label { Text = "Click a variant to tune it." });
            return;
        }

        AddHeading(_variant.Name, _session.RoundNameOf(_variant.Id), _variant.Pitch);
        AddButtons(_variant);
        Dictionary<string, LabValue> values = _session.ValuesFor(_variant, _options.PaletteLock);
        VariantFeedback feedback = _session.Feedback.For(_variant.Id);
        foreach (IGrouping<string, (UniformSpec Spec, string Parts)> group in MergedUniforms().GroupBy(u => u.Spec.Group))
        {
            VBoxContainer section = AddSection(group.Key.Length > 0 ? Readable(group.Key) : "Look");
            foreach ((UniformSpec spec, string parts) in group)
            {
                LabValue value = values.TryGetValue(spec.Name, out LabValue set) ? set : spec.Default;
                section.AddChild(UniformRow.Of(Readable(spec.Name), $"{spec.Name} ({parts})", spec, value, feedback.Values.ContainsKey(spec.Name), v => SetValue(spec.Name, v)));
            }
        }

        foreach (LabEffectCard card in _cards)
        {
            VBoxContainer section = AddSection($"{card.Kind} part", folded: true);
            section.AddChild(new Label { Text = card.Part.Shader, ClipText = true, TooltipText = card.Part.Shader, Modulate = new Color(1f, 1f, 1f, 0.6f) });
            foreach (LabPartSetting setting in LabPartSettings.All.Where(s => !s.FlightOnly || card.Kind == LabPartKind.Flight))
            {
                string key = setting.KeyFor(card.Kind);
                UniformSpec spec = SliderSpec(key, setting.Min, setting.Max, setting.Step);
                section.AddChild(UniformRow.Of(setting.Label, key, spec, LabValue.Number(card.Setting(setting)), feedback.Values.ContainsKey(key), v => SetValue(key, v)));
            }
        }

        VBoxContainer timing = AddSection("Move timing (every variant)", folded: true);
        foreach (LabTimingSetting setting in LabTimingSettings.All)
        {
            UniformSpec spec = SliderSpec(setting.Key, setting.Min, setting.Max, setting.Step);
            timing.AddChild(UniformRow.Of(setting.Label, setting.Key, spec, LabValue.Number(setting.Read(_session.Timing)), _session.Feedback.Timing.ContainsKey(setting.Key), v => SetTiming(setting.Key, v)));
        }

        VBoxContainer style = AddSection($"Style: {_session.Sheet.Style.Character}", folded: true);
        style.AddChild(Wrapped(_session.Sheet.Style.Motifs));
        style.AddChild(Wrapped(_options.PaletteLock ? "Palette locked: every variant uses the style's colours." : "Palette lock is off: variants may use their own colours."));
    }

    // Every part's uniforms, each name once (parts that share an include share its values), with the parts that have it.
    private IEnumerable<(UniformSpec Spec, string Parts)> MergedUniforms()
    {
        Dictionary<string, (UniformSpec Spec, List<string> Parts)> merged = new(StringComparer.Ordinal);
        List<string> order = [];
        foreach (LabEffectCard card in _cards.OrderBy(card => card.Kind == LabPartKind.Flight ? 0 : 1))
        {
            foreach (UniformSpec spec in card.Uniforms)
            {
                if (!merged.TryGetValue(spec.Name, out (UniformSpec Spec, List<string> Parts) entry))
                {
                    entry = (spec, []);
                    merged[spec.Name] = entry;
                    order.Add(spec.Name);
                }

                entry.Parts.Add(card.Kind.ToString().ToLowerInvariant());
            }
        }

        return order.Select(name => (merged[name].Spec, string.Join(", ", merged[name].Parts)));
    }

    private void SetValue(string key, LabValue? value)
    {
        Dictionary<string, LabValue> changes = _session!.Feedback.For(_variant!.Id).Values;
        if (value is { } set)
        {
            changes[key] = set;
        }
        else
        {
            changes.Remove(key);
        }

        ValuesChanged(_variant.Id);
        if (value is null)
        {
            Rebuild();
        }
    }

    private void SetTiming(string key, LabValue? value)
    {
        if (value is { } set)
        {
            _session!.Feedback.Timing[key] = set.First;
        }
        else
        {
            _session!.Feedback.Timing.Remove(key);
        }

        TimingChanged();
        if (value is null)
        {
            Rebuild();
        }
    }

    private void AddHeading(string name, string round, string pitch)
    {
        Label title = new() { Text = name, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        title.AddThemeFontSizeOverride("font_size", 20);
        _rows.AddChild(title);
        _rows.AddChild(new Label { Text = round.ToUpperInvariant(), Modulate = new Color(1f, 1f, 1f, 0.55f) });
        _rows.AddChild(Wrapped(pitch));
    }

    private void AddButtons(LabVariant variant)
    {
        HFlowContainer buttons = new();
        Button reset = new() { Text = "Reset all", TooltipText = "Forget every change to this variant" };
        reset.Pressed += () =>
        {
            _session!.Feedback.For(variant.Id).Values.Clear();
            ValuesChanged(variant.Id);
            Rebuild();
        };
        Button copy = new() { Text = "Copy settings", TooltipText = "Its values as JSON, to paste in the chat" };
        copy.Pressed += () => DisplayServer.ClipboardSet(_session!.SettingsText(variant));
        Button fork = new() { Text = "Fork", TooltipText = "Save it with your changes as a new variant to drill down on" };
        fork.Pressed += () => ForkAsked(variant.Id);
        buttons.AddChild(reset);
        buttons.AddChild(copy);
        buttons.AddChild(fork);
        _rows.AddChild(buttons);
    }

    private VBoxContainer AddSection(string title, bool folded = false)
    {
        FoldableContainer fold = new() { Title = title, Folded = folded };
        VBoxContainer body = new();
        fold.AddChild(body);
        _rows.AddChild(fold);
        return body;
    }

    private static Label Wrapped(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        Modulate = new Color(1f, 1f, 1f, 0.75f),
        CustomMinimumSize = new Vector2(Width - 40f, 0f),
    };

    private static UniformSpec SliderSpec(string name, float min, float max, float step) => new(
        name, string.Empty, step >= 1f ? UniformControlKind.WholeSlider : UniformControlKind.Slider, min, max, step, LabValue.Number(min), false);

    // glow_color → Glow color.
    private static string Readable(string name)
    {
        string spaced = name.Replace('_', ' ');
        return spaced.Length == 0 ? spaced : char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }
}
