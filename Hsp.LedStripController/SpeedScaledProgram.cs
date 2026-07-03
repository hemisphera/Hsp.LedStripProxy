namespace Hsp.LedStripController;

/// <summary>
///   Base class for programs whose animation speed is driven by the generic
///   argument (MIDI Note 16 velocity). The argument selects a musical-note
///   duration (see <see cref="NoteSpeed" />); the program advances one step
///   per that many 32nd notes of program position. Velocities 0 and 1 both run
///   at the maximum speed (32nd note).
/// </summary>
public abstract class SpeedScaledProgram : ILedStripProgram
{
  public virtual void Start(LedStrip ledStrip)
  {
  }

  public virtual void Stop()
  {
  }

  /// <summary>
  ///   Returns the program position scaled by the argument's musical-note
  ///   duration (one animation step per N 32nd notes).
  /// </summary>
  protected double GetScaledPosition(double programPosition, in ProgramArguments arguments)
  {
    var duration = NoteSpeed.DurationFromArgument((int)Math.Round(arguments.Argument1));
    return duration > 0 ? programPosition / duration : 0;
  }

  public abstract void Render(double programPosition, Span<double> segments, double[] buffer, ProgramArguments arguments);
}