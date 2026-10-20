using FightingGame.Presentation.World.Projectiles;
using Godot;

namespace FightingGame.Presentation.World.Sparks;

/// <summary>
/// Draws one hit spark (<c>hit_spark.gdshader</c>) for the frame of it the logic is on: a card on the gameplay plane,
/// turned the way the attack travels, over the fighters.
/// </summary>
/// <remarks>Reads only; called once per displayed frame while the spark plays, so it must not allocate.</remarks>
public partial class HitSparkArt : Node3D
{
    // Developer setting (presentation): the card in metres, from 1.4 m behind the contact to 3.8 m past it: room for
    // the longest tear and the debris.
    private static readonly Vector2 _cardSize = new(5.2f, 2.8f);
    private static readonly Vector2 _cardCentre = new(1.2f, 0f);

    private static readonly StringName _age = "age";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _style = "style";
    private static readonly StringName _weight = "weight";
    private static readonly StringName _seed = "seed";
    private static readonly StringName _down = "down";
    private static readonly StringName _cardCentreName = "card_centre";

    private readonly ShaderCard _card = new();

    public HitSparkArt()
    {
        _card.Build("hit_spark", _cardSize, _cardCentre);
        _card.SetParameter(_cardCentreName, _cardCentre);
        AddChild(_card);
    }

    /// <param name="age">Logic frames since the spark started (0 on its first drawing).</param>
    public void ShowFrame(int age, in HitSparkLook look)
    {
        Position = WorldScale.ToWorld(look.Position);
        Rotation = new Vector3(0f, 0f, look.Angle);
        Scale = new Vector3(look.Scale, look.Scale, 1f);
        _card.SetClock(age + 1, 0f);
        _card.SetParameter(_age, (float)age);
        _card.SetParameter(_kind, (int)look.Tone);
        _card.SetParameter(_style, (int)look.Style);
        _card.SetParameter(_weight, look.Weight);
        _card.SetParameter(_seed, look.Seed);

        // World down turned into the card's space, which is turned by the attack's angle.
        _card.SetParameter(_down, new Vector2(-Mathf.Sin(look.Angle), -Mathf.Cos(look.Angle)));
    }
}
