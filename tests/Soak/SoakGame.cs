using System;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Transport;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Data.Online;
using Sim.Data.Sources;
using Sim.Rules;
using Sim.State;

namespace Soak;

/// <summary>
/// What every soak match is played with: the real game data embedded in Sim.Data (ADR-0015), a mirror of its first
/// character, the online settings from data and the ADR-0005 bad-network test conditions.
/// </summary>
internal sealed class SoakGame
{
    // Both soak peers are this build, so the version handshake always agrees.
    private const string GameVersion = "soak";

    private readonly GameData _data;
    private readonly CharacterData _character;

    public SoakGame()
    {
        var source = new EmbeddedDataSource();
        _data = new GameDataLoader(source).Load();
        _character = _data.Characters[0];
        OnlineSettings online = OnlineSettingsLoader.Load(source);
        RollbackFrames = online.RollbackFrames;
        DesyncHistoryFrames = online.Desync.HistoryFrames;
    }

    /// <summary>The rollback window (developer setting, <c>data/netcode/online.json</c>).</summary>
    public int RollbackFrames { get; }

    /// <summary>Frames a desync dump holds (developer setting, <c>data/netcode/online.json</c>).</summary>
    public int DesyncHistoryFrames { get; }

    /// <summary>Lag, jitter and loss each peer adds to what it sends: the ADR-0005 test conditions.</summary>
    public NetworkSimulation Network { get; } = NetworkSimulation.TestConditions;

    public string Describe() =>
        $"{_character.Id} mirror, data hash {_data.Hash}, rollback window {RollbackFrames} frames, " +
        $"{Network.PingMilliseconds} ms ping ±{Network.JitterMilliseconds} ms, {Network.PacketLossPercent} % loss, " +
        "in-memory transport";

    /// <summary>What a dump says about this build, as the online driver writes it from the handshake.</summary>
    public DesyncDumpHeader DumpHeader(bool localIsPlayer1) =>
        new(GameVersion, Convert.ToHexString(_data.Hash.ToBytes()), localIsPlayer1);

    /// <summary>A fresh version handshake for one peer (ADR-0005 §6): each PC runs its own.</summary>
    public VersionHandshake CreateHandshake() =>
        new(new MatchIdentity(GameVersion, _data.Hash.ToBytes(), _data.CreateMatchRules()));

    /// <summary>A fresh simulation for one peer: peers never share one, as two PCs wouldn't.</summary>
    public IMatchSimulation<MatchState> CreateSimulation()
    {
        MatchRules rules = _data.CreateMatchRules();
        return new ComposedMatchSimulation(new MatchSimulation(
            _character, _character, _data.Buttons, _data.Motions.Motions, _data.Motions.Movement, in rules));
    }
}
