using System.Security.Cryptography;

namespace Hsp.LedStripController.Programs;

/// <summary>
///   Takes the first segment's color as base and applies a random amount of
///   opacity to each segment every 16th note.
///   Program number: 3.
/// </summary>
public class RandomProgram : ILedStripProgram
{
  private readonly double _speed;
  private int _lastStep = int.MinValue;
  private readonly int[] _alphas = new int[LedStrip.NumSegmentsPerLedStrips];


  public RandomProgram(double speed)
  {
    _speed = speed;
  }


  public void Start(LedStrip ledStrip)
  {
    _lastStep = int.MinValue;
  }

  public void Stop()
  {
  }

  public void Render(double programPosition, Span<double> segments, double[] buffer)
  {
    var first = (int)segments[0];
    var r = (first >> 16) & 0xFF;
    var g = (first >> 8) & 0xFF;
    var b = first & 0xFF;

    var step = (int)Math.Floor(programPosition * _speed);
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