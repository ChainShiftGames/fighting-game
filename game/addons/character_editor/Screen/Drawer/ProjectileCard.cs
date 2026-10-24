#if TOOLS
using System;
using System.Globalization;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// One projectile's main settings in plain words: when it appears, how fast it flies forward and up, how long it
/// lasts and how many clashes it survives. Its hit, offset and boxes are under Overview › All settings.
/// </summary>
[Tool]
public partial class ProjectileCard : PanelContainer
{
    private const int Subpixels = 1000;
    private const int SpeedStep = 500;
    private const int MaxSpeed = 1_000_000;

    private readonly Label _title = new() { ThemeTypeVariation = MapTheme.FactLabel };
    private readonly Label _summary = new() { ThemeTypeVariation = MapTheme.FactHint, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly NumberStepper _spawn = new() { Minimum = 1 };
    private readonly NumberStepper _lifetime = new() { Minimum = 1, Maximum = 999 };
    private readonly NumberStepper _forward = new()
    {
        Minimum = -MaxSpeed,
        Maximum = MaxSpeed,
        Step = SpeedStep,
        Divisor = Subpixels,
    };

    private readonly NumberStepper _up = new()
    {
        Minimum = -MaxSpeed,
        Maximum = MaxSpeed,
        Step = SpeedStep,
        Divisor = Subpixels,
    };

    private readonly NumberStepper _durability = new() { Minimum = 1, Maximum = 99 };
    private readonly NumberStepper _rehit = new() { Minimum = 0, Maximum = 999 };
    private JsonObject _projectile = [];

    public ProjectileCard()
    {
        ThemeTypeVariation = MapTheme.Card;
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.AddChild(_title);
        body.AddChild(_summary);
        body.AddChild(Row(_spawn, _lifetime));
        body.AddChild(Row(_forward, _up));
        body.AddChild(Row(_durability, _rehit));
        AddChild(body);
        _spawn.ValueChanged += frame => Edit(() => ProjectileFields.SetSpawnFrame(_projectile, frame));
        _lifetime.ValueChanged += frames => Edit(() => ProjectileFields.SetLifetime(_projectile, frames));
        _forward.ValueChanged += speed => Edit(() => ProjectileFields.SetSpeedForward(_projectile, speed));
        _up.ValueChanged += speed => Edit(() => ProjectileFields.SetSpeedUp(_projectile, speed));
        _durability.ValueChanged += hits => Edit(() => ProjectileFields.SetDurability(_projectile, hits));
        _rehit.ValueChanged += frames => Edit(() => ProjectileFields.SetRehitInterval(_projectile, frames));
    }

    public event Action? Changed;

    /// <param name="moveFrames">The move's length: a projectile appears within it.</param>
    public void ShowProjectile(int number, JsonObject projectile, int moveFrames)
    {
        _projectile = projectile;
        _title.Text = string.Create(CultureInfo.InvariantCulture, $"Projectile {number}");
        _spawn.Maximum = Math.Max(1, moveFrames);
        _spawn.Show("Appears on frame", "of the move", ProjectileFields.SpawnFrame(projectile) ?? 1);
        _lifetime.Show("Lasts", "frames, counting the first", ProjectileFields.LifetimeOf(projectile) ?? 1);
        _forward.Show("Speed forward", "px per frame", ProjectileFields.SpeedForward(projectile) ?? 0);
        _up.Show("Speed up", "px per frame", ProjectileFields.SpeedUp(projectile));
        _durability.Show("Survives clashes", "hits from other projectiles", ProjectileFields.DurabilityOf(projectile));
        _rehit.Show("Hits again after", "frames; 0 = gone on hit", ProjectileFields.RehitIntervalOf(projectile));
        ShowSummary();
    }

    private static HBoxContainer Row(Control left, Control right)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(left);
        row.AddChild(right);
        return row;
    }

    private void Edit(Action change)
    {
        change();
        ShowSummary();
        Changed?.Invoke();
    }

    private void ShowSummary()
    {
        int speed = ProjectileFields.SpeedForward(_projectile) ?? 0;
        int lifetime = ProjectileFields.LifetimeOf(_projectile) ?? 0;
        double distance = Math.Abs((double)speed * lifetime / Subpixels);
        _summary.Text = speed == 0
            ? "Stays where it appears. Hit, position and boxes: Overview › All settings."
            : string.Create(CultureInfo.InvariantCulture,
                $"Flies {distance:0} px in {lifetime} frames ({DrawerMove.Seconds(lifetime)}) unless it hits. Hit, position and boxes: Overview › All settings.");
    }
}
#endif
