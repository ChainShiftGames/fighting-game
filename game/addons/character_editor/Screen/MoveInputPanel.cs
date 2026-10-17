#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// An attack's input: its motion in numpad notation and the buttons of the button set it is pressed with. Ticking
/// two or more makes a press of buttons together (like a throw).
/// </summary>
[Tool]
public partial class MoveInputPanel : HBoxContainer
{
    private readonly LineEdit _motion = new()
    {
        CustomMinimumSize = new Vector2(90f, 0f),
        TooltipText = "Numpad notation: 5 none, 2 down, 236 quarter circle forward",
    };

    private readonly HFlowContainer _buttons = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private JsonObject _move = [];
    private IReadOnlyList<string> _fieldOrder = [];
    private List<string> _pressed = [];

    public MoveInputPanel()
    {
        AddChild(new Label { Text = "Motion" });
        AddChild(_motion);
        AddChild(new Label { Text = "Buttons" });
        AddChild(_buttons);
        _motion.TextChanged += motion =>
        {
            MoveInput.SetMotion(_move, motion, _fieldOrder);
            Changed?.Invoke();
        };
    }

    public event Action? Changed;

    /// <param name="buttonIds">The button set's buttons, in the order the UI lists them.</param>
    /// <param name="fieldOrder">The move's field names in schema order.</param>
    public void ShowMove(JsonObject move, IReadOnlyList<string> buttonIds, IReadOnlyList<string> fieldOrder)
    {
        _move = move;
        _fieldOrder = fieldOrder;
        _motion.Text = MoveInput.Motion(move);
        _pressed = [.. MoveInput.Buttons(move)];
        foreach (Node child in _buttons.GetChildren())
        {
            _buttons.RemoveChild(child);
            child.QueueFree();
        }

        // Buttons the set doesn't have are listed too, so they can be unticked (validation reports them).
        List<string> shown = [.. buttonIds];
        foreach (string button in _pressed)
        {
            if (!shown.Contains(button))
            {
                shown.Add(button);
            }
        }

        foreach (string button in shown)
        {
            var check = new CheckBox { Text = button, ButtonPressed = _pressed.Contains(button) };
            check.Toggled += on => Toggle(button, on);
            _buttons.AddChild(check);
        }
    }

    private void Toggle(string button, bool on)
    {
        _pressed.Remove(button);
        if (on)
        {
            _pressed.Add(button);
        }

        MoveInput.SetButtons(_move, _pressed, _fieldOrder);
        Changed?.Invoke();
    }
}
#endif
