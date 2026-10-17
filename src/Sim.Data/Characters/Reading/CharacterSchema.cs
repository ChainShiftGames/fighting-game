namespace Sim.Data.Characters.Reading;

/// <summary>Versions, paths and sanity limits of the character and move file format (data/schema/).</summary>
/// <remarks>
/// The limits are format bounds, not tuning values: they catch typos (an extra zero) and keep every sum of frames
/// and distances far inside <c>int</c>. data/schema/*.schema.json repeats them; change both together.
/// </remarks>
public static class CharacterSchema
{
    public const int CharacterVersion = 1;
    public const int MoveVersion = 2;

    public const string CharactersDirectory = "characters";

    public const int MaxMoveFrames = 999;
    public const int MaxDistance = 1_000_000;
    public const int MaxHealth = 1_000_000;
    public const int MaxDamage = 1_000_000;
    public const int MaxAirActions = 9;

    /// <summary>Upper bound for a counter hit's extra damage: 1000 % more, ten times beyond any real tuning.</summary>
    public const int MaxExtraDamagePercent = 1000;

    /// <summary>Upper bound for a projectile's clash durability.</summary>
    public const int MaxDurability = 99;

    /// <summary>Longest numpad motion (8 digits still fits an <c>int</c>).</summary>
    public const int MaxMotionLength = 8;

    public static string CharacterFile(string characterId) => $"{CharactersDirectory}/{characterId}/character.json";

    public static string MovesDirectory(string characterId) => $"{CharactersDirectory}/{characterId}/moves";

    public static string MoveFile(string characterId, string moveId) => $"{MovesDirectory(characterId)}/{moveId}.json";
}
