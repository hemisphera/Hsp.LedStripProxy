using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hsp.LedStripController;

public class GmemToOscDispatcher
{
  private readonly GmemService _memService;
  private readonly IOscClient _oscClient;
  private readonly ILogger<GmemToOscDispatcher> _logger;
  private const int NumLedStrips = 4;
  private const int NumSegmentsPerLedStrips = 12;
  private CancellationTokenSource? _cts;
  private Task? _loopTask;


  public GmemToOscDispatcher(GmemService memService, IOscClient oscClient, ILogger<GmemToOscDispatcher> logger)
  {
    _memService = memService;
    _oscClient = oscClient;
    _logger = logger;
  }


  public async Task StartAsync()
  {
    await StopAsync();

    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    _memService.Connect("ledcontroller");
    await _oscClient.ConnectAsync();
    _loopTask = Loop(token);
    _logger.LogInformation("OSC dispatcher started.");
  }

  private async Task Loop(CancellationToken ct)
  {
    var block = new double[NumSegmentsPerLedStrips * NumLedStrips];
    while (!ct.IsCancellationRequested)
    {
      try
      {
        await Task.Delay(10, ct);
        _memService.Read(0, block);
        for (var i = 0; i < NumLedStrips; i++)
        {
          var msg = new Message($"/led/{i + 1}");
          for (var j = 0; j < NumSegmentsPerLedStrips; j++)
          {
            msg.PushAtom((int)block[i * NumSegmentsPerLedStrips + j]);
          }

          await msg.Send(_oscClient);
        }
      }
      catch (TaskCanceledException)
      {
      }
      catch (OperationCanceledException)
      {
      }
    }
  }

  public async Task StopAsync()
  {
    if (_cts == null) return;

    await _cts.CancelAsync();

    // Wait for the loop task to complete with a timeout
    if (_loopTask != null)
    {
      try
      {
        await _loopTask.WaitAsync(TimeSpan.FromSeconds(2));
      }
      catch (TimeoutException)
      {
        _logger.LogWarning("Loop task did not complete within timeout.");
      }
    }

    _memService.Disconnect();
    await _oscClient.DisconnectAsync();

    _cts.Dispose();
    _cts = null;
    _loopTask = null;
    _logger.LogInformation("OSC dispatcher stopped.");
  }
}