namespace Sim.Data.Characters.Compiled;

/// <summary>What a box does: hurt (can be hit), hit (hits hurtboxes), push (keeps fighters apart), throw.</summary>
public enum BoxKind
{
    Hurt,
    Hit,
    Push,
    Throw,
}
