namespace Hsp.LedStripController;

/// <summary>
///   Carries the generic program arguments read from the GMEM block (slots 14
///   and 15) into <see cref="ILedStripProgram.Render" />. Packed as a struct so
///   that additional arguments can be added in the future without changing the
///   <see cref="ILedStripProgram.Render" /> signature. Passed by value (16
///   bytes for two doubles) so there is no heap allocation or indirection.
/// </summary>
public readonly struct ProgramArguments
{
  /// <summary>Generic argument 1 (GMEM slot 14, MIDI Note 16 velocity).</summary>
  public double Argument1 { get; }

  /// <summary>Generic argument 2 (GMEM slot 15, MIDI Note 17 velocity).</summary>
  public double Argument2 { get; }


  public ProgramArguments(double argument1, double argument2)
  {
    Argument1 = argument1;
    Argument2 = argument2;
  }
}