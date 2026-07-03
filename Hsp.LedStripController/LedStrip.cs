using Microsoft.Extensions.Logging;

namespace Hsp.LedStripController;

public class LedStrip
{
  private readonly LedStripProgramRegistry _programRegistry;
  private readonly ILogger _logger;
  public const int NumSegmentsPerLedStrips = 12;
  public const int CellsPerStrip = NumSegmentsPerLedStrips + 5; // 12 segment ARGB + program number + program position + 2 generic arguments + brightness multiplier

  private readonly int _memOffset;
  private ILedStripProgram? _currProgram;
  private readonly EmitSegmentsProgram _defaultProgram;
  private int _currProgramNo;

  public int Index { get; }


  public LedStrip(int index, LedStripProgramRegistry programRegistry, ILogger logger)
  {
    _programRegistry = programRegistry;
    _logger = logger;
    Index = index;
    _memOffset = index * CellsPerStrip;
    _defaultProgram = new EmitSegmentsProgram();
  }


  public void Render(double[] memBlock, double[] buffer)
  {
    var myBlock = memBlock.AsSpan(_memOffset .. (_memOffset + CellsPerStrip));
    var segments = memBlock.AsSpan(_memOffset .. (_memOffset + NumSegmentsPerLedStrips));

    var programNumber = (int)myBlock[NumSegmentsPerLedStrips];
    var programPosition = myBlock[NumSegmentsPerLedStrips + 1];
    var programArgument1 = myBlock[NumSegmentsPerLedStrips + 2]; // slot 14: generic argument 1 (Note 16)
    var programArgument2 = myBlock[NumSegmentsPerLedStrips + 3]; // slot 15: generic argument 2 (Note 17)
    var multiplier = myBlock[NumSegmentsPerLedStrips + 4]; // slot 16: brightness multiplier (0.0-1.0)
    if (programNumber != _currProgramNo)
    {
      _logger.LogDebug("Led {index} switching to program {number}", Index, programNumber);
      _currProgram?.Stop();
      _currProgram = _programRegistry.Get(programNumber);
      _currProgram?.Start(this);
      _currProgramNo = programNumber;
    }

    var program = _currProgram ?? _defaultProgram;
    program.Render(programPosition, segments, buffer, new ProgramArguments(programArgument1, programArgument2));

    // The JSFX writes raw segment brightness (no multiplier) into the alpha
    // channel; the backend applies the per-strip brightness multiplier here.
    if (multiplier < 1.0)
    {
      for (var i = 0; i < buffer.Length; i++)
      {
        var packed = (int)buffer[i];
        var a = (packed >> 24) & 0xFF;
        var r = (packed >> 16) & 0xFF;
        var g = (packed >> 8) & 0xFF;
        var b = packed & 0xFF;
        a = (int)Math.Round(a * multiplier);
        if (a > 255) a = 255;
        if (a < 0) a = 0;
        buffer[i] = (a << 24) | (r << 16) | (g << 8) | b;
      }
    }
  }
}