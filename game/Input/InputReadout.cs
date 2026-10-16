using System;
using System.Text;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Developer overlay text: each local player's raw input word next to the word the logic sees after cleaning, and
/// which joypad fills which slot. Lets a human check SOCD (raw L R → logic -) and rebinding on real controllers.
/// </summary>
public sealed class InputReadout
{
    private readonly ButtonSet? _buttons;
    private readonly InputStep? _step;
    private readonly JoypadRoster _roster;

    public InputReadout(ButtonSet? buttons, JoypadRoster roster)
    {
        _buttons = buttons;
        _step = buttons is null ? null : new InputStep(buttons);
        _roster = roster;
    }

    public string Describe(ReadOnlySpan<InputWord> rawWords)
    {
        var text = new StringBuilder();
        for (int player = 0; player < rawWords.Length; player++)
        {
            InputWord raw = rawWords[player];

            // The logic's own cleaning step, so the readout can't disagree with what the match uses.
            InputWord clean = _step?.Clean(raw) ?? SocdResolver.Resolve(raw);
            text.Append($"local player {player + 1}  raw: {InputWordText.Describe(raw, _buttons)}  ");
            text.AppendLine($"logic: {InputWordText.Describe(clean, _buttons)}");
        }

        if (_roster.Connected.Count == 0)
        {
            text.AppendLine("no joypads connected");
        }

        foreach (JoypadInfo joypad in _roster.Connected)
        {
            text.AppendLine($"joypad {joypad.Device}: {JoypadText.Describe(joypad, _roster.SlotOf(joypad.Device))}");
        }

        return text.ToString().TrimEnd();
    }
}
