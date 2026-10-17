namespace Soak;

/// <summary>The bot seeds of each soak match, all derived from the one soak seed so a run can be replayed.</summary>
internal static class MatchSeeds
{
    public static uint Player1(uint soakSeed, int match) => Mix(soakSeed, match * 2);

    public static uint Player2(uint soakSeed, int match) => Mix(soakSeed, (match * 2) + 1);

    // A murmur3-style finaliser: nearby inputs give unrelated seeds.
    private static uint Mix(uint soakSeed, int stream)
    {
        uint value = soakSeed ^ ((uint)stream * 0x9E3779B9u);
        value ^= value >> 16;
        value *= 0x85EBCA6Bu;
        value ^= value >> 13;
        value *= 0xC2B2AE35u;
        value ^= value >> 16;
        return value;
    }
}
