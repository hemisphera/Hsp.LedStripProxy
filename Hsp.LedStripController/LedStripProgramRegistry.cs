using ReaSharp;

namespace Hsp.LedStripController;

public class LedStripProgramRegistry
{
  private readonly Dictionary<int, Func<ILedStripProgram>> _factories = new();


  public void Register<T>(int programNumber) where T : ILedStripProgram, new()
  {
    _factories[programNumber] = static () => new T();
  }

  public void Register(int programNumber, Func<ILedStripProgram> factory)
  {
    _factories[programNumber] = factory;
  }

  public ILedStripProgram? Get(int programNumber)
  {
    return _factories.TryGetValue(programNumber, out var factory) ? factory() : null;
  }
}