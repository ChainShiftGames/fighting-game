using Godot;

namespace FightingGame.UI;

/// <summary>
/// Short notices in the top-right corner, below the HUD gauges, each gone after a few seconds; the newest at the
/// bottom. Presentation only, over menus and matches alike.
/// </summary>
public partial class ToastNotice : CanvasLayer
{
    // Developer settings.
    private const double ShownSeconds = 3.0;
    private const int MaxShown = 3;
    private const int Margin = 16;

    // Below the health bars, burst gauges and names (HudLayout's top block ends at about 14 % of the height).
    private const float TopShare = 0.17f;

    // Above the menus, under the controller disconnect overlay.
    private const int ToastLayer = 15;
    private static readonly Color _panel = new(0.08f, 0.08f, 0.1f, 0.85f);

    private readonly VBoxContainer _list = new()
    {
        AnchorLeft = 1f,
        AnchorRight = 1f,
        AnchorTop = TopShare,
        AnchorBottom = TopShare,
        OffsetRight = -Margin,
        GrowHorizontal = Control.GrowDirection.Begin,
    };

    public override void _Ready()
    {
        Layer = ToastLayer;
        AddChild(_list);
    }

    public void Show(string text)
    {
        if (_list.GetChildCount() >= MaxShown)
        {
            Node oldest = _list.GetChild(0);
            _list.RemoveChild(oldest);
            oldest.QueueFree();
        }

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = _panel,
            ContentMarginLeft = Margin,
            ContentMarginRight = Margin,
            ContentMarginTop = Margin / 2,
            ContentMarginBottom = Margin / 2,
        });
        panel.AddChild(new Label { Text = text });
        _list.AddChild(panel);
        GetTree().CreateTimer(ShownSeconds).Timeout += () =>
        {
            if (IsInstanceValid(panel))
            {
                panel.QueueFree();
            }
        };
    }
}
