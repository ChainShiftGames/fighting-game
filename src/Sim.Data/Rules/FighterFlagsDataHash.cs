using Sim.Data.Hashing;

namespace Sim.Data.Rules;

/// <summary>Adds the fighter flags to the data hash: names in bit order and which a hit clears.</summary>
public static class FighterFlagsDataHash
{
    public static void Append(DataHashBuilder hash, FighterFlagsFile flags)
    {
        hash.Add(flags.Names.Names.Count);
        foreach (string name in flags.Names.Names)
        {
            hash.Add(name);
        }

        hash.Add(flags.ClearedWhenHit);
    }
}
