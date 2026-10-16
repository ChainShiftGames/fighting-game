using System;
using System.Linq;
using Netcode.Handshake;
using Netcode.Tests.Transport;
using Sim.Rules;
using Xunit;

namespace Netcode.Tests.Handshake;

/// <summary>P2-05: two PCs only play when build, game data and match rules all match (ADR-0005 §6).</summary>
public sealed class VersionHandshakeTests
{
    private const int FightingFrames = 60;

    // Enough steps for many hellos each way (one per 6 frames), after both sides refused.
    private const int StepsAfterRefusal = 60;

    [Fact]
    public void PeersOfTheSameBuildAgreeAndPlay()
    {
        using var match = new InMemoryMatch(FightingFrames);

        match.RunUntilFrame(FightingFrames * 2);

        Assert.Equal(HandshakeOutcome.Agreed, match.Player1Handshake.Outcome);
        Assert.Equal(HandshakeOutcome.Agreed, match.Player2Handshake.Outcome);
    }

    [Fact]
    public void APeerWithOneChangedRuleValueIsRefused()
    {
        MatchRules rules = SameBuild.Rules;
        rules.ButtonBuffer++;

        AssertRefusedOnBothSides(new MatchIdentity(SameBuild.GameVersion, SameBuild.DataHash, rules), "match rules differ");
    }

    [Fact]
    public void APeerOnAnotherBuildIsRefused()
    {
        AssertRefusedOnBothSides(
            new MatchIdentity("1.0.0+other", SameBuild.DataHash, SameBuild.Rules), "game version 1.0.0+test here, 1.0.0+other there");
    }

    [Fact]
    public void APeerWithOtherGameDataIsRefused()
    {
        byte[] otherData = SameBuild.DataHash;
        otherData[^1] ^= 1;

        AssertRefusedOnBothSides(new MatchIdentity(SameBuild.GameVersion, otherData, SameBuild.Rules), "game data differs");
    }

    [Fact]
    public void AHelloOfAnotherFormatIsRefused()
    {
        using var match = new InMemoryMatch(FightingFrames);
        byte[] futureHello = [0x46, 0x47, HelloMessage.Format + 1, 0];
        match.Player1Transport.Deliver(futureHello);

        match.RunUntil(() => match.Player1Handshake.Outcome != HandshakeOutcome.Waiting);

        Assert.Equal(HandshakeOutcome.Refused, match.Player1Handshake.Outcome);
        Assert.Contains($"handshake format 1 here, 2 there", match.Player1Handshake.Refusal, StringComparison.Ordinal);
    }

    // P1-20's test proves this hash changes with every rules field; the handshake must compare exactly it.
    [Fact]
    public void TheHandshakeComparesTheMatchRulesHash()
    {
        MatchRules rules = SameBuild.Rules;

        Assert.Equal(rules.ComputeHash(), new MatchIdentity(SameBuild.GameVersion, SameBuild.DataHash, rules).RulesHash);
    }

    private static void AssertRefusedOnBothSides(MatchIdentity player2, string difference)
    {
        using var match = new InMemoryMatch(FightingFrames, player2Identity: player2);
        int stepsLeft = StepsAfterRefusal;

        match.RunUntil(() =>
            match.Player1Handshake.Outcome == HandshakeOutcome.Refused &&
            match.Player2Handshake.Outcome == HandshakeOutcome.Refused &&
            --stepsLeft == 0);

        Assert.Equal(0, match.Player1.CurrentFrame);
        Assert.Equal(0, match.Player2.CurrentFrame);
        Assert.StartsWith(VersionHandshake.Mismatch, match.Player1Handshake.Refusal, StringComparison.Ordinal);
        Assert.Contains(difference, match.Player1Handshake.Refusal, StringComparison.Ordinal);

        // Not one netcode packet crossed: only hellos.
        Assert.All(match.Player1Transport.Sent.Concat(match.Player2Transport.Sent), packet => Assert.True(HelloMessage.IsHello(packet)));
    }
}
