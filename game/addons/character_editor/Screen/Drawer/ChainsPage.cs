#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// What the move can be interrupted into, as chips of plain names, and its chain (cancel) windows: when, on what
/// (hit, block, a miss) and into which moves (<see cref="CancelPanel"/>).
/// </summary>
[Tool]
public partial class ChainsPage : VBoxContainer, IDrawerPage
{
    private static readonly Dictionary<string, string> _categoryNames = new(StringComparer.Ordinal)
    {
        ["normal"] = "Any normal attack",
        ["special"] = "Any special move",
        ["super"] = "Any super",
        ["throw"] = "Any throw",
    };

    private readonly HFlowContainer _chips = new();
    private readonly CancelPanel _windows = new();
    private DrawerMove? _move;

    public ChainsPage()
    {
        AddThemeConstantOverride("separation", 10);
        AddChild(SectionTitle.Of("Can interrupt into"));
        AddChild(_chips);
        AddChild(SectionTitle.Of("Chain windows", "frames · on what · into what"));
        AddChild(_windows);
        _windows.Changed += () =>
        {
            ShowChips(_move!);
            Changed?.Invoke(false);
        };
    }

    public event Action<bool>? Changed;

    public void ShowMove(DrawerMove move)
    {
        _move = move;
        _windows.NameOf = move.NameOf;
        _windows.ShowMove(move.Root, move.Context.MoveSchema, move.CancelTargets);
        ShowChips(move);
    }

    private void ShowChips(DrawerMove move)
    {
        foreach (Node child in _chips.GetChildren())
        {
            _chips.RemoveChild(child);
            child.QueueFree();
        }

        IReadOnlyList<ChainLink> links = MoveChains.From(move.Id, move.Root);
        foreach (ChainLink link in links)
        {
            _chips.AddChild(Chip(link.Kind switch
            {
                ChainTargetKind.Move => move.NameOf(link.Target),
                ChainTargetKind.Category => _categoryNames.GetValueOrDefault(link.Target, link.Target),
                _ => "A jump",
            }));
        }

        if (links.Count == 0)
        {
            _chips.AddChild(new Label
            {
                Text = "Nothing yet: add a window below and tick the moves it leads to.",
                ThemeTypeVariation = MapTheme.MutedLabel,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
        }
    }

    private static Button Chip(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = MapTheme.Chip,
        FocusMode = FocusModeEnum.None,
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
#endif
