#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// How the move is blocked: one picker per hit and per projectile (<see cref="MoveGuards"/>), with the guards the
/// move schema allows. Hidden for a move without hits.
/// </summary>
[Tool]
public partial class GuardPanel : VBoxContainer
{
    private const string GuardField = "guard";

    private static readonly Dictionary<string, string> _names = new(StringComparer.Ordinal)
    {
        ["mid"] = "Mid: block standing or crouching",
        ["low"] = "Low: block crouching",
        ["overhead"] = "Overhead: block standing",
        ["unblockable"] = "Unblockable",
    };

    private readonly SectionTitle _title = SectionTitle.Of("How it's blocked", "per hit");
    private readonly VBoxContainer _rows = new();

    public GuardPanel()
    {
        AddThemeConstantOverride("separation", 10);
        _rows.AddThemeConstantOverride("separation", 6);
        AddChild(_title);
        AddChild(_rows);
    }

    public event Action? Changed;

    public void ShowMove(DrawerMove move)
    {
        foreach (Node child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }

        IReadOnlyList<MoveGuards.Slot> slots = MoveGuards.Of(move.Root);
        IReadOnlyList<string> choices =
            move.Context.MoveSchema.Field(MoveFields.Hits)?.Items?.Field(GuardField)?.Choices ?? [];
        Visible = slots.Count > 0;
        foreach (MoveGuards.Slot slot in slots)
        {
            _rows.AddChild(Row(slot, choices));
        }
    }

    private HBoxContainer Row(MoveGuards.Slot slot, IReadOnlyList<string> schemaChoices)
    {
        List<string> choices = [.. schemaChoices];
        if (slot.Guard is string current && !choices.Contains(current))
        {
            choices.Add(current);
        }

        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (string choice in choices)
        {
            picker.AddItem(_names.GetValueOrDefault(choice, choice));
        }

        picker.Select(slot.Guard is string guard ? choices.IndexOf(guard) : -1);
        picker.ItemSelected += index =>
        {
            MoveGuards.Set(slot, choices[(int)index]);
            Changed?.Invoke();
        };

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(new Label
        {
            Text = slot.Label,
            ThemeTypeVariation = MapTheme.FactLabel,
            CustomMinimumSize = new Vector2(96f, 0f),
        });
        row.AddChild(picker);
        return row;
    }
}
#endif
