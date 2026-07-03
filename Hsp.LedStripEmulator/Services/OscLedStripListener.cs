using System.Net;
using System.Text.RegularExpressions;
using Hsp.Osc;
using Microsoft.Extensions.Options;

namespace Hsp.LedStripEmulator.Services;

public sealed partial class OscLedStripListener : BackgroundService
{
    private readonly LedStripState _stripState;
    private readonly OscListenerOptions _options;
    private readonly ILogger<OscLedStripListener> _logger;
    private OscUdpServer? _server;

    public OscLedStripListener(IOptions<OscListenerOptions> options, LedStripState stripState, ILogger<OscLedStripListener> logger)
    {
        _stripState = stripState;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ipAddress = ResolveAddress(_options.BindAddress);
        _stripState.SetListenEndpoint($"{ipAddress}:{_options.Port}");

        _server = new OscUdpServer(ipAddress, _options.Port);
        _server.MessageReceived += OnMessageReceived;
        _server.MessageFailed += OnMessageFailed;
        _server.BeginListen();

        _logger.LogInformation("Listening for LED OSC messages on {Endpoint}.", _stripState.GetSnapshot().ListenEndpoint);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            DisposeServer();
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        DisposeServer();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        DisposeServer();
        base.Dispose();
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs eventArgs)
    {
        if (!TryParseStripMessage(eventArgs.Message, out var stripIndex, out var values))
        {
            return;
        }

        _stripState.UpdateStrip(stripIndex, values);
    }

    private void OnMessageFailed(object? sender, Exception exception)
    {
        _logger.LogWarning(exception, "Failed to parse OSC packet.");
    }

    private void DisposeServer()
    {
        var server = Interlocked.Exchange(ref _server, null);
        if (server is null)
        {
            return;
        }

        server.MessageReceived -= OnMessageReceived;
        server.MessageFailed -= OnMessageFailed;
        server.EndListen();
        server.Dispose();
    }

    private static bool TryParseStripMessage(Message message, out int stripIndex, out int[] argbValues)
    {
        stripIndex = -1;
        argbValues = Array.Empty<int>();

        var match = LedAddressRegex().Match(message.Address);
        if (!match.Success)
        {
            return false;
        }

        stripIndex = int.Parse(match.Groups["strip"].Value) - 1;
        if (stripIndex < 0 || stripIndex >= LedStripState.StripCount)
        {
            return false;
        }

        argbValues = new int[LedStripState.SegmentCount];
        var count = Math.Min(message.Atoms.Count, LedStripState.SegmentCount);
        for (var i = 0; i < count; i++)
        {
            argbValues[i] = message.Atoms[i].Int32Value;
        }

        return true;
    }

    private static IPAddress ResolveAddress(string bindAddress)
    {
        if (string.IsNullOrWhiteSpace(bindAddress) || bindAddress == "*" || bindAddress == "0.0.0.0")
        {
            return IPAddress.Any;
        }

        return IPAddress.Parse(bindAddress);
    }

    [GeneratedRegex("^/led/(?<strip>[1-4])$")]
    private static partial Regex LedAddressRegex();
}