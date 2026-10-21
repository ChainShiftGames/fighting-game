#if TOOLS
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// The character editor's colours (developer setting, editor look): deep indigo surfaces, an aether-cyan accent, and
/// one colour per move phase that the map, the frame strip and the timeline all share.
/// </summary>
public static class MapPalette
{
    public static readonly Color Background = Color.FromHtml("0d0e21");
    public static readonly Color Panel = Color.FromHtml("14152c");
    public static readonly Color Raised = Color.FromHtml("1b1d3a");
    public static readonly Color Line = Color.FromHtml("2a2c50");
    public static readonly Color LineSoft = Color.FromHtml("22244a");
    public static readonly Color Text = Color.FromHtml("eceefe");
    public static readonly Color TextSoft = Color.FromHtml("c3c6e6");
    public static readonly Color Muted = Color.FromHtml("8e92bd");
    public static readonly Color Faint = Color.FromHtml("6b6f99");
    public static readonly Color Glow = Color.FromHtml("7fe3f0");
    public static readonly Color Violet = Color.FromHtml("a59cff");
    public static readonly Color OnGlow = Color.FromHtml("10112a");
    public static readonly Color Node = Color.FromHtml("181a36e6");
    public static readonly Color Keycap = Color.FromHtml("282b52");
    public static readonly Color EdgeDim = Color.FromHtml("3b3e72");
    public static readonly Color ArrowDim = Color.FromHtml("4b4f86");
    public static readonly Color PreviewCentre = Color.FromHtml("232659");

    public static readonly Color Windup = Color.FromHtml("f2b04a");
    public static readonly Color Active = Color.FromHtml("ff5a6e");
    public static readonly Color Recovery = Color.FromHtml("5f8dff");
    public static readonly Color Loop = Color.FromHtml("a59cff");

    // How a hit is blocked, for the map's guard tags.
    public static readonly Color GuardLow = Color.FromHtml("6ea8ff");
    public static readonly Color GuardOverhead = Color.FromHtml("ffa86b");

    public static readonly Color Good = Color.FromHtml("8be9b3");
    public static readonly Color Warn = Color.FromHtml("f4c47a");
    public static readonly Color Bad = Color.FromHtml("ff6b8a");
}
#endif
