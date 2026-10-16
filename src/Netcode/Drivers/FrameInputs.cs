using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Both players' input words for one logic frame, as a replay records them.</summary>
public readonly record struct FrameInputs(InputWord Player1, InputWord Player2);
