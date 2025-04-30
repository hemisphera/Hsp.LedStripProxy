using System.Drawing;
using ConsoleApp3;

var dispatcher = new LedStripDispatcher("loopMIDI Port");
dispatcher.Frequency = TimeSpan.FromMilliseconds(25);
dispatcher.Start();

foreach (var str in dispatcher.Strips)
{
  str.ChangeColor(Color.FromArgb(255, 120, 120, 0));
}

Console.WriteLine($"Listening on '{dispatcher.MidiDeviceName}'");
Console.ReadLine();

Console.WriteLine("Stopping...");
dispatcher.Stop();
dispatcher.Dispose();