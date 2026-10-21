#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// The drawer's first tab: the move in plain facts, what kind of move it is, how to do it, how it's blocked, and
/// every other setting of its file through the schema form (hits, projectiles in full, velocity, flags, cost…).
/// </summary>
[Tool]
public partial class OverviewPage : VBoxContainer, IDrawerPage
{
    // Edited on this page's own controls or on another tab, so the settings form leaves them out.
    private static readonly HashSet<string> _elsewhere =
    [
        MoveFields.Schema, MoveFields.SchemaVersion, MoveFields.Id, MoveFields.Category, MoveFields.Duration,
        MoveFields.FrameData, MoveFields.Input, MoveFields.Cancels, MoveFields.Boxes,
    ];

    // Not for basic moves; shown for one only when the file has them, so they can be removed.
    private static readonly HashSet<string> _attackOnly =
        [MoveFields.UsableIn, MoveFields.Hits, MoveFields.Throw, MoveFields.Projectiles];

    private static readonly Dictionary<string, string> _categoryNames = new(StringComparer.Ordinal)
    {
        [MoveFields.BasicCategory] = "Plays on its own (movement, reactions)",
        ["normal"] = "Normal attack",
        ["special"] = "Special move",
        ["super"] = "Super",
        ["throw"] = "Throw",
    };

    private readonly FactList _facts = new();
    private readonly OptionButton _category = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly SectionTitle _inputTitle = SectionTitle.Of("How to do it", "numpad notation · buttons");
    private readonly MoveInputPanel _input = new();
    private readonly GuardPanel _guards = new();
    private readonly SchemaForm _settings = new();
    private readonly List<string> _choices = [];
    private DrawerMove? _move;

    public OverviewPage()
    {
        AddThemeConstantOverride("separation", 10);
        AddChild(_facts);
        AddChild(SectionTitle.Of("Kind of move"));
        AddChild(_category);
        AddChild(_inputTitle);
        AddChild(_input);
        AddChild(_guards);
        AddChild(SectionTitle.Of("All settings", "from the move's file"));
        AddChild(_settings);
        _category.ItemSelected += index => ChangeCategory(_choices[(int)index]);
        _input.Changed += () => Changed?.Invoke(false);
        _settings.Changed += () =>
        {
            _guards.ShowMove(_move!);
            Changed?.Invoke(false);
        };

        // The settings form shows the guards too: it's rebuilt once the picker's signal has finished.
        _guards.Changed += () => Callable.From(() => Changed?.Invoke(true)).CallDeferred();
    }

    public event Action<bool>? Changed;

    public void ShowMove(DrawerMove move)
    {
        _move = move;
        ShowFacts(move);
        ShowCategory(move);
        _inputTitle.Visible = _input.Visible = !move.IsBasic;
        _input.ShowMove(move.Root, move.Context.ButtonIds, move.Context.MoveSchema.FieldNames);
        _guards.ShowMove(move);
        _settings.Show(move.Root, move.Context.MoveSchema, field => !_elsewhere.Contains(field)
            && (!move.IsBasic || !_attackOnly.Contains(field) || move.Root.ContainsKey(field)));
    }

    /// <summary>Works the facts out again after any edit, without rebuilding the form being typed in.</summary>
    public void ShowFacts(DrawerMove move) => _facts.ShowFacts(DrawerFacts.Of(move));

    private void ShowCategory(DrawerMove move)
    {
        _category.Clear();
        _choices.Clear();
        _choices.AddRange(move.Context.MoveSchema.Field(MoveFields.Category)?.Choices ?? []);
        if (!_choices.Contains(move.Category))
        {
            _choices.Add(move.Category);
        }

        foreach (string choice in _choices)
        {
            _category.AddItem(_categoryNames.GetValueOrDefault(choice, choice));
        }

        _category.Select(_choices.IndexOf(move.Category));
    }

    private void ChangeCategory(string category)
    {
        DrawerMove move = _move!;
        MoveCategory.Set(move.Root, category, move.Context.MoveSchema, move.Context.FirstButton);
        // Basic moves and attacks show different fields, and other tabs follow; the signal is still running.
        Callable.From(() => Changed?.Invoke(true)).CallDeferred();
    }
}
#endif
