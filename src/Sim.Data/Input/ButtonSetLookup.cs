using System.Collections.Generic;
using Sim.Input;

namespace Sim.Data.Input;

/// <summary>Resolves button ids in character data against the loaded <see cref="ButtonSet"/>.</summary>
public sealed class ButtonSetLookup : IButtonLookup
{
    private readonly ButtonSet _buttons;

    public ButtonSetLookup(ButtonSet buttons)
    {
        _buttons = buttons;
        string[] ids = new string[buttons.Count];
        for (int button = 0; button < buttons.Count; button++)
        {
            ids[button] = buttons.IdOf(button);
        }

        ButtonIds = ids;
    }

    public IReadOnlyList<string> ButtonIds { get; }

    public bool TryGetMask(string buttonId, out int mask)
    {
        if (_buttons.TryFind(buttonId, out int button))
        {
            mask = _buttons.MaskOf(button);
            return true;
        }

        mask = 0;
        return false;
    }
}
