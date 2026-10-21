namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>What pins a frame on the animation track (ADR-0028).</summary>
public enum TrackPinKind
{
    /// <summary>The first active frame: <c>activeFrom</c>, the contact pose.</summary>
    ActiveFrom,

    /// <summary>The first recovery frame: <c>recoveryFrom</c>.</summary>
    RecoveryFrom,

    /// <summary>A timing key inside a phase.</summary>
    Key,
}
