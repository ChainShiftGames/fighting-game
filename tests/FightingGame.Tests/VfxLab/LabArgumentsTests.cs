using FightingGame.Presentation.World.Projectiles;
using FightingGame.VfxLab;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class LabArgumentsTests
{
    [Fact]
    public void ReadsACaptureRequest()
    {
        LabArguments args = LabArguments.Parse(
            ["--lab-sheet", "dark_fireball", "--lab-capture", "/tmp/a.png", "--lab-frame", "30", "--lab-size", "1600x900", "--lab-framing", "closeup", "--lab-impact", "1", "--lab-backdrop", "checker"],
            out string error);
        LabViewOptions options = new();
        args.ApplyTo(options);

        Assert.Equal(string.Empty, error);
        Assert.Equal("dark_fireball", args.Sheet);
        Assert.Equal("/tmp/a.png", args.Capture);
        Assert.Equal(30, args.Frame);
        Assert.Equal((1600, 900), args.Size);
        Assert.Equal(LabFramingKind.CloseUp, options.Framing);
        Assert.Equal(ImpactKind.Block, options.Impact);
        Assert.Equal(LabBackdrop.Checker, options.Backdrop);
    }

    [Fact]
    public void SaysWhichArgumentItCouldNotRead()
    {
        LabArguments.Parse(["--lab-frame", "soon"], out string error);

        Assert.Contains("--lab-frame soon", error);
    }
}
