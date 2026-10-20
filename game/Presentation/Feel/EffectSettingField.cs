namespace FightingGame.Presentation.Feel;

/// <summary>
/// One number in the effect settings file: where it sits, what the control panel calls it, and the range the file
/// may set.
/// </summary>
public sealed class EffectSettingField
{
    /// <param name="section">The object in the file holding the field, e.g. <c>screenShake</c>.</param>
    /// <param name="key">The field's key in that object.</param>
    /// <param name="step">The control panel's arrow increment.</param>
    /// <param name="whole">True when the value must be a whole number (frames).</param>
    /// <param name="hint">One line on what changing it does, shown by the control panel.</param>
    public EffectSettingField(
        string section, string key, string label, float min, float max, float step, bool whole, string hint)
    {
        Section = section;
        Key = key;
        Label = label;
        Min = min;
        Max = max;
        Step = step;
        Whole = whole;
        Hint = hint;
    }

    public string Section { get; }

    public string Key { get; }

    /// <summary>The field's path in the file, <c>section.key</c>.</summary>
    public string Path => $"{Section}.{Key}";

    public string Label { get; }

    public float Min { get; }

    public float Max { get; }

    public float Step { get; }

    public bool Whole { get; }

    public string Hint { get; }
}
