using Sim.Data.Hashing;
using Sim.Input;

namespace Sim.Data.Input;

/// <summary>Adds the button set to the data hash: ids and bits both decide what an input word means.</summary>
public static class ButtonSetDataHash
{
    public static void Append(DataHashBuilder hash, ButtonSet buttons)
    {
        hash.Add(buttons.Count);
        for (int button = 0; button < buttons.Count; button++)
        {
            hash.Add(buttons.IdOf(button));
            hash.Add((int)buttons.MaskOf(button));
        }
    }
}
