# ADR-0008: Presentation direction (placeholders first)

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-7, GAP-30, GAP-31, GAP-32, GAP-37

## Decision

1. **Systems first.** Placeholder art until most systems are built. Visual decisions never block gameplay work.
   The art spike (Spike B) happens just before the art phase.
2. **Visual effects are developer toggles** (outlines, shading style, shake, flashes, hit effects…) so the visual
   direction can be iterated on without code changes. The accessibility options in point 5 are separate
   player settings.
3. Target look for later: fixed light per character, Xrd-style hard shading, constant-thickness outlines,
   4× MSAA. Hit-effect implementation is decided later.
4. **Audio:** Godot's built-in audio. Switch to FMOD only if we hit limits. Loudness target −16 LUFS.
5. **Accessibility baseline:** full remapping, shake/flash toggles, colour-blind-safe colours, scalable text,
   input display, and **directional audio** (sounds panned by on-screen position, with distinct cues) so blind
   players can follow positions and actions.
