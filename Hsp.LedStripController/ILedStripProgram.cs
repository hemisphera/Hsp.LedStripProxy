namespace Hsp.LedStripController;

public interface ILedStripProgram
{
  void Stop();
  void Start(LedStrip ledStrip);
  void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments);
}