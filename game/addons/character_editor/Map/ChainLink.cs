namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>One "can interrupt into" arrow: from a move to a move id, a move category, or a jump.</summary>
/// <param name="Target">The move id or category name; empty for a jump.</param>
public sealed record ChainLink(string From, ChainTargetKind Kind, string Target);
