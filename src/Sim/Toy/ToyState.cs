namespace Sim.Toy;

/// <summary>
/// The whole toy match state: fixed size, integers only, no references. A snapshot is a plain copy and the
/// checksum is a hash of its bytes (ARCHITECTURE.md §4).
/// </summary>
public struct ToyState
{
    public int Frame;
    public ToyFighters Fighters;
}
