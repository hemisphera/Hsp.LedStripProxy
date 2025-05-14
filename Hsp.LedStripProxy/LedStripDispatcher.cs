using System.Net;
using System.Net.Sockets;
using Hsp.Midi;
using Hsp.Midi.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hsp.LedStripProxy;

public sealed class LedStripDispatcher : BackgroundService
{
  private readonly ILogger<LedStripDispatcher> _logger;
  private readonly IPackageSender _sender;
  public string MidiDeviceName { get; }
  private InputMidiDevice? _device;
  private readonly TimeSpan _frequency;

  public LedStrip[] Strips { get; }


  public LedStripDispatcher(IOptions<Settings> settings, ILogger<LedStripDispatcher> logger, IPackageSender sender)
  {
    _logger = logger;
    _sender = sender;
    MidiDeviceName = settings.Value.MidiDeviceName;
    _frequency = TimeSpan.FromMilliseconds(settings.Value.UpdateInterval);

    var stripCount = settings.Value.StripCount;
    Strips = new LedStrip[stripCount];
    for (byte i = 0; i < stripCount; i++)
    {
      Strips[i] = new LedStrip(i);
    }
  }

  private void MidiMessageHandler(object? sender, IMidiMessage e)
  {
    foreach (var strip in Strips)
    {
      strip.ProcessMessage(e);
    }
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      await Task.Delay(_frequency, stoppingToken);
      await Task.WhenAll(Strips.Select(strip => strip.Send(_sender, stoppingToken)));
    }
  }

  public override async Task StartAsync(CancellationToken cancellationToken)
  {
    CloseMidiDevice();
    OpenMidiDevice();
    await base.StartAsync(cancellationToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    CloseMidiDevice();
    await base.StopAsync(cancellationToken);
  }

  private void OpenMidiDevice()
  {
    _logger.LogInformation("Opening MIDI device '{MidiDeviceName}'", MidiDeviceName);
    _device = InputMidiDevicePool.Instance.Open(MidiDeviceName);
    _device.MessageReceived += MidiMessageHandler;
  }

  private void CloseMidiDevice()
  {
    if (_device == null) return;
    _logger.LogInformation("Closing MIDI device '{MidiDeviceName}'", MidiDeviceName);
    _device.MessageReceived -= MidiMessageHandler;
    InputMidiDevicePool.Instance.Close(_device);
  }
}