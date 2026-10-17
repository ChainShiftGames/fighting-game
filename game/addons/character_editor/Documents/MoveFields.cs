namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>JSON keys and values of a move file (<c>data/schema/move.schema.json</c>) that editor code works with.</summary>
public static class MoveFields
{
    public const string Schema = "$schema";
    public const string SchemaVersion = "schemaVersion";
    public const string Id = "id";
    public const string Category = "category";
    public const string Duration = "duration";
    public const string FrameData = "frameData";
    public const string Startup = "startup";
    public const string Active = "active";
    public const string Recovery = "recovery";
    public const string UsableIn = "usableIn";
    public const string Input = "input";
    public const string Motion = "motion";
    public const string Button = "button";
    public const string Buttons = "buttons";
    public const string Boxes = "boxes";
    public const string Kind = "kind";
    public const string Cancels = "cancels";
    public const string FirstFrame = "firstFrame";
    public const string LastFrame = "lastFrame";
    public const string On = "on";
    public const string Into = "into";
    public const string IntoCategories = "intoCategories";
    public const string Velocity = "velocity";
    public const string Invulnerable = "invulnerable";
    public const string Hits = "hits";
    public const string Throw = "throw";
    public const string Projectiles = "projectiles";
    public const string SpawnFrame = "spawnFrame";

    /// <summary>The category of moves the state machine enters on its own: <c>duration</c>, no input or frame data.</summary>
    public const string BasicCategory = "basic";
}
