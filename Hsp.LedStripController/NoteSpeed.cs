namespace Hsp.LedStripController;

/// <summary>
///   Converts the generic argument (MIDI Note 16 velocity, 0-127) into an
///   animation step duration expressed as a musical note in 32nd-note units.
///   The 32nd note is the finest resolution because the program position is
///   counted in 32nd notes, so no speed faster than one step per 32nd note is
///   possible.
///   Velocities 0 and 1 both map to a 32nd note (duration 1, the maximum speed);
///   higher velocities map to progressively longer (slower) note values. The
///   breakpoints fall on multiples of 10 so the mapping is easy to program:
///   0-9 = 32nd, 10-19 = 16th, 20-29 = 8th, 30-39 = quarter, 40-49 = half,
///   50-59 = whole, 60-69 = breve, 70+ = longa (duration 128).
/// </summary>
public static class NoteSpeed
{
  // Musical note durations in 32nd-note units, fastest to slowest:
  // 32nd=1, 16th=2, 8th=4, quarter=8, half=16, whole=32, breve=64, longa=128.
  private static readonly int[] Durations = { 1, 2, 4, 8, 16, 32, 64, 128 };

  /// <summary>
  ///   Returns the step duration in 32nd-note units for the given argument.
  ///   Breakpoints are at multiples of 10 (0-9 = 32nd, 10-19 = 16th, ...).
  /// </summary>
  public static double DurationFromArgument(int argument)
  {
    if (argument < 0) argument = 0;
    if (argument > 127) argument = 127;

    // Each 10-velocity step selects the next longer note; 70+ clamps to longa.
    var idx = argument / 10;
    if (idx >= Durations.Length) idx = Durations.Length - 1;
    return Durations[idx];
  }
}