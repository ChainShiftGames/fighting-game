using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// The Characters page: one character's <c>stats</c> (health, walking, jumping, gravity), with the ranges
/// <c>CharacterFileReader</c> accepts.
/// </summary>
public static class CharacterStatSettingList
{
    private const int MaxDistance = CharacterSchema.MaxDistance;
    private const int SpeedStep = 100;
    private const int HealthStep = 500;

    /// <summary>The setting holding the character's jump speed, for the jump readout.</summary>
    public static PanelSetting JumpVelocity(string characterId) =>
        Speed(characterId, "Jump velocity", "jumpVelocity", "Upward speed on the first airborne frame.");

    public static PanelSetting HighJumpVelocity(string characterId) =>
        Speed(characterId, "High jump velocity", "highJumpVelocity", "Upward speed of a high jump (28).");

    public static PanelSetting JumpForwardSpeed(string characterId) =>
        Speed(characterId, "Jump forward speed", "jumpForwardSpeed", "Sideways speed of a forward jump.");

    public static PanelSetting Gravity(string characterId) =>
        Speed(characterId, "Gravity", "gravity", "Taken off the upward speed every airborne frame.") with
        { Step = 50 };

    public static PanelSetting PrejumpFrames(string characterId)
    {
        PanelSetting setting = Stat(characterId, "Prejump", "prejumpFrames", 0, CharacterSchema.MaxMoveFrames);
        return setting with
        {
            Unit = SettingUnit.Frames,
            Hint = "Grounded frames between pressing up and leaving the ground.",
        };
    }

    public static IReadOnlyList<SettingGroup> For(string characterId) =>
    [
        new SettingGroup("Health and ground movement",
        [
            Stat(characterId, "Health", "health", 1, CharacterSchema.MaxHealth, HealthStep)
                with { Hint = "Damage the character can take before being knocked out." },
            Speed(characterId, "Walk forward speed", "walkForwardSpeed", "How fast the character walks forward."),
            Speed(characterId, "Walk back speed", "walkBackSpeed", "How fast the character walks backward."),
            PanelSetting.Choice(
                "Dash style", PanelFile.InData(CharacterSchema.CharacterFile(characterId)), "stats.dashStyle",
                CharacterJsonNames.DashStyles.Names) with { Hint = "Run: hold to keep running. Step: a fixed hop." },
        ])
        {
            Hint = "How much damage the character takes and how they move on the ground.",
        },
        new SettingGroup("Jumping and gravity",
        [
            PrejumpFrames(characterId),
            JumpVelocity(characterId),
            HighJumpVelocity(characterId),
            JumpForwardSpeed(characterId),
            Speed(characterId, "Jump back speed", "jumpBackSpeed", "Sideways speed of a back jump."),
            Gravity(characterId),
            Stat(characterId, "Air actions", "airActions", 0, CharacterSchema.MaxAirActions)
                with { Hint = "Shared budget for double jumps and air dashes." },
        ])
        {
            Hint = "How high, far and floaty the character's jumps are. The readout above shows the result.",
        },
    ];

    private static PanelSetting Stat(string characterId, string label, string key, int min, int max, int step = 1) =>
        PanelSetting.Whole(
            label, PanelFile.InData(CharacterSchema.CharacterFile(characterId)), $"stats.{key}", min, max, step);

    private static PanelSetting Speed(string characterId, string label, string key, string hint = "")
    {
        PanelSetting setting = Stat(characterId, label, key, 0, MaxDistance, SpeedStep);
        return setting with { Unit = SettingUnit.SubpixelsPerFrame, Hint = hint };
    }
}
