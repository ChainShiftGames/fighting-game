using System;
using Godot;

namespace FightingGame.Debug;

/// <summary>
/// Debug panel button that steps through the models each art file lists (developer setting, not saved): the main
/// model, then its <c>otherModels</c>, so they can be compared on the same moves.
/// </summary>
public partial class FighterModelSwitch : VBoxContainer
{
    private readonly Button _button = new() { Text = "Switch fighter model", FocusMode = FocusModeEnum.None };
    private readonly Label _shown = new();

    /// <summary>Raised with the model to show now; 0 is the art file's main model.</summary>
    public event Action<int>? Switched;

    /// <summary>The model to show: 0 is the art file's main model, 1 on its other models.</summary>
    public int Variant { get; private set; }

    public override void _Ready()
    {
        _button.Pressed += () => Switched?.Invoke(++Variant);
        AddChild(_button);
        AddChild(_shown);
    }

    /// <summary>Names the model now on screen.</summary>
    public void ShowName(string model) => _shown.Text = $"Showing: {model}";
}
