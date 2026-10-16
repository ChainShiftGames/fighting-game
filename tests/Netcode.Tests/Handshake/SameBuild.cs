using Netcode.Handshake;
using Netcode.Tests.Matches;
using Sim.Rules;

namespace Netcode.Tests.Handshake;

/// <summary>The identity of the build under test, with Testman mirror rules, for peers that should agree.</summary>
internal static class SameBuild
{
    public const string GameVersion = "1.0.0+test";

    public static MatchRules Rules => TestmanMirror.Create().Rules;

    public static byte[] DataHash => TestmanMirror.Data.Hash.ToBytes();

    public static MatchIdentity Identity() => new(GameVersion, DataHash, Rules);

    public static VersionHandshake Handshake() => new(Identity());
}
