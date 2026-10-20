#if TOOLS
using System;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// Where the move hits and can be hit, frame by frame: the box editor (<see cref="BoxPanel"/>), drawing on the
/// drawer's preview, which is editable only while this tab is open.
/// </summary>
[Tool]
public partial class HitAreasPage : VBoxContainer, IDrawerPage
{
    private readonly BoxPanel _boxes = new();

    public HitAreasPage()
    {
        AddChild(_boxes);
        _boxes.Changed += () => Changed?.Invoke(false);
    }

    public event Action<bool>? Changed;

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

    public void ShowMove(DrawerMove move) => _boxes.ShowMove(move.Root, move.Context.MoveSchema);

    public void SelectFrame(int frame) => _boxes.SelectFrame(frame);
}
#endif
