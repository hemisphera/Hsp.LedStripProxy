namespace Hsp.LedStripController.Programs;

/// <summary>
///   Draws a "falling star" that moves across the segments. The lead segment
///   is at 100% opacity, followed by a trail at 50%, 25% and 12%. The star
///   advances one segment per 16th note scaled by the speed coefficient. When
///   the last trail segment has vanished off-screen the animation restarts.
///   Program number: 5 (downward) / 6 (upward). The animation speed is driven by
///   the generic argument (MIDI Note 16 velocity), expressed as a musical note
///   (velocities 0 and 1 run at the maximum 32nd-note speed; higher velocity is
///   slower; see <see cref="NoteSpeed" />).
/// </summary>
public class FallProgram : SpeedScaledProgram
{
  private const int StarLength = 4; // lead + 3 trail segments
  private static readonly int[] TrailAlphas = { 255, 128, 64, 31 }; // 100%, 50%, 25%, 12%


  private readonly bool _upward;


  public FallProgram(bool upward)
  {
    _upward = upward;
  }


  public override void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments)
  {
    var first = (int)segments[0];
    var r = (first >> 16) & 0xFF;
    var g = (first >> 8) & 0xFF;
    var b = first & 0xFF;

    var step = (int)Math.Floor(GetScaledPosition(programPosition, in arguments));
    var cycleLength = LedStrip.NumSegmentsPerLedStrips + StarLength - 1; // 12 + 4 - 1 = 15
    var cyclePos = ((step % cycleLength) + cycleLength) % cycleLength;

    var leadPos = _upward
      ? (LedStrip.NumSegmentsPerLedStrips - 1) - cyclePos
      : cyclePos;

    // Clear all segments to off (alpha 0) while retaining the base RGB.
    for (var i = 0; i < buffer.Length; i++)
      buffer[i] = (r << 16) | (g << 8) | b;

    // Draw the star: lead at full opacity, trail fading behind.
    for (var j = 0; j < StarLength; j++)
    {
      var segPos = _upward ? leadPos + j : leadPos - j;
      if (segPos >= 0 && segPos < buffer.Length)
        buffer[segPos] = (TrailAlphas[j] << 24) | (r << 16) | (g << 8) | b;
    }
  }
}