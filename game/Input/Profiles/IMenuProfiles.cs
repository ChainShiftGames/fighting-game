namespace FightingGame.Input.Profiles;

/// <summary>The layouts menus read their confirm and back buttons from, per device as menus see them.</summary>
public interface IMenuProfiles
{
    BindingProfile Keyboard { get; }

    /// <summary>The layout of Godot's joypad <paramref name="joypad"/>; null when the roster doesn't use it.</summary>
    BindingProfile? Joypad(int joypad);
}
