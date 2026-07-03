using System.Runtime.InteropServices;
using Hsp.LedStripController.Programs;
using Hsp.Osc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hsp.LedStripController;

public static class Plugin
{
  [UnmanagedCallersOnly(EntryPoint = "ReaperPluginEntry")]
  public static int ReaperPluginEntry(nint hInstance, nint rec)
  {
    var host = Host.CreateDefaultBuilder()
      .ConfigureLogging((context, lb) =>
      {
        lb.ClearProviders();
        lb.SetMinimumLevel(LogLevel.Warning);
#if DEBUG
        lb.SetMinimumLevel(LogLevel.Debug);
#endif
        lb.AddConfiguration(context.Configuration.GetSection("Logging"));
        lb.AddProvider(new ReaperConsoleLoggerProvider());
      })
      .ConfigureServices((context, sc) =>
      {
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<GmemService>();
        sc.AddSingleton<IOscClient>(c => new OscUdpClient(9100));
        sc.AddSingleton<LedStripProgramRegistry>();
        sc.AddSingleton<GmemToOscDispatcher>();
      })
      .Build();

    try
    {
      var state = PluginState.Initialize(ReaperPluginInfo.FromPointer(rec), host);
      var commands = host.Services.GetRequiredService<ICommandRegistry>();

      // Register LED strip programs (program number -> implementation).
      // Speed is no longer baked into the program; it is controlled live via the
      // generic argument (MIDI Note 16 velocity). Speed-only variants now map to
      // the same pattern/direction variant (e.g. 3==4, 5==7, 6==8, 9==11, 10==12).
      var programRegistry = host.Services.GetRequiredService<LedStripProgramRegistry>();
      programRegistry.Register<ColorCycleProgram>(1);
      programRegistry.Register<PulseProgram>(2);
      programRegistry.Register(3, () => new RandomProgram());
      programRegistry.Register(4, () => new RandomProgram());
      programRegistry.Register(5, () => new FallProgram(false));
      programRegistry.Register(6, () => new FallProgram(true));
      programRegistry.Register(7, () => new FallProgram(false));
      programRegistry.Register(8, () => new FallProgram(true));
      programRegistry.Register(9, () => new ExpandProgram(true));
      programRegistry.Register(10, () => new ExpandProgram(false));
      programRegistry.Register(11, () => new ExpandProgram(true));
      programRegistry.Register(12, () => new ExpandProgram(false));

      commands.Register("HSP_LEDCONTROLLER_START", "LED Controller: Start", Commands.Start);
      commands.Register("HSP_LEDCONTROLLER_STOP", "LED Controller: Stop", Commands.Stop);
      return 1;
    }
    catch (Exception ex)
    {
      return 0;
    }
  }
}