using Microsoft.Extensions.Logging;

namespace Hsp.LedStripController;

public class LedStrip
{
  private readonly LedStripProgramRegistry _programRegistry;
  private readonly ILogger _logger;
  public const int NumSegmentsPerLedStrips = 12;
  public const int CellsPerStrip = NumSegmentsPerLedStrips + 2; // 12 segment ARGB + program number + program position

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
    if (programNumber != _currProgramNo)
    {
      _logger.LogDebug("Led {index} switching to program {number}", Index, programNumber);
      _currProgram?.Stop();
      _currProgram = _programRegistry.Get(programNumber);
      _currProgram?.Start(this);
      _currProgramNo = programNumber;
    }

    var program = _currProgram ?? _defaultProgram;
    program.Render(programPosition, segments, buffer);
  }
}