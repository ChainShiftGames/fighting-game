namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>What a <see cref="ChainLink"/> points at.</summary>
public enum ChainTargetKind
{
    /// <summary>A move named in a window's <c>into</c>.</summary>
    Move,

    /// <summary>Every move of a category (<c>intoCategories</c>), e.g. any special move.</summary>
    Category,

    /// <summary>A jump cancel (<c>intoJump</c>).</summary>
    Jump,
}
