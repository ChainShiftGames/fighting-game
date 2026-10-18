using FightingGame.Presentation.World;
using Godot;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class RootTravelTests
{
    private static readonly Vector3 _first = new(0f, 0.9f, 0f);
    private static readonly Vector3 _last = new(0.2f, 0.9f, -1f);

    [Fact]
    public void BringsTheLastKeyBackOverTheFirst()
    {
        Vector3 last = RootTravel.Removed(_last, _first, _last, 1f);

        Assert.Equal(_first, last);
    }

    [Fact]
    public void TakesOutTravelInProportionAndKeepsTheSway()
    {
        // Halfway, swayed 5 cm sideways off the straight walk.
        Vector3 key = new(0.1f, 0.95f, -0.45f);

        Vector3 inPlace = RootTravel.Removed(key, _first, _last, 0.5f);

        Assert.Equal(0f, inPlace.X, 4);
        Assert.Equal(0.05f, inPlace.Z, 4);
    }

    [Fact]
    public void KeepsTheHeightSoACrouchStillDrops()
    {
        Vector3 crouched = RootTravel.Removed(new Vector3(0f, 0.4f, 0f), _first, new Vector3(0f, 0.4f, 0f), 1f);

        Assert.Equal(0.4f, crouched.Y, 4);
    }
}
