namespace Hsp.LedStripProxy;

public class Settings
{
  public string MidiDeviceName { get; set; } = "loopMIDI Port";
  public int StripCount { get; set; } = 4;
  public int UpdateInterval { get; set; } = 10;
  public int UdpPort { get; set; } = 9977;
}