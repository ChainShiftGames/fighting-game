using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Turns the input names in a bindings file into input word bits: <c>up</c>, <c>down</c>, <c>left</c>,
/// <c>right</c>, and every button id in the button set (ADR-0010), so bindings never hard-code the buttons.
/// </summary>
/// <remarks>Without a button set (no data folder) only the directions exist.</remarks>
public sealed class BindingTargets
{
    private static readonly (string Name, ushort Mask)[] _directions =
    [
        ("up", InputWord.Up),
        ("down", InputWord.Down),
        ("left", InputWord.Left),
        ("right", InputWord.Right),
    ];

    private readonly ButtonSet? _buttons;

    public BindingTargets(ButtonSet? buttons)
    {
        _buttons = buttons;
    }

    public bool TryGetMask(string name, out ushort mask)
    {
        foreach ((string direction, ushort directionMask) in _directions)
        {
            if (direction == name)
            {
                mask = directionMask;
                return true;
            }
        }

        if (_buttons is not null && _buttons.TryFind(name, out int button))
        {
            mask = _buttons.MaskOf(button);
            return true;
        }

        mask = 0;
        return false;
    }

    /// <summary>The input a single input word bit is bound as; false for no input or several bits.</summary>
    public bool TryGetName(ushort mask, out string name)
    {
        foreach (string input in Names())
        {
            if (TryGetMask(input, out ushort inputMask) && inputMask == mask)
            {
                name = input;
                return true;
            }
        }

        name = string.Empty;
        return false;
    }

    public bool HasButtons => _buttons is not null;

    /// <summary>All valid names: the four directions, then the buttons in button set order.</summary>
    public string[] Names()
    {
        int count = _buttons?.Count ?? 0;
        string[] names = new string[_directions.Length + count];
        for (int direction = 0; direction < _directions.Length; direction++)
        {
            names[direction] = _directions[direction].Name;
        }

        for (int button = 0; button < count; button++)
        {
            names[_directions.Length + button] = _buttons!.IdOf(button);
        }

        return names;
    }

    /// <summary>All valid names, for error messages.</summary>
    public string Describe() => string.Join(", ", Names());
}
