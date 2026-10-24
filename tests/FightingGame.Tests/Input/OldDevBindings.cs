namespace FightingGame.Tests.Input;

/// <summary>An old per-player dev bindings file (schema 1), as every PC had one before per-device layouts (UI-10).</summary>
internal static class OldDevBindings
{
    /// <summary>The defaults the game copied to <c>user://dev_bindings.jsonc</c>, without their header comment.</summary>
    public const string Defaults = """
        {
          "schemaVersion": 1,
          "players": [
            {
              "keyboard": {
                "bindings": {
                  "up": ["W"], "down": ["S"], "left": ["A"], "right": ["D"],
                  // P is special + C at once: the 236C fireball by its shortcut, in one key for laptops.
                  "a": ["U"], "b": ["I"], "c": ["J", "P"], "d": ["K"], "special": ["O", "P"], "dash": ["L"]
                }
              },
              "gamepad": {
                "leftStick": true,
                "stickDeadzone": 0.5,
                "triggerThreshold": 0.5,
                "bindings": {
                  "up": ["DpadUp"], "down": ["DpadDown"], "left": ["DpadLeft"], "right": ["DpadRight"],
                  "a": ["X"], "b": ["Y"], "c": ["B"], "d": ["A"], "special": ["RightShoulder"], "dash": ["TriggerRight"]
                }
              }
            },
            {
              "keyboard": {
                "bindings": {
                  "up": ["Up"], "down": ["Down"], "left": ["Left"], "right": ["Right"],
                  "a": ["Kp4"], "b": ["Kp5"], "c": ["Kp1"], "d": ["Kp2"], "special": ["Kp6"], "dash": ["Kp3"]
                }
              },
              "gamepad": {
                "leftStick": true,
                "stickDeadzone": 0.5,
                "triggerThreshold": 0.5,
                "bindings": {
                  "up": ["DpadUp"], "down": ["DpadDown"], "left": ["DpadLeft"], "right": ["DpadRight"],
                  "a": ["X"], "b": ["Y"], "c": ["B"], "d": ["A"], "special": ["RightShoulder"], "dash": ["TriggerRight"]
                }
              }
            }
          ]
        }
        """;
}
