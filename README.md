# Hsp.LedProxy

A project that converts MIDI messages (received via REAPER) into UDP/OSC
messages that drive external LED strips over WiFi. It consists of:

- **Hsp.LedStripController** — a REAPER plugin (.NET) that reads a shared GMEM
  block written by the `ledcontroller.jsfx` JSFX plugin, runs an animation
  program per strip, and broadcasts the resulting segment colors as OSC
  messages over UDP.
- **Hsp.LedStripEmulator** — a debugging tool that emulates the LED strips.
- **Arduino/LedStripController** — an ESP32 sketch that receives the OSC
  messages and drives the physical LED strips.

See [`copilot-instructions.md`](copilot-instructions.md) for the full
architecture, MIDI protocol, and OSC wire format. This document focuses on
the animation programs that `Hsp.LedStripController` can run per strip.

## Strip model

Each of the 4 strips has **12 segments**. A program renders all 12 segments
every tick. Each segment is packed as a 32-bit ARGB value where `A` is a
per-segment brightness/opacity (0 = off, 255 = full) and `R/G/B` is the color.

## GMEM layout (per strip)

The JSFX writes one block of 17 doubles per strip into the shared GMEM block.
`Hsp.LedStripController` reads it on every tick (every ~10 ms):

| Slot | Field              | Source                       | Description                                             |
|------|--------------------|------------------------------|---------------------------------------------------------|
| 0–11 | Segment 0–11 ARGB  | MIDI notes 0–11 velocity     | Per-segment brightness/color (used by the default program and as the base color for animation programs). |
| 12   | Program number     | —                            | Selects the animation program (see table below). `0` or any unregistered number runs the pass-through default program. |
| 13   | Program position   | —                            | Playhead position in **32nd-note** units. Driven by the JSFX transport. |
| 14   | Argument 1         | MIDI Note 16 velocity (0–127) | Animation **speed** for speed-scaled programs (see [Speed mapping](#speed-mapping)). Unused by programs 1 and 2. |
| 15   | Argument 2         | MIDI Note 17 velocity (0–127) | Generic argument 2. Currently unused by all shipped programs; reserved for future use. |
| 16   | Brightness multiplier | MIDI CC 110 (0–127)       | Per-strip brightness multiplier (0.0–1.0), applied to the alpha channel of every segment after the program renders. |

## Supported programs

The program number (GMEM slot 12) selects one of the programs registered in
`Plugin.cs`. Speed is controlled live through Argument 1 (MIDI Note 16
velocity), so each pattern/direction variant is registered only once.

| #  | Program              | Direction/Variant       | Speed-scaled | Description                                                                                                                                                                                                                                                                                  |
|----|----------------------|-------------------------|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 0  | _Default (pass-through)_ | —                  | No           | Used when no program is registered for the given number (and as the fallback). Copies the 12 segment ARGB values from GMEM slots 0–11 straight to the output unchanged. This is the original MIDI-driven behavior where the JSFX controls every segment directly.                       |
| 1  | **ColorCycle**       | —                       | No           | Cycles a hue across the 12 segments based on the program position. Hue advances 15° per 32nd note, with a 30° hue offset between adjacent segments, producing a rolling rainbow. Ignores Argument 1. Output is always full alpha (255).                                                     |
| 2  | **Pulse**            | —                       | Yes          | Takes the color of segment 0 as the base RGB and replicates it across all 12 segments, then pulses the opacity from 0% → 100% → 0% as a triangle wave over a cycle of 32 scaled steps. At the fastest speed (Argument 1 = 0/1) one cycle equals one bar of 4/4 (32 32nd notes); higher velocities lengthen the cycle. |
| 3  | **Random**           | —                       | Yes          | Takes segment 0's color as the base RGB and assigns a **random opacity** (0–255) to each segment. New random values are generated once per animation step.                                                                                                                                 |
| 4  | **Fall**             | Downward                | Yes          | A "falling star" with a 4-segment trail (100%, 50%, 25%, 12% opacity) moves from segment 0 toward segment 11 and restarts once the last trail segment has left the strip. Uses segment 0's color as the base RGB.                                                                            |
| 5  | **Fall**             | Upward                  | Yes          | Same as program 4 but the star travels from segment 11 toward segment 0, with the trail trailing behind in the opposite direction.                                                                                                                                                         |
| 6  | **Expand**           | Expand outward          | Yes          | Two stars start at the center (segments 5 and 6) and travel symmetrically outward toward the edges, each leaving a 4-segment trail (100%, 50%, 25%, 12%) pointing back toward the center. Restarts once the last trail segment has vanished. Uses segment 0's color as the base RGB.    |
| 7  | **Expand**           | Collide inward          | Yes          | Two stars start at the outer edges (segments 0 and 11) and travel inward, colliding at the center and passing through, each leaving a 4-segment trail pointing outward.                                                                                                                     |

### Program arguments

Arguments are read from GMEM slots 14 and 15 and passed into every program's
`Render` call as a `ProgramArguments` struct (two doubles).

| Argument    | GMEM slot | MIDI source            | Used by                                                                                       |
|-------------|-----------|------------------------|-----------------------------------------------------------------------------------------------|
| Argument 1  | 14        | MIDI Note 16 velocity  | Animation **speed** for all speed-scaled programs (2–7). Ignored only by program 1. |
| Argument 2  | 15        | MIDI Note 17 velocity  | Reserved. Currently unused by all shipped programs.                                          |

For the speed-scaled programs, Argument 1 selects a musical-note duration: the
program advances one animation step per that many 32nd notes of program
position. See the mapping below.

## Speed mapping

`NoteSpeed.DurationFromArgument` maps Argument 1 (MIDI Note 16 velocity,
0–127) to a step duration in 32nd-note units. The 32nd note is the finest
resolution because the program position is counted in 32nd notes, so nothing
faster than one step per 32nd note is possible. Breakpoints fall on multiples
of 10 so the mapping is easy to program, and velocities 0 and 1 both run at
the maximum (32nd-note) speed.

| Velocity range | Musical note | Duration (32nd notes) | Relative speed |
|----------------|--------------|------------------------|-----------------|
| 0–9            | 32nd note    | 1                      | Fastest         |
| 10–19          | 16th note    | 2                      | ½ ×             |
| 20–29          | 8th note     | 4                      | ¼ ×             |
| 30–39          | Quarter note | 8                      | ⅛ ×             |
| 40–49          | Half note    | 16                     | 1/16 ×          |
| 50–59          | Whole note   | 32                     | 1/32 ×          |
| 60–69          | Breve        | 64                     | 1/64 ×          |
| 70–127         | Longa        | 128                    | Slowest         |

Values are clamped to the `[0, 127]` range before mapping. Velocity 70 and
above all clamp to the longa (duration 128).

## Brightness multiplier

In addition to the per-program animation, MIDI CC 110 (0–127) is scaled to a
0.0–1.0 multiplier stored in GMEM slot 16. After a program renders,
`Hsp.LedStripController` multiplies every segment's alpha channel by this
value (clamped to 0–255). This lets the musician fade a whole strip in and
out independently of the running program.