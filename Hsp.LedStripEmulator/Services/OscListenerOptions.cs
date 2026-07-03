namespace Hsp.LedStripEmulator.Services;

public sealed class OscListenerOptions
{
    public const string SectionName = "OscListener";

    public string BindAddress { get; set; } = "0.0.0.0";

    public int Port { get; set; } = 9100;
}