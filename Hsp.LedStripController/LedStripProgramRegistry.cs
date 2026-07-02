using ReaSharp;

namespace Hsp.LedStripController;

public class LedStripProgramRegistry
{
  private readonly Dictionary<int, Type> _programs = new();


  public void Register<T>(int programNumber)
  {
    _programs[programNumber] = typeof(T);
  }

  public ILedStripProgram? Get(int programNumber)
  {
    ILedStripProgram? program = null;
    if (_programs.TryGetValue(programNumber, out var programType))
      program = Activator.CreateInstance(programType) as ILedStripProgram;
    return program;
  }
}