using System.Net;
using System.Net.Sockets;
using Hsp.Midi;
using Hsp.Midi.Messages;

namespace ConsoleApp3;

public sealed class LedStripDispatcher : IDisposable
{
  public string MidiDeviceName { get; }
  private readonly UdpClient _client;
  public const int StripCount = 4;
  private CancellationTokenSource? _cts;
  private InputMidiDevice? _device;
  private bool _running;

  public TimeSpan Frequency { get; set; } = TimeSpan.FromMilliseconds(100);

  public LedStrip[] Strips { get; }


  public LedStripDispatcher(string midiDeviceName, int port = 9977)
  {
    MidiDeviceName = midiDeviceName;
    _client = new UdpClient();
    _client.Connect(new IPEndPoint(IPAddress.Broadcast, port));
    Strips = new LedStrip[StripCount];
    for (byte i = 0; i < StripCount; i++)
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


  public void Start()
  {
    if (_running) return;

    _cts = new CancellationTokenSource();

    _device = InputMidiDevicePool.Instance.Open(MidiDeviceName);
    _device.MessageReceived += MidiMessageHandler;

    Task.Run(async () =>
    {
      var token = _cts.Token;
      while (!token.IsCancellationRequested)
      {
        await Task.Delay(Frequency, token);
        foreach (var strip in Strips)
        {
          await strip.Send(_client, token);
        }
      }
    });
    _running = true;
  }

  public void Stop()
  {
    if (!_running) return;
    _cts?.Cancel();

    if (_device != null)
    {
      _device.MessageReceived -= MidiMessageHandler;
      InputMidiDevicePool.Instance.Close(_device);
    }

    _running = false;
  }


  public void Dispose()
  {
    _client.Dispose();
  }
}