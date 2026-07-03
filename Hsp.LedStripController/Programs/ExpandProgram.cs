namespace Hsp.LedStripController.Programs;

/// <summary>
///   Animates two stars symmetrically from the centre of the strip.
///   When <c>_expand</c> is true the stars start at the centre and expand
///   outward; when false they start at the outer edges and collide at the
///   centre. Each star has a 4-segment trail (100%, 50%, 25%, 12%). The
///   animation restarts once the last trail segment has vanished.
///   Program number: 7 (expand) / 8 (collide). The animation speed is driven by
///   the generic argument (MIDI Note 16 velocity), expressed as a musical note
///   (velocities 0 and 1 run at the maximum 32nd-note speed; higher velocity is
///   slower; see <see cref="NoteSpeed" />).
/// </summary>
public class ExpandProgram : SpeedScaledProgram
{
  private const int StarLength = 4; // lead + 3 trail segments
  private static readonly int[] TrailAlphas = { 255, 128, 64, 31 }; // 100%, 50%, 25%, 12%

  private const int CenterLeft = LedStrip.NumSegmentsPerLedStrips / 2 - 1; // 5
  private const int CenterRight = LedStrip.NumSegmentsPerLedStrips / 2; // 6


  private readonly bool _expand;


  public ExpandProgram(bool expand)
  {
    _expand = expand;
  }


  public override void Start(LedStrip ledStrip)
  {
  }

  public override void Stop()
  {
  }

  public override void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments)
  {
    var first = (int)segments[0];
    var r = (first >> 16) & 0xFF;
    var g = (first >> 8) & 0xFF;
    var b = first & 0xFF;

    var step = (int)Math.Floor(GetScaledPosition(programPosition, in arguments));

    int cycleLength;
    int leftLead;
    int rightLead;
    int leftTrailDir; // direction the trail extends from the lead
    int rightTrailDir;

    if (_expand)
    {
      // Stars travel from centre outward; trail trails toward the centre.
      cycleLength = CenterLeft + StarLength; // 5 + 4 = 9
      var cyclePos = ((step % cycleLength) + cycleLength) % cycleLength;
      leftLead = CenterLeft - cyclePos; // 5 → -3
      leftTrailDir = +1; // trail extends right (toward centre)
      rightLead = CenterRight + cyclePos; // 6 → 14
      rightTrailDir = -1; // trail extends left (toward centre)
    }
    else
    {
      // Stars travel from the outer edges inward and pass through the centre.
      cycleLength = LedStrip.NumSegmentsPerLedStrips + StarLength - 1; // 15
      var cyclePos = ((step % cycleLength) + cycleLength) % cycleLength;
      leftLead = cyclePos; // 0 → 14
      leftTrailDir = -1; // trail extends left (outside)
      rightLead = (LedStrip.NumSegmentsPerLedStrips - 1) - cyclePos; // 11 → -3
      rightTrailDir = +1; // trail extends right (outside)
    }

    // Clear all segments to off (alpha 0) while retaining the base RGB.
    for (var i = 0; i < buffer.Length; i++)
      buffer[i] = (r << 16) | (g << 8) | b;

    DrawStar(buffer, leftLead, leftTrailDir, r, g, b);
    DrawStar(buffer, rightLead, rightTrailDir, r, g, b);
  }


  private static void DrawStar(double[] buffer, int leadPos, int trailDir, int r, int g, int b)
  {
    for (var j = 0; j < StarLength; j++)
    {
      var segPos = leadPos + j * trailDir;
      if (segPos < 0 || segPos >= buffer.Length)
        continue;

      // Blend by taking the brighter alpha where the two stars overlap.
      var existing = (int)buffer[segPos];
      var existingAlpha = (existing >> 24) & 0xFF;
      if (TrailAlphas[j] > existingAlpha)
        buffer[segPos] = (TrailAlphas[j] << 24) | (r << 16) | (g << 8) | b;
    }
  }
}