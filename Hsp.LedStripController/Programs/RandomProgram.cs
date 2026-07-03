using System.Security.Cryptography;

namespace Hsp.LedStripController.Programs;

/// <summary>
///   Takes the first segment's color as base and applies a random amount of
///   opacity to each segment every musical-note step.
///   Program number: 3. The animation speed is driven by the generic argument
///   (MIDI Note 16 velocity), expressed as a musical note (velocities 0 and 1
///   run at the maximum 32nd-note speed; higher velocity is slower; see
///   <see cref="NoteSpeed" />).
/// </summary>
public class RandomProgram : SpeedScaledProgram
{
  private int _lastStep = int.MinValue;
  private readonly int[] _alphas = new int[LedStrip.NumSegmentsPerLedStrips];


  public override void Start(LedStrip ledStrip)
  {
    base.Start(ledStrip);
    _lastStep = int.MinValue;
  }

  public override void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments)
  {
    var first = (int)segments[0];
    var r = (first >> 16) & 0xFF;
    var g = (first >> 8) & 0xFF;
    var b = first & 0xFF;

    var step = (int)Math.Floor(GetScaledPosition(programPosition, in arguments));
    if (step != _lastStep)
    {
      _lastStep = step;
      for (var i = 0; i < _alphas.Length; i++)
        _alphas[i] = RandomNumberGenerator.GetInt32(0, 256);
    }

    for (var i = 0; i < buffer.Length; i++)
    {
      var a = _alphas[Math.Min(i, _alphas.Length - 1)];
      buffer[i] = (a << 24) | (r << 16) | (g << 8) | b;
    }
  }
}