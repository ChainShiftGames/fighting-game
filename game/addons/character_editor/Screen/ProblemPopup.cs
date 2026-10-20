#if TOOLS
using System.Collections.Generic;
using Godot;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>The problems Sim.Data found, dropped down under the status chip (<see cref="ProblemList"/>).</summary>
[Tool]
public partial class ProblemPopup : PopupPanel
{
    private const float Width = 560f;
    private const float Height = 260f;
    private readonly ProblemList _problems = new();

    public ProblemPopup()
    {
        AddChild(_problems);
    }

    public void ShowProblems(IReadOnlyList<DataError> problems) => _problems.ShowProblems(problems);

    /// <summary>Opens under <paramref name="anchor"/>, right edges lined up.</summary>
    public void OpenUnder(Control anchor, float scale)
    {
        Rect2 at = anchor.GetGlobalRect();
        Vector2 origin = anchor.GetScreenTransform().Origin - anchor.GetGlobalTransform().Origin;
        var size = new Vector2I((int)(Width * scale), (int)(Height * scale));
        Vector2 position = origin + new Vector2(at.End.X - size.X, at.End.Y + (6f * scale));
        Popup(new Rect2I((Vector2I)position, size));
    }
}
#endif
