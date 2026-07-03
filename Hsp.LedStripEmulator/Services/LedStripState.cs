namespace Hsp.LedStripEmulator.Services;

public sealed class LedStripState
{
    public const int StripCount = 4;
    public const int SegmentCount = 12;

    private readonly object _gate = new();
    private readonly int[][] _strips = Enumerable.Range(0, StripCount).Select(_ => new int[SegmentCount]).ToArray();
    private DateTimeOffset? _lastMessageAt;
    private string _listenEndpoint = "0.0.0.0:9100";

    public event Action? Changed;

    public LedStripSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new LedStripSnapshot(
                _strips.Select(strip => strip.ToArray()).ToArray(),
                _lastMessageAt,
                _listenEndpoint);
        }
    }

    public void SetListenEndpoint(string listenEndpoint)
    {
        lock (_gate)
        {
            _listenEndpoint = listenEndpoint;
        }

        Changed?.Invoke();
    }

    public void UpdateStrip(int stripIndex, IReadOnlyList<int> argbValues)
    {
        if (stripIndex < 0 || stripIndex >= StripCount)
        {
            return;
        }

        lock (_gate)
        {
            var strip = _strips[stripIndex];
            Array.Clear(strip);

            var count = Math.Min(argbValues.Count, SegmentCount);
            for (var i = 0; i < count; i++)
            {
                strip[i] = argbValues[i];
            }

            _lastMessageAt = DateTimeOffset.UtcNow;
        }

        Changed?.Invoke();
    }
}