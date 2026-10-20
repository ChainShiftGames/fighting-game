#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// A move's startup, active and recovery frames (its duration for a basic move), and whether changing them moves
/// boxes, cancel windows and other frame ranges along with their phase (<see cref="MoveFrameData"/>).
/// </summary>
[Tool]
public partial class FrameDataPanel : VBoxContainer
{
    private readonly HBoxContainer _fields = new();
    private readonly Label _total = new();
    private readonly CheckBox _moveRanges = new()
    {
        Text = "Move boxes and cancels with their phase",
        ButtonPressed = true,
        TooltipText = "Off: only the frame data changes; ranges keep their frame numbers",
    };

    private JsonObject _move = [];
    private IReadOnlyList<string> _fieldOrder = [];
    private bool _isBasic;
    private SpinBox[] _spins = [];

    public FrameDataPanel()
    {
        AddChild(_fields);
        AddChild(_moveRanges);
    }

    /// <summary>The frame data changed; ranges in the move may have moved with it.</summary>
    public event Action? FramesChanged;

    /// <param name="fieldOrder">The move's field names in schema order.</param>
    public void ShowMove(JsonObject move, IReadOnlyList<string> fieldOrder)
    {
        _move = move;
        _fieldOrder = fieldOrder;
        // The total label is kept: freeing it with the spin boxes made every later move fail to show.
        foreach (Node child in _fields.GetChildren())
        {
            _fields.RemoveChild(child);
            if (child != _total)
            {
                child.QueueFree();
            }
        }

        _fields.AddChild(_total);
        FramePhases? phases = FramePhases.Of(move);
        if (phases is null)
        {
            _spins = [];
            _total.Text = "Frame data can't be read: fix it in the file (see the problems below).";
            return;
        }

        _isBasic = phases.IsBasic;
        _spins = phases.IsBasic
            ? [Spin("Duration", phases.Duration, 1)]
            : [Spin("Startup", phases.Startup, 1), Spin("Active", phases.Active, 1), Spin("Recovery", phases.Recovery, 0)];
        ShowTotal(phases);
    }

    private SpinBox Spin(string label, int value, int minimum)
    {
        _fields.AddChild(new Label { Text = label });
        var spin = new SpinBox
        {
            MinValue = minimum,
            MaxValue = CharacterSchema.MaxMoveFrames,
            Step = 1,
            Rounded = true,
            Value = value,
        };
        spin.ValueChanged += _ => Apply();
        _fields.AddChild(spin);
        return spin;
    }

    private void Apply()
    {
        FramePhases phases = _isBasic
            ? FramePhases.Basic((int)_spins[0].Value)
            : FramePhases.Attack((int)_spins[0].Value, (int)_spins[1].Value, (int)_spins[2].Value);
        MoveFrameData.Set(_move, phases, _moveRanges.ButtonPressed, _fieldOrder);
        ShowTotal(phases);
        FramesChanged?.Invoke();
    }

    private void ShowTotal(FramePhases phases) => _total.Text = $"{phases.TotalFrames} frames:";
}
#endif
