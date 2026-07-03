namespace Hsp.LedStripController;

public class EmitSegmentsProgram : ILedStripProgram
{
  public void Stop()
  {
  }

  public void Start(LedStrip ledStrip)
  {
  }

  public void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments)
  {
    for (var i = 0; i < segments.Length; i++)
    {
      buffer[i] = segments[i];
    }
  }
}