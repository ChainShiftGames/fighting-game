namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>The part of a box being dragged: its middle moves it, an edge or corner resizes it.</summary>
public enum BoxHandle
{
    None,
    Move,
    Left,
    Right,
    Bottom,
    Top,
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight,
}
