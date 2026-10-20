#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.CharacterEditor.Screen.Map;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>A move's phases as one bar, each as long as its frames, in the same colours as the map's cards.</summary>
[Tool]
public partial class PhaseBar : Control
{
    private const float Height = 10f;
    private IReadOnlyList<int> _phases = [];

    public void ShowPhases(IReadOnlyList<int> phases)
    {
        _phases = phases;
        QueueRedraw();
    }

    public override void _Ready() => CustomMinimumSize = new Vector2(0f, MapLook.Of(this).Px(Height));

    public override void _Draw() =>
        new MapPainter(this, MapLook.Of(this), Vector2.Zero).Bar(new Rect2(Vector2.Zero, Size), _phases);
}
#endif
