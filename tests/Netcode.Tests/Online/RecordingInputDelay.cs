using System.Collections.Generic;
using Netcode.Online;

namespace Netcode.Tests.Online;

/// <summary>Records every input delay change instead of making it.</summary>
internal sealed class RecordingInputDelay : IInputDelayControl
{
    public List<int> Changes { get; } = [];

    public void LowerLocalInputDelay(int frames) => Changes.Add(frames);
}
