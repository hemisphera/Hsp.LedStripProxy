namespace Hsp.LedStripEmulator.Services;

public sealed record LedStripSnapshot(IReadOnlyList<int[]> Strips, DateTimeOffset? LastMessageAt, string ListenEndpoint)
{
    public static LedStripSnapshot Empty { get; } = new(
        Enumerable.Range(0, LedStripState.StripCount).Select(_ => new int[LedStripState.SegmentCount]).ToArray(),
        null,
        "0.0.0.0:9100");

    public bool HasSignal => LastMessageAt.HasValue;
}