#if TOOLS
using System;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// Where the move hits and can be hit, frame by frame: the box editor (<see cref="BoxPanel"/>), drawing on the
/// drawer's preview, which is editable only while this tab is open, and where its hit sparks start
/// (<see cref="SparkPanel"/>).
/// </summary>
[Tool]
public partial class HitAreasPage : VBoxContainer, IDrawerPage
{
    private readonly BoxPanel _boxes = new();
    private readonly SparkPanel _sparks = new();

    public HitAreasPage()
    {
        AddThemeConstantOverride("separation", 14);
        AddChild(_boxes);
        AddChild(_sparks);
        _sparks.UseCanvas(_boxes.Canvas);
        _boxes.Changed += () => Changed?.Invoke(false);
        _sparks.Changed += () => SparksChanged?.Invoke();
    }

    public event Action<bool>? Changed;

    /// <summary>The spark settings changed: the art file, not the move's data file.</summary>
    public event Action? SparksChanged;

    /// <summary>The frame the preview shows changed.</summary>
    public event Action<int>? FrameShown
    {
        add => _boxes.FrameShown += value;
        remove => _boxes.FrameShown -= value;
    }

    public BoxCanvas Canvas => _boxes.Canvas;

    public int Frame => _boxes.Frame;

    public void ShowCharacter(string? characterId, System.Collections.Generic.IReadOnlyList<string> moveIds) =>
        _boxes.ShowCharacter(characterId, moveIds);

    public void ShowMove(DrawerMove move)
    {
        _boxes.ShowMove(move.Root, move.Context.MoveSchema);
        _sparks.ShowMove(move);
    }

    public void SelectFrame(int frame) => _boxes.SelectFrame(frame);
}
#endif
