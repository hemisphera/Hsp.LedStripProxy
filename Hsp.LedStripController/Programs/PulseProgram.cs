namespace Hsp.LedStripController.Programs;

/// <summary>
///   Replicates the color of the first segment across all 12 segments and pulses
///   the opacity (alpha) from 0% to 100% and back within one bar of 4/4 (16 16th notes).
///   Program number: 2.
/// </summary>
public class PulseProgram : ILedStripProgram
{
  private const double SixteenthNotesPerBar = 16.0; // 4/4


  public void Start(LedStrip ledStrip)
  {
  }

  public void Stop()
  {
  }

  public void Render(double programPosition, Span<double> segments, double[] buffer)
  {
    // Extract RGB from the first segment (packed ARGB int32); ignore its original alpha.
    var first = (int)segments[0];
    var r = (first >> 16) & 0xFF;
    var g = (first >> 8) & 0xFF;
    var b = first & 0xFF;

    // Triangle pulse: 0% at bar start, 100% at mid-bar, 0% at bar end.
    var barPos = programPosition % SixteenthNotesPerBar;
    if (barPos < 0) barPos += SixteenthNotesPerBar;
    var half = SixteenthNotesPerBar / 2.0;
    var alphaFrac = barPos <= half ? barPos / half : (SixteenthNotesPerBar - barPos) / half;
    var a = (int)Math.Round(alphaFrac * 255);
    if (a < 0) a = 0;
    if (a > 255) a = 255;

    var packed = (a << 24) | (r << 16) | (g << 8) | b;
    for (var i = 0; i < buffer.Length; i++)
    {
      buffer[i] = packed;
    }
  }
}