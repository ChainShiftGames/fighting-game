using Godot;

namespace FightingGame.Settings;

/// <summary>Sets the window mode and, when windowed, its size, centred on the screen it is on.</summary>
public sealed class ScreenApplier : IPlayerSettingApplier
{
    private (ScreenMode Mode, int Width, int Height)? _applied;

    public void Apply(PlayerSettings settings)
    {
        (ScreenMode, int, int) wanted = (settings.ScreenMode, settings.WindowWidth, settings.WindowHeight);
        if (_applied == wanted)
        {
            return;
        }

        _applied = wanted;
        DisplayServer.WindowSetMode(settings.ScreenMode switch
        {
            ScreenMode.Borderless => DisplayServer.WindowMode.Fullscreen,
            ScreenMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
            _ => DisplayServer.WindowMode.Windowed,
        });
        if (settings.ScreenMode != ScreenMode.Windowed)
        {
            return;
        }

        var size = new Vector2I(settings.WindowWidth, settings.WindowHeight);
        Rect2I screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(screen.Position + ((screen.Size - size) / 2));
    }
}
