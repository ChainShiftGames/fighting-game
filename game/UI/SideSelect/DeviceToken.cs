using FightingGame.UI.CharacterSelect;
using Godot;

namespace FightingGame.UI.SideSelect;

/// <summary>
/// One device's icon on side select: picture, name and status, framed in its side's colour. It slides to where it is
/// told to be and shakes on a refused move; both only change how it looks, never when input counts.
/// </summary>
public partial class DeviceToken : PanelContainer
{
    private const float ShakePixels = 8f;
    private const float ShakeSwings = 3f;

    private static readonly Color _middle = new(1f, 1f, 1f, 0.06f);
    private static readonly Color _middleBorder = new(1f, 1f, 1f, 0.25f);

    private readonly DeviceIconShape _icon = new();
    private readonly Label _name = new() { ThemeTypeVariation = "MenuValue", ClipText = true };
    private readonly Label _status = new() { ThemeTypeVariation = "MenuItemDescription" };
    private readonly StyleBoxFlat _style = SelectStyles.Box(_middle, _middleBorder, 2, 8f);
    private Vector2 _from;
    private Vector2 _to;
    private long _slideStart;
    private long? _shakeStart;
    private int _refusals = -1;
    private bool _isPlaced;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", _style);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        text.AddThemeConstantOverride("separation", 0);
        text.AddChild(_name);
        text.AddChild(_status);
        row.AddChild(_icon);
        row.AddChild(text);
        AddChild(row);
    }

    /// <param name="side">0 or 1 on a side, -1 in the middle.</param>
    public void Show(DeviceIcon icon, string name, string status, int side, bool isReady)
    {
        _icon.Icon = icon;
        _name.Text = name;
        _status.Text = status;
        _status.Visible = status.Length > 0;
        Color colour = side < 0 ? _middleBorder : SelectStyles.Of(side);
        _style.BorderColor = colour;
        _style.BgColor = isReady ? new Color(colour, 0.45f) : side < 0 ? _middle : new Color(colour, 0.15f);
        _style.SetBorderWidthAll(isReady ? 4 : 2);
    }

    /// <summary>Where the token belongs now; it slides there from where it is, or appears there the first time.</summary>
    public void MoveTo(Vector2 target, long nowMilliseconds)
    {
        if (!_isPlaced)
        {
            _isPlaced = true;
            _from = _to = target;
            return;
        }

        if (target != _to)
        {
            _from = Position;
            _to = target;
            _slideStart = nowMilliseconds;
        }
    }

    /// <summary>Shakes when <paramref name="refusals"/> went up since the last call.</summary>
    public void Refused(int refusals, long nowMilliseconds)
    {
        if (_refusals >= 0 && refusals > _refusals)
        {
            _shakeStart = nowMilliseconds;
        }

        _refusals = refusals;
    }

    /// <summary>Places the token for this displayed frame.</summary>
    public void Animate(SideSelectSettings settings, long nowMilliseconds)
    {
        float slide = Progress(nowMilliseconds - _slideStart, settings.SlideMilliseconds);
        float eased = 1f - ((1f - slide) * (1f - slide));
        Vector2 position = _from.Lerp(_to, eased);
        float shake = _shakeStart is { } start ? Progress(nowMilliseconds - start, settings.ShakeMilliseconds) : 1f;
        if (shake < 1f)
        {
            position.X += Mathf.Sin(shake * Mathf.Pi * 2f * ShakeSwings) * ShakePixels * (1f - shake);
        }

        Position = position;
    }

    private static float Progress(long elapsed, long length) =>
        length <= 0 || elapsed >= length ? 1f : elapsed < 0 ? 0f : (float)elapsed / length;
}
