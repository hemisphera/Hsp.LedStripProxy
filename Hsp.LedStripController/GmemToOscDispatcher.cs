using System.Diagnostics;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hsp.LedStripController;

public class GmemToOscDispatcher
{
  private readonly GmemService _memService;
  private readonly Func<IOscClient> _oscClientFactory;
  private readonly List<LedStrip> _strips = [];
  private readonly ILogger<GmemToOscDispatcher> _logger;
  private const int NumLedStrips = 4;
  private CancellationTokenSource? _cts;
  private Task? _loopTask;
  private IOscClient? _oscClient;
  private int _failureCount;
  private Stopwatch? _failureWatch;


  public GmemToOscDispatcher(
    GmemService memService, Func<IOscClient> oscClientFactory,
    LedStripProgramRegistry programRegistry,
    ILogger<LedStrip> ledStripLogger,
    ILogger<GmemToOscDispatcher> logger)
  {
    _memService = memService;
    _oscClientFactory = oscClientFactory;

    _strips.AddRange(Enumerable.Range(0, NumLedStrips).Select(i => new LedStrip(i, programRegistry, ledStripLogger)));

    _logger = logger;
  }


  public bool IsRunning => _loopTask is { IsCompleted: false };

  public async Task<bool> ToggleAsync()
  {
    if (IsRunning)
    {
      await StopAsync();
      return false;
    }

    await StartAsync();
    return true;
  }

  public async Task StartAsync()
  {
    await StopAsync();

    _failureWatch = Stopwatch.StartNew();
    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    _memService.Connect("ledcontroller");
    _oscClient = _oscClientFactory();
    await _oscClient.ConnectAsync();
    _loopTask = Loop(token, _oscClient);
    _logger.LogInformation("OSC dispatcher started.");
  }

  private async Task Loop(CancellationToken ct, IOscClient oscClient)
  {
    var block = new double[LedStrip.CellsPerStrip * NumLedStrips];
    var segments = new double[LedStrip.NumSegmentsPerLedStrips];
    var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
    while (await timer.WaitForNextTickAsync(ct))
    {
      EmitFailures();
      try
      {
        await Task.Delay(10, ct);
        _memService.Read(0, block);
        foreach (var strip in _strips)
        {
          var msg = new Message($"/led/{strip.Index + 1}");
          strip.Render(block, segments);
          for (var j = 0; j < segments.Length; j++)
          {
            msg.PushAtom((int)segments[j]);
          }

          await msg.Send(oscClient);
        }
      }
      catch (Exception ex)
      {
        // ignore
        _logger.LogWarning(ex.Message);
        _failureCount++;
      }
    }
  }

  private void EmitFailures()
  {
    if (_failureCount == 0 || _failureWatch == null) return;
    if (_failureWatch.ElapsedMilliseconds < 5000) return;
    _logger.LogWarning("{count} messages have failed to send.", _failureCount);
    _failureCount = 0;
    _failureWatch.Restart();
  }

  public async Task StopAsync()
  {
    if (_cts == null) return;

    await _cts.CancelAsync();
    _failureWatch?.Stop();

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
      catch (OperationCanceledException)
      {
        // Expected: the loop was cancelled by the CTS above.
      }
    }

    _memService.Disconnect();
    if (_oscClient != null)
    {
      await _oscClient.DisconnectAsync();
      _oscClient.Dispose();
      _oscClient = null;
    }

    _cts.Dispose();
    _cts = null;
    _loopTask = null;
    _logger.LogInformation("OSC dispatcher stopped.");
  }
}