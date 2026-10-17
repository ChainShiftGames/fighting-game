namespace Sim.Modules.ComboLimits;

/// <summary>The combo one fighter is taking, as the combo limits module tracks it.</summary>
public struct DefenderCombo
{
    /// <summary>Hits (and throws) taken since the fighter could last act; 0 when not in a combo.</summary>
    public int Hits;

    /// <summary>Hits taken while knocked down in this combo (OTG hits).</summary>
    public int OtgHits;

    /// <summary>1 while the fighter is knocked down, as of the state machines this frame or its latest hit.</summary>
    public int IsDown;
}
