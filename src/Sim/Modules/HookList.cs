namespace Sim.Modules;

/// <summary>The lineup's modules that implement one hook, in slot order, each with its slot.</summary>
internal sealed class HookList<THook>
    where THook : class
{
    private readonly int[] _slots;
    private readonly THook[] _hooks;

    /// <param name="modulesBySlot">The lineup indexed by slot; null for an empty slot.</param>
    public HookList(IMechanicModule?[] modulesBySlot)
    {
        int count = 0;
        foreach (IMechanicModule? module in modulesBySlot)
        {
            if (module is THook)
            {
                count++;
            }
        }

        _slots = new int[count];
        _hooks = new THook[count];
        int index = 0;
        for (int slot = 0; slot < modulesBySlot.Length; slot++)
        {
            if (modulesBySlot[slot] is THook hook)
            {
                _slots[index] = slot;
                _hooks[index] = hook;
                index++;
            }
        }
    }

    public int Count => _hooks.Length;

    public int SlotAt(int index) => _slots[index];

    public THook HookAt(int index) => _hooks[index];
}
