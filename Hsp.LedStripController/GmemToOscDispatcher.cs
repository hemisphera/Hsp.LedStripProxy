using Hsp.Osc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hsp.LedStripController;

public class GmemToOscDispatcher : BackgroundService
{
  private readonly GmemService _memService;
  private readonly IOscClient _oscClient;
  private readonly ILogger<GmemToOscDispatcher> _logger;
  private const int NumLedStrips = 4;
  private const int NumSegmentsPerLedStrips = 12;


  public GmemToOscDispatcher(GmemService memService, IOscClient oscClient, ILogger<GmemToOscDispatcher> logger)
  {
    _memService = memService;
    _oscClient = oscClient;
    _logger = logger;
  }


  public override async Task StartAsync(CancellationToken cancellationToken)
  {
    await base.StartAsync(cancellationToken);
    _memService.Connect("ledcontroller");
    await _oscClient.ConnectAsync();
    _logger.LogInformation("OSC dispatcher started.");
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var block = new double[NumSegmentsPerLedStrips * NumLedStrips];
    while (!stoppingToken.IsCancellationRequested)
    {
      await Task.Delay(10, stoppingToken);
      _memService.Read(0, block);
      for (var i = 0; i < NumLedStrips; i++)
      {
        var msg = new Message($"/led/{i}");
        for (var j = 0; j < NumSegmentsPerLedStrips; j++)
        {
          msg.PushAtom((int)block[i * NumSegmentsPerLedStrips + j]);
        }

        await msg.Send(_oscClient);
      }
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _memService.Disconnect();
    await base.StopAsync(cancellationToken);
    await _oscClient.DisconnectAsync();
  }
}