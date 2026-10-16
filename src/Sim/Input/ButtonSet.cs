using System;

namespace Sim.Input;

/// <summary>The buttons of the input word and their bits, built from data (ADR-0010, developer setting).</summary>
/// <remarks>
/// Logic refers to a button by its index in this set, never by name or by assuming a count: moves, motions and
/// cancels resolve button names from data to an index once, at load time. Read-only during a match and not part of
/// the game state; it is part of the data hash.
/// </remarks>
public sealed class ButtonSet
{
    private readonly string[] _names;
    private readonly ushort[] _masks;

    /// <exception cref="ArgumentException">
    /// The set is empty, a name is empty or repeated, or a bit is repeated or outside the button bits.
    /// </exception>
    public ButtonSet(ReadOnlySpan<ButtonDefinition> buttons)
    {
        if (buttons.IsEmpty)
        {
            throw new ArgumentException("A button set needs at least one button.", nameof(buttons));
        }

        _names = new string[buttons.Length];
        _masks = new ushort[buttons.Length];
        for (int index = 0; index < buttons.Length; index++)
        {
            ButtonDefinition button = buttons[index];
            ushort mask = MaskFor(button, nameof(buttons));
            if (TryFind(button.Name, out _))
            {
                throw new ArgumentException($"Button '{button.Name}' is defined twice.", nameof(buttons));
            }

            if ((AllButtons & mask) != 0)
            {
                throw new ArgumentException(
                    $"Button '{button.Name}' uses bit {button.Bit}, which another button already uses.",
                    nameof(buttons));
            }

            _names[index] = button.Name;
            _masks[index] = mask;
            AllButtons |= mask;
        }
    }

    public int Count => _names.Length;

    /// <summary>The bits of every button in the set.</summary>
    public ushort AllButtons { get; }

    public string NameOf(int button) => _names[button];

    /// <summary>The input word bit of <paramref name="button"/>, as a mask.</summary>
    public ushort MaskOf(int button) => _masks[button];

    public bool IsHeld(InputWord input, int button) => input.IsHeld(_masks[button]);

    /// <summary>Finds a button by its name in data. For loading data, not for per-frame logic.</summary>
    public bool TryFind(string name, out int button)
    {
        // Linear and ordinal: sets are tiny, and the result must not depend on culture.
        for (button = 0; button < _names.Length; button++)
        {
            if (string.Equals(_names[button], name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        button = -1;
        return false;
    }

    private static ushort MaskFor(ButtonDefinition button, string paramName)
    {
        if (string.IsNullOrEmpty(button.Name))
        {
            throw new ArgumentException("A button needs a name.", paramName);
        }

        if (button.Bit < InputWord.FirstButtonBit || button.Bit >= InputWord.BitCount)
        {
            throw new ArgumentException(
                $"Button '{button.Name}' uses bit {button.Bit}; buttons use bits " +
                $"{InputWord.FirstButtonBit} to {InputWord.BitCount - 1}.",
                paramName);
        }

        return (ushort)(1 << button.Bit);
    }
}
