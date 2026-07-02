using System.Drawing;

namespace Hsp.LedStripController.Programs;

/// <summary>
///   Cycles a hue across the 12 segments based on the program position (in 16th notes).
///   Program number: 1.
/// </summary>
public class ColorCycleProgram : ILedStripProgram
{
  private const int NumSegments = 12;
  private const double HuesPerStep = 30.0; // degrees of hue per 16th note
  private const double SegmentSpacing = 30.0; // hue offset between adjacent segments

  public void Stop()
  {
  }

  public void Start(LedStrip ledStrip)
  {
  }

  public void Render(double position, Span<double> segments, double[] buffer)
  {
    for (var i = 0; i < buffer.Length; i++)
    {
      var hue = (position * HuesPerStep + i * SegmentSpacing) % 360;
      if (hue < 0) hue += 360;
      var color = HsvToRgb(hue, 1.0, 1.0);
      // Pack as ARGB int32 with full alpha (255).
      buffer[i] = (255 << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }
  }


  private static (int R, int G, int B) HsvToRgb(double h, double s, double v)
  {
    var c = v * s;
    var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
    var m = v - c;
    double r, g, b;
    switch ((int)(h / 60))
    {
      case 0:
        r = c;
        g = x;
        b = 0;
        break;
      case 1:
        r = x;
        g = c;
        b = 0;
        break;
      case 2:
        r = 0;
        g = c;
        b = x;
        break;
      case 3:
        r = 0;
        g = x;
        b = c;
        break;
      case 4:
        r = x;
        g = 0;
        b = c;
        break;
      default:
        r = c;
        g = 0;
        b = x;
        break;
    }

    return ((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
  }
}