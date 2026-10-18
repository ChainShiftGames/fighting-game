namespace FightingGame.VfxLab;

/// <summary>What the Lab draws behind the effect; numbered as the backdrop shader's <c>mode</c>.</summary>
public enum LabBackdrop
{
    /// <summary>A bright daylight stage: sky, rock and falling water, busy like a real stage.</summary>
    Day = 0,
    Night = 1,
    Grey = 2,
    Black = 3,
    White = 4,

    /// <summary>A hard checkerboard, to see how much the effect bends what's behind it.</summary>
    Checker = 5,

    /// <summary>The picture the stage art file names, when there is one.</summary>
    StagePicture = 6,
}
