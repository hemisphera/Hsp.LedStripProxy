using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hsp.LedStripProxy;

public sealed class UdpPackageSender : IPackageSender, IDisposable
{
  private readonly UdpClient _client;
  private int _packageCounter = 0;
  private readonly CancellationTokenSource _cts = new();


  public UdpPackageSender(IOptions<Settings> settings, ILogger<UdpPackageSender> logger)
  {
    _client = new UdpClient();
    _client.Connect(new IPEndPoint(IPAddress.Broadcast, settings.Value.UdpPort));
    logger.LogInformation("Sending UDP packages to {IpAddress}:{Port}", IPAddress.Broadcast, settings.Value.UdpPort);

    Task.Run(async () =>
    {
      var token = _cts.Token;
      while (!_cts.IsCancellationRequested)
      {
        await Task.Delay(TimeSpan.FromSeconds(3), token);
        var v = _packageCounter;
        _packageCounter = 0;
        logger.LogInformation("{PackageCounter} packages sent in the last 3 seconds", v);
      }
    });
  }


  public async Task Send(byte[] data, CancellationToken cancellationToken)
  {
    _packageCounter++;
    await _client.SendAsync(data, cancellationToken);
  }


  public void Dispose()
  {
    _cts.Cancel();
    _client.Dispose();
  }
}