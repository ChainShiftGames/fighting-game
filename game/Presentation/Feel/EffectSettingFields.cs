using System.Collections.Generic;

namespace FightingGame.Presentation.Feel;

/// <summary>
/// Every field of the effect settings file with its range: the one list both <see cref="EffectSettingsFile"/>
/// checks the file against and the control panel shows.
/// </summary>
public static class EffectSettingFields
{
    public const string ShakeSection = "screenShake";
    public const string SparkSection = "hitSparks";
    public const string RumbleSection = "rumble";

    // The ranges keep each effect usable: no shake longer than a second, sparks from barely there to huge, pulses
    // within what a controller motor takes.
    private const float MaxShakePixels = 40f;
    private const int MaxShakeFrames = 60;
    private const int MaxSparkHitstop = 60;
    private const float MaxSparkScale = 4f;
    private const float MaxRumbleSeconds = 2f;

    public static readonly EffectSettingField ShakeLifetime = new(
        ShakeSection, "lifetimeFrames", "Shake length (frames)", 1, MaxShakeFrames, 1, whole: true,
        "How long every shake takes to die down.");

    public static readonly EffectSettingField ShakeHit = new(
        ShakeSection, "hitPixels", "Hit shake (px)", 0, MaxShakePixels, 0.5f, whole: false,
        "Starting strength of a normal hit's shake. 0 turns it off.");

    public static readonly EffectSettingField ShakeCounter = new(
        ShakeSection, "counterPixels", "Counter hit shake (px)", 0, MaxShakePixels, 0.5f, whole: false,
        "Starting strength on counter and punish counter hits.");

    public static readonly EffectSettingField ShakeThrow = new(
        ShakeSection, "throwPixels", "Throw shake (px)", 0, MaxShakePixels, 0.5f, whole: false,
        "Starting strength when a throw connects.");

    public static readonly EffectSettingField ShakeKnockOut = new(
        ShakeSection, "knockOutPixels", "Knock-out shake (px)", 0, MaxShakePixels, 0.5f, whole: false,
        "Starting strength of the round-ending hit.");

    public static readonly EffectSettingField ShakeVertical = new(
        ShakeSection, "verticalShare", "Vertical share", 0, 2, 0.05f, whole: false,
        "Up-down movement as a share of the sideways one. 0 shakes sideways only.");

    public static readonly EffectSettingField SparkLightHitstop = new(
        SparkSection, "lightHitstop", "Lightest hit's hitstop", 0, MaxSparkHitstop, 1, whole: true,
        "Hits with this much hitstop or less get the smallest spark (weight 0).");

    public static readonly EffectSettingField SparkHeavyHitstop = new(
        SparkSection, "heavyHitstop", "Heaviest hit's hitstop", 1, MaxSparkHitstop, 1, whole: true,
        "Hits with this much hitstop or more get the biggest spark (weight 1). Must be above the lightest.");

    public static readonly EffectSettingField SparkCounterWeight = new(
        SparkSection, "counterWeight", "Counter hit extra weight", 0, 1, 0.05f, whole: false,
        "Added to a counter hit's weight: bigger sparks.");

    public static readonly EffectSettingField SparkBlockShare = new(
        SparkSection, "blockShare", "Block weight share", 0, 1, 0.05f, whole: false,
        "The share of the hit's weight a blocked hit keeps.");

    public static readonly EffectSettingField SparkTearFrom = new(
        SparkSection, "tearFromWeight", "Tear from weight", 0, 1.5f, 0.05f, whole: false,
        "Normals at least this heavy tear through the defender; lighter ones burst. Specials always tear.");

    public static readonly EffectSettingField SparkTechWeight = new(
        SparkSection, "techWeight", "Throw tech weight", 0, 1, 0.05f, whole: false,
        "The spark weight of a throw tech, which has no hit data.");

    public static readonly EffectSettingField SparkScale = new(
        SparkSection, "scale", "Spark size", 0.1f, MaxSparkScale, 0.05f, whole: false,
        "Multiplies the size of every hit spark.");

    public static readonly EffectSettingField RumbleHeavy = new(
        RumbleSection, "heavyStrength", "Heavy rumble strength", 0, 1, 0.05f, whole: false,
        "For the player who got hit, and the loser on knock-out.");

    public static readonly EffectSettingField RumbleLight = new(
        RumbleSection, "lightStrength", "Light rumble strength", 0, 1, 0.05f, whole: false,
        "For the attacker, and for the defender on block.");

    public static readonly EffectSettingField RumbleHitSeconds = new(
        RumbleSection, "hitSeconds", "Hit pulse (s)", 0, MaxRumbleSeconds, 0.05f, whole: false,
        "How long a hit's or block's pulse lasts.");

    public static readonly EffectSettingField RumbleKnockOutSeconds = new(
        RumbleSection, "knockOutSeconds", "Knock-out pulse (s)", 0, MaxRumbleSeconds, 0.05f, whole: false,
        "How long the knock-out pulse lasts.");

    /// <summary>The file's sections in file order.</summary>
    public static IReadOnlyList<string> Sections { get; } = [ShakeSection, SparkSection, RumbleSection];

    /// <summary>All fields, in file order.</summary>
    public static IReadOnlyList<EffectSettingField> All { get; } =
    [
        ShakeLifetime, ShakeHit, ShakeCounter, ShakeThrow, ShakeKnockOut, ShakeVertical,
        SparkLightHitstop, SparkHeavyHitstop, SparkCounterWeight, SparkBlockShare, SparkTearFrom, SparkTechWeight,
        SparkScale,
        RumbleHeavy, RumbleLight, RumbleHitSeconds, RumbleKnockOutSeconds,
    ];
}
