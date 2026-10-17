#if TOOLS
using System.Collections.Generic;
using Godot;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>The problems Sim.Data found, as <c>file: field: reason</c>, the way the game reports them.</summary>
[Tool]
public partial class ProblemList : VBoxContainer
{
    private readonly Label _title = new();
    private readonly ItemList _problems = new() { CustomMinimumSize = new Vector2(0f, 120f) };

    public ProblemList()
    {
        AddChild(_title);
        AddChild(_problems);
        ShowProblems([]);
    }

    public void ShowProblems(IReadOnlyList<DataError> problems)
    {
        _title.Text = problems.Count == 0 ? "No problems" : $"Problems ({problems.Count})";
        _problems.Clear();
        foreach (DataError problem in problems)
        {
            _problems.AddItem(problem.ToString());
        }
    }
}
#endif
